namespace SecondMonitor.Hub.Core.Ai
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using SecondMonitor.Hub.Core.Race;
    using SecondMonitor.Hub.Core.Telemetry;

    /// <summary>
    /// L'ingénieur prend la parole de lui-même quand une décision se présente : pluie qui arrive, piste assez mouillée
    /// pour les pneus pluie (ou qui sèche), relais qu'on peut allonger (seulement si ça évite un arrêt), arrêt dans deux tours,
    /// voiture qui revient par derrière, surchauffe. Les chiffres sont calculés ici ; les messages sont courts, en français.
    /// </summary>
    public sealed class ProactiveEngineer
    {
        private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");
        private readonly Dictionary<string, DateTime> _fired = new();
        private readonly Dictionary<string, DateTime> _hotSince = new();
        private DateTime _lastMessage = DateTime.MinValue;
        private int _previousRain = -1;

        public void Reset()
        {
            _debriefLap = -1;
            _fired.Clear();
            _hotSince.Clear();
            _lastMessage = DateTime.MinValue;
            _previousRain = -1;
        }

        /// <summary>Message à dire maintenant, ou null. À appeler toutes les quelques secondes.</summary>
        public ProactiveMessage Evaluate(RaceSnapshot snap, WeatherReportDto weather, RaceMemory memory, ProactiveOptions options, DateTime nowUtc)
        {
            // Pneus, freins et moteur : dans toutes les sessions (essais, qualification, tour de formation compris).
            // Stratégie (relais, arrêts, duels) : en course seulement.
            if (snap?.HasData != true || snap.Player == null || snap.SessionPhase == "Checkered")
            {
                return null;
            }

            DriverRow me = snap.Drivers.FirstOrDefault(d => d.IsPlayer);
            if (me == null || me.InPits || me.FinishStatus is "Finished" or "Dnf" or "Dq")
            {
                return null;
            }

            bool race = snap.SessionType == "Race" && me.Laps >= 1;

            int rain = snap.Weather.Rain;
            bool rainStarted = _previousRain is >= 0 and < 5 && rain >= 5;
            _previousRain = rain;

            if (memory?.Pending(options.AnswerWindowSeconds) != null)
            {
                return null; // on attend la réponse à la question précédente
            }

            var candidates = new List<ProactiveMessage>();
            PlayerStrategy p = snap.Player;
            TyreAlerts(p, me, candidates);
            DamageAlerts(p, candidates);
            if (HybridAdvisor.Proactive(snap, me) is ProactiveMessage battery)
            {
                candidates.Add(battery); // batterie : attaque / défense, dernier tour, batterie vide ou basse, tour de sortie
            }
            if (snap.SessionType is "Practice" or "WarmUp")
            {
                PracticeDebrief(snap, me, p, candidates);
            }

            if (race)
            {
                CutAlerts(snap, me, p, candidates);
            }
            bool onWets = IsWetCompound(me.Tyres.Compound);
            int threshold = options.WetThresholdPct;

            // ── Pluie ──
            if (rainStarted && !onWets && me.Laps >= 1)
            {
                candidates.Add(Q("rain-start", "pluie", $"Il commence à pleuvoir, intensité {rain} %. Comment est la piste de votre côté ? On peut rester en pneus secs ?", 600));
            }

            if (!onWets && snap.Weather.Wetness >= threshold)
            {
                candidates.Add(Q("wet-crossover", "pluie", $"La piste est mouillée à {snap.Weather.Wetness} %. Je conseille les pneus pluie. Comment ça se passe ? On rentre au prochain passage ?", 480, urgent: true));
            }
            else if (onWets && snap.Weather.Wetness < threshold / 2 && rain == 0)
            {
                candidates.Add(Q("drying", "pluie", $"La piste sèche, humidité {snap.Weather.Wetness} %. Comment sont les pneus pluie ? On repasse en pneus secs ?", 480));
            }

            // ── Relais : on peut aller plus loin côté essence, est-ce que ça vaut le coup ? ──
            ProactiveMessage extend = race ? StintExtension(snap, me, p, memory) : null;
            if (extend != null)
            {
                candidates.Add(extend);
            }

            // ── Arrêt dans deux tours ──
            int? stopLap = memory?.PlannedStopLap > me.Laps ? memory.PlannedStopLap : p.RecommendedStopLap;
            if (race && memory?.NoStopPlanned != true && stopLap is int sl && sl - me.Laps is 1 or 2)
            {
                string load = Load(p);
                candidates.Add(Info($"pit-{me.Laps - p.StintLaps}", "stand", $"Box dans {sl - me.Laps} tour{(sl - me.Laps > 1 ? "s" : string.Empty)}, au tour {sl}.{load}", 100000, urgent: true));
            }

            // ── Duels ──
            foreach (BattleDto b in p.Battles.Where(b => race && b.LapsToContact is <= 3 && (b.Gap ?? 0) >= 1.0))
            {
                string pace = N(Math.Abs(b.PaceDelta ?? 0), 1);
                double n = Math.Max(1, Math.Ceiling(b.LapsToContact.Value));
                string laps = n <= 1 ? "un tour" : $"{N(n, 0)} tours";
                candidates.Add(b.Side == "behind"
                    ? Info($"threat-{b.Id}", "duel", $"Attention, le numéro {b.Number} revient de {pace} seconde par tour. Il sera dans vos roues dans environ {laps}.", 360)
                    : Info($"chance-{b.Id}", "duel", $"Vous reprenez {pace} seconde par tour au numéro {b.Number}. Contact dans environ {laps}, continuez comme ça.", 360));
            }

            // ── Surchauffe (au moins 30 s d'affilée) ──
            EngineDto e = p.Engine;
            if (e != null)
            {
                if (Hot("water", e.WaterTemp, e.WaterIdeal, e.WaterWindow, nowUtc) || Hot("oil", e.OilTemp, e.OilIdeal, e.OilWindow, nowUtc))
                {
                    candidates.Add(Info("engine-hot", "moteur", $"Le moteur chauffe : eau {N(e.WaterTemp ?? 0, 0)} degrés, huile {N(e.OilTemp ?? 0, 0)} degrés. Sortez de l'aspiration quand vous pouvez.", 600));
                }
            }

            WheelStatus hottestBrake = p.Wheels.Where(w => w.Brake.HasValue && w.BrakeIdeal.HasValue).OrderByDescending(w => w.Brake - (w.BrakeIdeal + (w.BrakeWindow ?? 0))).FirstOrDefault();
            if (hottestBrake != null && Hot("brake", hottestBrake.Brake, hottestBrake.BrakeIdeal, hottestBrake.BrakeWindow, nowUtc))
            {
                candidates.Add(Info("brake-hot", "freins", $"Les freins chauffent, {N(hottestBrake.Brake.Value, 0)} degrés à l'{WheelName(hottestBrake.Position)}. Freinez un peu plus tôt et plus progressivement.", 600));
            }

            foreach (ProactiveMessage m in candidates.OrderByDescending(c => c.Urgent))
            {
                if (_fired.TryGetValue(m.Key, out DateTime last) && (nowUtc - last).TotalSeconds < m.CooldownSeconds)
                {
                    continue;
                }

                double since = (nowUtc - _lastMessage).TotalSeconds;
                if (since < (m.Urgent ? 20 : options.MinIntervalSeconds))
                {
                    continue;
                }

                _fired[m.Key] = nowUtc;
                _lastMessage = nowUtc;
                return m;
            }

            return null;
        }

        /// <summary>
        /// Pneus : tour entier au-dessus de la plage (moyenne), surchauffe de plus de 30 s d'affilée, pneus froids au départ
        /// (tour de formation, sortie des stands). Un seul message à la fois : le pneu le plus touché, les autres cités.
        /// </summary>
        private static void TyreAlerts(PlayerStrategy p, DriverRow me, List<ProactiveMessage> candidates)
        {
            var thermal = p.TyreThermal ?? new List<TyreThermalDto>();
            var lapHot = thermal.Where(t => t.LastLapState == "chaud" && t.LastLapAverage > t.Max).OrderByDescending(t => t.LastLapAverage - t.Max).ToList();
            if (lapHot.Count > 0)
            {
                TyreThermalDto t = lapHot[0];
                string others = lapHot.Count > 1 ? $" Aussi : {string.Join(", ", lapHot.Skip(1).Select(x => TyreThermalWatch.WheelName(x.Position)))}." : string.Empty;
                candidates.Add(Info(
                    $"tyre-lap-{t.Position}-{me.Laps}",
                    "pneus",
                    $"Pneu {TyreThermalWatch.WheelName(t.Position)} trop chaud sur tout le dernier tour : {N(t.LastLapAverage.Value, 0)} degrés de moyenne, pour une plage de {N(t.Min ?? 0, 0)} à {N(t.Max ?? 0, 0)}.{others} {TyreAdvice(lapHot)}",
                    240,
                    urgent: true));
            }

            TyreThermalDto hot = thermal.Where(t => t.HotSeconds >= 30).OrderByDescending(t => t.HotSeconds).FirstOrDefault();
            if (hot != null && lapHot.Count == 0)
            {
                candidates.Add(Info(
                    $"tyre-hot-{hot.Position}",
                    "pneus",
                    $"Le pneu {TyreThermalWatch.WheelName(hot.Position)} surchauffe depuis {N(hot.HotSeconds, 0)} secondes : {N(hot.Surface ?? 0, 0)} degrés pour {N(hot.Max ?? 0, 0)} au maximum. {TyreAdvice(new List<TyreThermalDto> { hot })}",
                    180,
                    urgent: true));
            }

            // Pneus froids au départ : tour de formation (0 tour bouclé) ou relais tout neuf.
            if (me.Laps == 0 || p.StintLaps == 0)
            {
                TyreThermalDto cold = thermal.Where(t => t.Surface.HasValue && t.Min.HasValue && t.Surface < t.Min - 10).OrderBy(t => t.Surface - t.Min).FirstOrDefault();
                if (cold != null && me.Speed > 40)
                {
                    candidates.Add(Info(
                        "tyre-cold",
                        "pneus",
                        $"Pneus encore froids : {N(cold.Surface.Value, 0)} degrés à l'{TyreThermalWatch.WheelName(cold.Position)}, il en faut au moins {N(cold.Min.Value, 0)}. Faites-les chauffer : zigzags et freinages appuyés.",
                        600));
                }
            }
        }

        /// <summary>Nouveau dégât, puis ce qu'il coûte au tour une fois mesuré (et s'il vaut un arrêt).</summary>
        private static void DamageAlerts(PlayerStrategy p, List<ProactiveMessage> candidates)
        {
            if (p.DamageEvent is not DamageEvent ev)
            {
                return;
            }

            string id = $"{ev.Key}-{ev.Time:0}";
            candidates.Add(Info($"damage-{id}", "dégâts", $"Dégât : {ev.Label}, {N(ev.Percent, 0)} %. Je mesure ce que ça coûte au tour.", 100000, urgent: true));
            if (ev.LossPerLap is double loss)
            {
                double left = p.LapsRemaining ?? 0;
                double pitLoss = p.PitPlan?.PitLoss ?? SecondMonitor.Hub.Core.Strategy.PitPlanner.DefaultPitLoss;
                string verdict = loss < 0.1 ? "C'est négligeable, on continue."
                    : left > 0 && loss * left > pitLoss + 5 ? $"Sur la fin de course ça fait {N(loss * left, 0)} secondes, plus qu'un arrêt : on peut réparer au prochain passage."
                    : "Moins qu'un arrêt d'ici l'arrivée : on continue.";
                candidates.Add(Info($"damage-loss-{id}", "dégâts", $"Le dégât vous coûte {N(Math.Max(0, loss), 2)} seconde par tour. {verdict}", 100000, urgent: true));
            }
        }

        /// <summary>Undercut jouable sur la voiture devant (si l'arrêt approche), menace d'undercut de la voiture derrière.</summary>
        private static void CutAlerts(RaceSnapshot snap, DriverRow me, PlayerStrategy p, List<ProactiveMessage> candidates)
        {
            var plan = p.PitPlan;
            if (plan == null)
            {
                return;
            }

            bool stopSoon = p.RecommendedStopLap is int sl && sl - me.Laps is >= 0 and <= 3;
            var under = plan.Cuts.FirstOrDefault(c => c.Kind == "undercut" && c.Feasible);
            if (under != null && stopSoon)
            {
                candidates.Add(Info($"undercut-{under.RivalId}-{me.Laps}", "stratégie", under.Text + (plan.RejoinNow?.Text is string r ? " " + r : string.Empty), 300));
            }

            var threat = plan.Cuts.FirstOrDefault(c => c.Kind == "menace-undercut" && c.Feasible);
            DriverRow rival = threat == null ? null : snap.Drivers.FirstOrDefault(d => d.Id == threat.RivalId);
            if (threat != null && rival?.NextStop?.LapsUntil is <= 1)
            {
                candidates.Add(Info($"threat-under-{threat.RivalId}-{me.Laps}", "stratégie", threat.Text, 300));
            }
        }

        private int _debriefLap = -1;

        /// <summary>Essais : à la fin d'un tour, ce qui mérite un réglage (pressions, carrossage) ou un nouveau meilleur tour.</summary>
        private void PracticeDebrief(RaceSnapshot snap, DriverRow me, PlayerStrategy p, List<ProactiveMessage> candidates)
        {
            if (me.Laps <= _debriefLap || me.LastLap is not double last || last <= 0)
            {
                _debriefLap = Math.Max(_debriefLap, me.Laps);
                return;
            }

            _debriefLap = me.Laps;
            var notes = new List<string>();
            if (me.LastLapIsPersonalBest)
            {
                notes.Add($"Meilleur tour : {LapSpoken(last)}.");
            }

            foreach (WheelStatus w in p.Wheels)
            {
                if (w.Pressure is double pr && w.IdealPressure is double ideal && Math.Abs(pr - ideal) >= 5)
                {
                    notes.Add($"Pression {WheelName(w.Position)} {(pr > ideal ? "trop haute" : "trop basse")} de {N(Math.Abs(pr - ideal), 0)} kilopascals.");
                }
            }

            WheelStatus fl = p.Wheels.FirstOrDefault(w => w.Position == "FL");
            if (fl?.TempInner is double inner && fl.TempOuter is double outer)
            {
                double spread = inner - outer;
                if (spread > 12)
                {
                    notes.Add($"À l'avant, l'intérieur du pneu est {N(spread, 0)} degrés plus chaud que l'extérieur : trop de carrossage négatif.");
                }
                else if (spread < 0)
                {
                    notes.Add("À l'avant, l'extérieur du pneu est plus chaud que l'intérieur : pas assez de carrossage négatif.");
                }
            }

            if (notes.Count > 0)
            {
                candidates.Add(Info($"debrief-{me.Laps}", "essais", string.Join(" ", notes.Take(3)), 60));
            }
        }

        private static string LapSpoken(double s)
        {
            int m = (int)(s / 60);
            double rest = s - (m * 60);
            return m > 0 ? $"{m} minute{(m > 1 ? "s" : string.Empty)} {N(rest, 1)}" : $"{N(rest, 1)} secondes";
        }

        private static string TyreAdvice(List<TyreThermalDto> hot)
        {
            bool front = hot.Any(t => t.Position is "FL" or "FR");
            bool rear = hot.Any(t => t.Position is "RL" or "RR");
            if (front && rear)
            {
                return "Rythme à calmer un tour, et vérifiez les pressions au prochain arrêt.";
            }

            return front
                ? "Moins de glisse en entrée de virage, n'attaquez pas le train avant."
                : "Doucement sur l'accélérateur en sortie de virage, évitez le patinage.";
        }

        /// <summary>Réponse du pilote à une question de l'IA : décision, réponse radio et note pour la mémoire.</summary>
        public static (string Reply, string Note) HandleAnswer(PendingQuestion q, string answer, RaceMemory memory)
        {
            bool? yes = RadioCommands.YesNo(answer);
            string quoted = $"« {answer.Trim().TrimEnd('.', '!')} »";
            switch (q.Topic)
            {
                case "relais" when yes == true && q.ProposedLap == int.MaxValue:
                    memory.NoStopPlanned = true;
                    memory.PlannedStopLap = null;
                    return ("Bien reçu, on va au bout sans s'arrêter. Dites-moi si les pneus lâchent.", $"décision : finir sans s'arrêter (accord du pilote : {quoted})");
                case "relais" when yes == true && q.ProposedLap.HasValue:
                    memory.PlannedStopLap = q.ProposedLap;
                    return ($"Bien reçu, on allonge le relais : arrêt au tour {q.ProposedLap}. Dites-moi si les pneus lâchent.", $"relais allongé jusqu'au tour {q.ProposedLap} (accord du pilote : {quoted})");
                case "relais" when yes == false:
                    memory.PlannedStopLap = q.PlannedLap;
                    return ($"Reçu, on garde l'arrêt au tour {q.PlannedLap}.", $"relais non allongé, pneus jugés limites ({quoted})");
                case "relais":
                    return ($"Reçu, je note. On garde l'arrêt au tour {q.PlannedLap} pour l'instant.", $"sur les pneus : {quoted}");
                case "pluie" when yes == false:
                    return ("Reçu. On prépare les pneus pluie, je vous dis quand rentrer.", $"pluie : le pilote veut des pneus pluie ou trouve la piste difficile ({quoted})");
                case "pluie" when yes == true:
                    return ("Bien reçu, on reste dehors. Je surveille l'humidité de la piste.", $"pluie : le pilote peut continuer avec ses pneus actuels ({quoted})");
                default:
                    return ("Reçu, c'est noté.", $"{q.Topic} : {quoted}");
            }
        }

        private ProactiveMessage StintExtension(RaceSnapshot snap, DriverRow me, PlayerStrategy p, RaceMemory memory)
        {
            if (p.RecommendedStopReason != "usure des pneus" || p.RecommendedStopLap is not int tyreStop || p.Fuel?.PerLap is not > 0 || p.Fuel.LapsLeft is not double fuelLaps || p.LapsRemaining is not double remaining)
            {
                return null;
            }

            if (memory?.PlannedStopLap > me.Laps || memory?.NoStopPlanned == true)
            {
                return null; // décision déjà prise pour ce relais
            }

            double energyLaps = p.Energy?.LapsLeft ?? double.MaxValue;
            double reach = Math.Min(fuelLaps, energyLaps) - 0.5; // tours encore possibles avant de devoir ravitailler
            int extraLaps = (int)Math.Floor(me.Laps + reach) - tyreStop;
            if (extraLaps < 3 || tyreStop - me.Laps > 6)
            {
                return null; // pas assez de marge, ou trop tôt pour en parler
            }

            WheelStatus worst = p.Wheels.Where(w => w.WearPerLapPct > 0).OrderByDescending(w => w.WearPct).FirstOrDefault();
            if (worst == null)
            {
                return null;
            }

            int proposed = (int)Math.Floor(me.Laps + Math.Min(reach, remaining));
            double wearAtProposed = worst.WearPct + (worst.WearPerLapPct.Value * (proposed - me.Laps));
            if (wearAtProposed > 92)
            {
                return null; // ça ne vaut pas le risque de crevaison
            }

            // Ça vaut le coup seulement si l'on finit sans s'arrêter ou si l'on économise un arrêt.
            bool finish = reach >= remaining;
            double capacityLaps = p.Fuel.Capacity / p.Fuel.PerLap.Value;
            int StopsAfter(double firstStint) => (int)Math.Ceiling(Math.Max(0, remaining - firstStint - 0.01) / Math.Max(1, capacityLaps - 1));
            int stopsEarly = 1 + Math.Max(0, StopsAfter(tyreStop - me.Laps) - 1);
            int stopsLate = finish ? 0 : 1 + Math.Max(0, StopsAfter(reach) - 1);
            if (!finish && stopsLate >= stopsEarly)
            {
                return null;
            }

            string gain = finish ? "on pourrait finir sans s'arrêter" : "on économiserait un arrêt";
            string text = finish
                ? $"Côté essence on peut aller au bout : {gain}. Les pneus finiraient vers {N(Math.Min(100, wearAtProposed), 0)} % d'usure. Comment vous les sentez ? On tente ?"
                : $"Côté essence on peut aller jusqu'au tour {proposed} au lieu du {tyreStop} : {gain}. Les pneus seraient vers {N(wearAtProposed, 0)} % d'usure. Comment vous les sentez ? On allonge le relais ?";
            ProactiveMessage m = Q($"extend-{me.Laps - p.StintLaps}", "relais", text, 100000);
            m.PlannedLap = tyreStop;
            m.ProposedLap = finish ? null : proposed;
            if (finish)
            {
                m.ProposedLap = int.MaxValue; // « pas d'arrêt » : on garde une valeur au-delà de l'arrivée
            }

            return m;
        }

        private bool Hot(string key, double? value, double? ideal, double? window, DateTime now)
        {
            if (!value.HasValue || !ideal.HasValue)
            {
                _hotSince.Remove(key);
                return false;
            }

            if (value <= ideal + (window ?? 10) + 2)
            {
                _hotSince.Remove(key);
                return false;
            }

            if (!_hotSince.TryGetValue(key, out DateTime since))
            {
                _hotSince[key] = now;
                return false;
            }

            return (now - since).TotalSeconds >= 30;
        }

        private static bool IsWetCompound(string compound)
        {
            string c = (compound ?? string.Empty).ToLowerInvariant();
            return c.Contains("wet") || c.Contains("rain") || c.Contains("pluie") || c.Contains("inter");
        }

        private static string Load(PlayerStrategy p)
        {
            var parts = new List<string>();
            if (p.Fuel?.NextStopAdd > 0.5)
            {
                parts.Add($"{N(p.Fuel.NextStopAdd.Value, 0)} litres");
            }

            if (p.Energy?.NextStopAddPct > 0.5)
            {
                parts.Add($"{N(p.Energy.NextStopAddPct.Value, 0)} % d'énergie");
            }

            return parts.Count == 0 ? string.Empty : " On mettra " + string.Join(" et ", parts) + ".";
        }

        private static string WheelName(string position) => position switch { "FL" => "avant gauche", "FR" => "avant droit", "RL" => "arrière gauche", _ => "arrière droit" };

        private static ProactiveMessage Q(string key, string topic, string text, double cooldown, bool urgent = false) => new() { Key = key, Topic = topic, Text = text, IsQuestion = true, CooldownSeconds = cooldown, Urgent = urgent };

        private static ProactiveMessage Info(string key, string topic, string text, double cooldown, bool urgent = false) => new() { Key = key, Topic = topic, Text = text, CooldownSeconds = cooldown, Urgent = urgent };

        private static string N(double v, int digits) => v.ToString("F" + digits, Fr);
    }

    public sealed class ProactiveMessage
    {
        public string Key { get; set; }

        public string Topic { get; set; }

        public string Text { get; set; }

        public bool IsQuestion { get; set; }

        public bool Urgent { get; set; }

        public double CooldownSeconds { get; set; }

        public int? PlannedLap { get; set; }

        public int? ProposedLap { get; set; }
    }

    public sealed class ProactiveOptions
    {
        public int WetThresholdPct { get; set; } = 25;

        public double MinIntervalSeconds { get; set; } = 120;

        public double AnswerWindowSeconds { get; set; } = 120;
    }
}
