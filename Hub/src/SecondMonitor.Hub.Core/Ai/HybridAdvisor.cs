namespace SecondMonitor.Hub.Core.Ai
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SecondMonitor.Hub.Core.Race;
    using static SecondMonitor.Hub.Core.Ai.RadioFormat;

    /// <summary>
    /// Conseils batterie pour les voitures hybrides : F1 (ERS : modes de déploiement, récolte, 4 MJ par tour) et hypercars LMU
    /// (batterie + énergie virtuelle). Où déployer (les plus longues lignes droites du profil du circuit), où récolter (gros freinages),
    /// attaque / défense selon l'écart, dernier tour, ne pas finir le tour à vide.
    /// </summary>
    public static class HybridAdvisor
    {
        /// <summary>Lignes de la fiche de faits (sujet Batterie) : (texte, étiquette). La première est la réponse par défaut.</summary>
        public static IEnumerable<(string Text, string Tag)> Facts(RaceSnapshot snap, SpeedAnalysisDto speeds)
        {
            HybridDto h = snap?.Player?.Hybrid;
            DriverRow me = snap?.Drivers?.FirstOrDefault(d => d.IsPlayer);
            if (h == null || me == null)
            {
                if (snap?.HasData == true && me != null)
                {
                    yield return ("Pas de batterie transmise par le jeu pour cette voiture (non hybride, ou donnée absente).", null);
                }

                yield break;
            }

            yield return (State(snap, h) + " " + Advice(snap, me, h, speeds), null);
            yield return (Where(snap, h, speeds), "où");
        }

        /// <summary>« Batterie 64 %, mode moyen, elle se vide ; ce tour : 45 % du déploiement, 30 % récupérés. »</summary>
        public static string State(RaceSnapshot snap, HybridDto h)
        {
            var parts = new List<string> { $"Batterie {N(h.ChargePct, 0)} %" };
            string mode = ModeName(h);
            if (mode != null)
            {
                parts.Add($"mode {mode}");
            }

            if (h.LastLapStartPct is double s && h.LastLapEndPct is double e)
            {
                double net = e - s;
                parts.Add(Math.Abs(net) < 2 ? "bilan du dernier tour équilibré" : net > 0 ? $"+{N(net, 0)} % sur le dernier tour" : $"{N(net, 0)} % sur le dernier tour");
            }

            string text = string.Join(", ", parts) + ".";
            if (h.System == "LMU" && snap.Player?.Energy is { } en)
            {
                text += $" Énergie virtuelle {N(en.RemainingPct, 0)} %{(en.PerLapPct.HasValue ? $", {N(en.PerLapPct.Value, 1)} %/tour" : string.Empty)}.";
            }

            return text;
        }

        /// <summary>Un seul conseil, le plus utile maintenant.</summary>
        public static string Advice(RaceSnapshot snap, DriverRow me, HybridDto h, SpeedAnalysisDto speeds)
        {
            PlayerStrategy p = snap.Player;
            string straight = LongestStraights(speeds).FirstOrDefault()?.Name ?? "la ligne droite principale";
            string braking = HeavyBrakings(speeds).FirstOrDefault()?.Name ?? "le plus gros freinage";
            bool race = snap.SessionType == "Race";
            bool f1 = h.System == "F1";

            if (race && p?.LapsRemaining is double left && left <= 1.05)
            {
                return h.ChargePct > 15 ? "Dernier tour : vide la batterie, déploiement maximum." : "Dernier tour : il reste peu de batterie, garde-la pour la dernière ligne droite.";
            }

            if (snap.SessionType is "Qualification" && h.ChargePct < 90 && (p?.StintLaps ?? 0) == 0)
            {
                return f1 ? "Tour de sortie : mode aucun pour recharger à fond avant le tour lancé." : "Tour de sortie : recharge au maximum avant le tour lancé.";
            }

            if (h.LastLapEmptyAt is double empty)
            {
                string where = SegmentAt(speeds, empty)?.Name ?? $"{N(empty, 0)} m";
                return f1
                    ? $"Batterie vide au dernier tour vers {where.ToLowerInvariant()} : déploie moins tôt dans le tour pour en garder jusqu'à la ligne."
                    : $"Batterie vide vers {where.ToLowerInvariant()} au dernier tour : lève le pied plus tôt avant les gros freinages pour récupérer davantage.";
            }

            if (h.ChargePct < 20)
            {
                return f1
                    ? $"Batterie basse : mode aucun un tour pour recharger, et récolte au freinage du {braking.ToLowerInvariant()}."
                    : $"Batterie basse : lève le pied un peu plus tôt avant le {braking.ToLowerInvariant()} pour récupérer.";
            }

            BattleDto ahead = p?.Battles?.FirstOrDefault(b => b.Side == "ahead");
            BattleDto behind = p?.Battles?.FirstOrDefault(b => b.Side == "behind");
            if (race && ahead?.Gap is double ga && ga <= 1.2 && h.ChargePct >= 40)
            {
                return $"Attaque sur le numéro {ahead.Number} ({Sec(ga)}) : garde la batterie pour la sortie du virage avant la {straight.ToLowerInvariant().Replace(" (principale)", " principale")}{(f1 ? ", mode overtake" : string.Empty)}.";
            }

            if (race && behind?.Gap is double gb && gb <= 1.0 && h.ChargePct >= 30)
            {
                return $"Défense : le numéro {behind.Number} est à {Sec(gb)}, déploie en sortie des virages lents pour rester devant.";
            }

            if (h.ChargePct > 90 && h.LastLapEndPct >= h.LastLapStartPct)
            {
                return f1 ? $"Batterie pleine : tu peux déployer plus, mode moyen ou overtake dans la {straight.ToLowerInvariant().Replace(" (principale)", " principale")}." : "Batterie pleine : tu peux attaquer, elle se recharge bien.";
            }

            if (h.LastLapStartPct is double s && h.LastLapEndPct is double e && e - s <= -10)
            {
                double perLap = s - e;
                return $"Tu vides {N(perLap, 0)} % par tour : à ce rythme, à vide dans {N(Math.Max(1, Math.Floor(h.ChargePct / perLap)), 0)} tours. {(f1 ? "Passez en mode moyen ou bas." : "Récupérez un peu plus au freinage.")}";
            }

            return "Gestion correcte, continue comme ça.";
        }

        /// <summary>« Je déploie où ? » : les plus longues lignes droites, et où récolter.</summary>
        public static string Where(RaceSnapshot snap, HybridDto h, SpeedAnalysisDto speeds)
        {
            var straights = LongestStraights(speeds).Take(2).ToList();
            var brakings = HeavyBrakings(speeds).Take(2).ToList();
            if (straights.Count == 0)
            {
                return "Je n'ai pas encore le profil du circuit (il faut un tour complet) : déploie dans les plus longues lignes droites, récupère dans les gros freinages.";
            }

            double lapLength = snap?.Track?.Length > 0 ? snap.Track.Length : speeds.Segments.Select(s => s.End).DefaultIfEmpty(0).Max();
            string deploy = string.Join(" et ", straights.Select(s => $"{Clean(s.Name)} ({N(Length(s, lapLength), 0)} m)"));
            string harvest = brakings.Count == 0 ? "dans les gros freinages" : "au freinage du " + string.Join(" et du ", brakings.Select(b => b.Name.ToLowerInvariant()));
            string text = h?.System == "LMU"
                ? $"La batterie se vide en accélération : elle sert le plus dans {deploy}. Elle se recharge {harvest} ; lève le pied juste avant pour récupérer plus."
                : $"Déploie dans {deploy}, en sortie des virages lents. Récolte {harvest}.";
            return text;
        }

        /// <summary>Intervention spontanée liée à la batterie, ou null.</summary>
        public static ProactiveMessage Proactive(RaceSnapshot snap, DriverRow me)
        {
            HybridDto h = snap?.Player?.Hybrid;
            PlayerStrategy p = snap?.Player;
            if (h == null || me == null || p == null || me.InPits)
            {
                return null;
            }

            bool race = snap.SessionType == "Race" && me.Laps >= 1;
            bool f1 = h.System == "F1";
            if (race && p.LapsRemaining is double left && left <= 1.05 && left > 0.3 && h.ChargePct > 25)
            {
                return Info("ers-last", "Dernier tour : vide la batterie, déploiement maximum.", 100000, urgent: true);
            }

            BattleDto ahead = p.Battles?.FirstOrDefault(b => b.Side == "ahead");
            if (race && ahead?.Gap is double ga && ga <= 1.0 && h.ChargePct >= 50)
            {
                // Une clé par situation (attaque / défense), pas par adversaire : sinon une phrase par voiture croisée, chaque minute.
                return Info("ers-attack", $"Le numéro {ahead.Number} est à {Sec(ga)} : garde la batterie pour la ligne droite principale{(f1 ? ", mode overtake" : string.Empty)}.", 600);
            }

            BattleDto behind = p.Battles?.FirstOrDefault(b => b.Side == "behind");
            if (race && behind?.Gap is double gb && gb <= 0.8 && h.ChargePct >= 40)
            {
                return Info("ers-defend", $"Le numéro {behind.Number} est à {Sec(gb)} derrière : déploie en sortie des virages lents pour le garder derrière.", 600);
            }

            if (race && h.ChargePct < 12)
            {
                return Info("ers-low", f1 ? $"Batterie à {N(h.ChargePct, 0)} % : mode aucun un tour pour recharger." : $"Batterie à {N(h.ChargePct, 0)} % : lève le pied un peu plus tôt avant les freinages pour la recharger.", 300);
            }

            if (h.LastLapEmptyAt.HasValue && me.Laps >= 1)
            {
                return Info($"ers-empty-{me.Laps}", f1 ? "Batterie vide avant la fin du dernier tour : déploie un peu moins tôt pour en garder jusqu'à la ligne." : "Batterie vide avant la fin du dernier tour : récupère davantage au freinage.", 400);
            }

            if (snap.SessionType == "Qualification" && p.StintLaps == 0 && h.ChargePct < 80 && me.Speed > 40)
            {
                return Info($"ers-outlap-{me.Laps}", "Tour de sortie : recharge la batterie à fond avant le tour lancé.", 600);
            }

            return null;
        }

        public static string ModeName(HybridDto h)
        {
            if (h?.Mode == null || h.Mode == "UnAvailable")
            {
                return null;
            }

            if (h.System == "F1")
            {
                return h.Mode switch
                {
                    "Build" => "aucun (recharge)",
                    "Balanced" => "bas",
                    "Attack" => "moyen",
                    "Qualification" => "élevé",
                    _ => null,
                };
            }

            // LMU : le jeu ne donne pas de mode (toujours « équilibré ») ; on dit plutôt si elle se vide ou se recharge.
            return h.ChargeChange switch
            {
                "deploy" => "déploiement en cours",
                "harvest" => "en recharge",
                _ => null,
            };
        }

        private static IEnumerable<TrackSegment> LongestStraights(SpeedAnalysisDto speeds)
        {
            if (speeds?.Segments == null || speeds.Segments.Count == 0)
            {
                return Enumerable.Empty<TrackSegment>();
            }

            double lapLength = speeds.Segments.Select(s => s.End).DefaultIfEmpty(0).Max();
            return speeds.Segments.Where(s => s.Kind == "straight").OrderByDescending(s => Length(s, lapLength));
        }

        /// <summary>Virages avec la plus grosse chute de vitesse depuis la ligne droite qui précède.</summary>
        private static IEnumerable<TrackSegment> HeavyBrakings(SpeedAnalysisDto speeds)
        {
            if (speeds?.Segments == null || speeds.Segments.Count == 0)
            {
                return Enumerable.Empty<TrackSegment>();
            }

            var ordered = speeds.Segments.OrderBy(s => s.Apex).ToList();
            var drops = new List<(TrackSegment Corner, double Drop)>();
            for (int i = 0; i < ordered.Count; i++)
            {
                if (ordered[i].Kind != "corner")
                {
                    continue;
                }

                TrackSegment prev = Enumerable.Range(1, ordered.Count).Select(k => ordered[(i - k + ordered.Count) % ordered.Count]).FirstOrDefault(s => s.Kind == "straight");
                if (prev != null)
                {
                    drops.Add((ordered[i], prev.ReferenceSpeed - ordered[i].ReferenceSpeed));
                }
            }

            return drops.OrderByDescending(d => d.Drop).Select(d => d.Corner);
        }

        private static TrackSegment SegmentAt(SpeedAnalysisDto speeds, double distance)
        {
            if (speeds?.Segments == null || speeds.Segments.Count == 0)
            {
                return null;
            }

            return speeds.Segments.FirstOrDefault(s => s.Start <= s.End ? distance >= s.Start && distance <= s.End : distance >= s.Start || distance <= s.End)
                ?? speeds.Segments.OrderBy(s => Math.Abs(s.Apex - distance)).First();
        }

        private static double Length(TrackSegment s, double lapLength) => s.End >= s.Start ? s.End - s.Start : (lapLength - s.Start) + s.End;

        private static string Clean(string name) => name.Replace(" (principale)", " principale").ToLowerInvariant().Replace("ligne droite", "la ligne droite");

        private static ProactiveMessage Info(string key, string text, double cooldown, bool urgent = false) => new() { Key = key, Topic = "batterie", Text = text, CooldownSeconds = cooldown, Urgent = urgent };
    }
}
