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
            _fired.Clear();
            _hotSince.Clear();
            _lastMessage = DateTime.MinValue;
            _previousRain = -1;
        }

        /// <summary>Message à dire maintenant, ou null. À appeler toutes les quelques secondes.</summary>
        public ProactiveMessage Evaluate(RaceSnapshot snap, WeatherReportDto weather, RaceMemory memory, ProactiveOptions options, DateTime nowUtc)
        {
            if (snap?.HasData != true || snap.SessionType != "Race" || snap.Player == null || snap.SessionPhase == "Checkered")
            {
                return null;
            }

            DriverRow me = snap.Drivers.FirstOrDefault(d => d.IsPlayer);
            if (me == null || me.InPits || me.Laps < 1 || me.FinishStatus is "Finished" or "Dnf" or "Dq")
            {
                return null;
            }

            int rain = snap.Weather.Rain;
            bool rainStarted = _previousRain is >= 0 and < 5 && rain >= 5;
            _previousRain = rain;

            if (memory?.Pending(options.AnswerWindowSeconds) != null)
            {
                return null; // on attend la réponse à la question précédente
            }

            var candidates = new List<ProactiveMessage>();
            PlayerStrategy p = snap.Player;
            bool onWets = IsWetCompound(me.Tyres.Compound);
            int threshold = options.WetThresholdPct;

            // ── Pluie ──
            if (rainStarted && !onWets)
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
            ProactiveMessage extend = StintExtension(snap, me, p, memory);
            if (extend != null)
            {
                candidates.Add(extend);
            }

            // ── Arrêt dans deux tours ──
            int? stopLap = memory?.PlannedStopLap > me.Laps ? memory.PlannedStopLap : p.RecommendedStopLap;
            if (memory?.NoStopPlanned != true && stopLap is int sl && sl - me.Laps is 1 or 2)
            {
                string load = Load(p);
                candidates.Add(Info($"pit-{me.Laps - p.StintLaps}", "stand", $"Box dans {sl - me.Laps} tour{(sl - me.Laps > 1 ? "s" : string.Empty)}, au tour {sl}.{load}", 100000, urgent: true));
            }

            // ── Duels ──
            foreach (BattleDto b in p.Battles.Where(b => b.LapsToContact is <= 3 && (b.Gap ?? 0) >= 1.0))
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
