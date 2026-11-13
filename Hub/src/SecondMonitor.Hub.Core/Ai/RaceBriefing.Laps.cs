namespace SecondMonitor.Hub.Core.Ai
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SecondMonitor.Hub.Core.Race;
    using static SecondMonitor.Hub.Core.Ai.RadioFormat;

    /// <summary>Réponses chiffrées instantanées : meilleur / pire / dernier tour, moyenne, régularité, vitesse de pointe.</summary>
    public static partial class RaceBriefing
    {
        /// <summary>Écart de Vmax (km/h) avec le plus rapide de la catégorie à partir duquel l'ingénieur propose quelque chose.</summary>
        public const double TopSpeedDeficitKph = 4;

        /// <summary>Tours « propres » : ni passage au stand, ni premier tour de course (départ arrêté ou lancé).</summary>
        public static List<LapDto> CleanLaps(IEnumerable<LapDto> laps, bool race) =>
            (laps ?? Enumerable.Empty<LapDto>())
                .Where(l => l.Time is > 0 && !l.Pit && !(race && l.Lap <= 1))
                .ToList();

        private static void AddLapFacts(RaceSnapshot snap, DriverRow me, BriefingExtras extras, Action<Topic, string, string> add)
        {
            bool race = snap.SessionType == "Race";
            List<LapDto> all = extras.Laps ?? snap.Player?.RecentLaps ?? new List<LapDto>();
            List<LapDto> clean = CleanLaps(all, race);
            double? best = me.BestLap is > 0 ? me.BestLap : clean.Where(l => l.Valid).Select(l => l.Time).DefaultIfEmpty(null).Min();
            if (best is not double b)
            {
                add(Topic.Laps, "Pas encore de tour complet chronométré : il faut boucler un tour.", null);
                return;
            }

            int? bestLap = me.BestLapNumber ?? clean.Where(l => l.Valid).OrderBy(l => l.Time).FirstOrDefault()?.Lap;
            string at = bestLap.HasValue ? $" au tour {bestLap}" : string.Empty;

            // Meilleur tour de la catégorie (pour situer le vôtre).
            DriverRow classBest = snap.Drivers.Where(d => d.ClassId == me.ClassId && d.BestLap > 0).OrderBy(d => d.BestLap).FirstOrDefault();
            string vsClass = classBest == null ? string.Empty
                : classBest.Id == me.Id ? " : meilleur temps de la catégorie"
                : $", à {Sec(b - classBest.BestLap.Value)} du meilleur de la catégorie (numéro {classBest.Number}, {Lap(classBest.BestLap)})";

            LapDto last = all.LastOrDefault(l => l.Time is > 0);
            double? lastTime = me.LastLap is > 0 ? me.LastLap : last?.Time;
            int? lastNumber = last?.Lap ?? (me.Laps > 0 ? me.Laps : null);
            var recent = clean.TakeLast(10).ToList();
            double? avg = recent.Count >= 2 ? recent.Average(l => l.Time.Value) : null;
            double? sd = recent.Count >= 3 ? Math.Sqrt(recent.Sum(l => Math.Pow(l.Time.Value - avg.Value, 2)) / recent.Count) : null;
            LapDto worst = clean.Count >= 2 ? clean.OrderByDescending(l => l.Time).First() : null;

            // Résumé (« mes tours ? ») puis une ligne par sous-question.
            var summary = new List<string> { $"Meilleur tour {Lap(b)}{at}" };
            if (lastTime is double lt && lastTime != b)
            {
                summary.Add($"dernier {Lap(lt)} (+{Sec(lt - b)})");
            }

            if (avg is double a)
            {
                summary.Add($"moyenne {Lap(a)} sur {recent.Count} tours");
            }

            add(Topic.Laps, string.Join(", ", summary) + ".", null);
            add(Topic.Laps, $"Meilleur tour {Lap(b)}{at}{vsClass}.", "meilleur");

            if (worst != null)
            {
                add(Topic.Laps, $"Pire tour {Lap(worst.Time)} au tour {worst.Lap}, à {Sec(worst.Time.Value - b)} de votre meilleur{(worst.Valid ? string.Empty : " (tour invalidé)")}{(race ? " ; hors premier tour et passages au stand" : " ; hors passages au stand")}.", "pire");
            }
            else
            {
                add(Topic.Laps, $"Pas encore assez de tours pour un pire tour : seulement {clean.Count} tour{(clean.Count > 1 ? "s" : string.Empty)} propre{(clean.Count > 1 ? "s" : string.Empty)}{(race ? " (le premier tour et les passages au stand ne comptent pas)" : string.Empty)}.", "pire");
            }

            if (lastTime is double l2)
            {
                string delta = Math.Abs(l2 - b) < 0.0005 ? " : c'est votre meilleur tour" : $", à {Sec(l2 - b)} de votre meilleur";
                add(Topic.Laps, $"Dernier tour {Lap(l2)}{(lastNumber.HasValue ? $" (tour {lastNumber})" : string.Empty)}{delta}.", "dernier");
            }

            if (avg is double a2)
            {
                add(Topic.Laps, $"Moyenne {Lap(a2)} sur vos {recent.Count} derniers tours propres, à {Sec(a2 - b)} de votre meilleur.", "moyenne");
            }
            else
            {
                add(Topic.Laps, "Pas encore de moyenne : il faut au moins deux tours propres.", "moyenne");
            }

            if (sd is double s)
            {
                string verdict = s < 0.3 ? "très régulier" : s < 0.6 ? "régulier" : s < 1.0 ? "un peu irrégulier" : "irrégulier";
                double spread = recent.Max(l => l.Time.Value) - recent.Min(l => l.Time.Value);
                add(Topic.Laps, $"Régularité : plus ou moins {Sec(s)} sur {recent.Count} tours, {verdict} ; {Sec(spread)} entre le plus rapide et le plus lent.", "régularité");
            }
            else
            {
                add(Topic.Laps, "Régularité : il faut au moins trois tours propres pour la mesurer.", "régularité");
            }
        }

        /// <summary>
        /// Vitesse de pointe : « Vitesse max 204 km/h au tour 7, en moyenne 202 km/h ; le plus rapide est le numéro 52 à 208 km/h »,
        /// et en cas de déficit, un conseil (sortie du virage précédent, ou un cran d'aileron). Retourne la proposition faite, s'il y en a une.
        /// </summary>
        private static Proposal AddSpeedFacts(RaceSnapshot snap, DriverRow me, SpeedAnalysisDto speeds, Action<Topic, string, bool> add)
        {
            DriverSpeedDto mine = speeds?.Drivers.FirstOrDefault(d => d.Id == me.Id);
            if (mine?.TopSpeedBest == null)
            {
                add(Topic.Speed, "Vitesse de pointe pas encore mesurée : il faut un tour complet sans passage au stand.", false);
                return null;
            }

            var classIds = snap.Drivers.Where(d => d.ClassId == me.ClassId).Select(d => d.Id).ToHashSet();
            var rivals = speeds.Drivers.Where(d => classIds.Contains(d.Id) && d.Id != me.Id && d.TopSpeedBest.HasValue).ToList();
            DriverSpeedDto fastest = rivals.OrderByDescending(d => d.TopSpeedBest).FirstOrDefault();
            double top = mine.TopSpeedBest.Value;
            string text = $"Vitesse max {Kph(top)}{(mine.TopSpeedBestLap.HasValue ? $" au tour {mine.TopSpeedBestLap}" : string.Empty)}";
            if (mine.TopSpeedAverage is double avg && mine.LapsAnalysed >= 2)
            {
                text += $", en moyenne {Kph(avg)}";
            }

            double deficit = 0;
            if (fastest != null)
            {
                DriverRow f = snap.Drivers.FirstOrDefault(d => d.Id == fastest.Id);
                deficit = fastest.TopSpeedBest.Value - top;
                text += deficit > 0.5
                    ? $" ; le plus rapide de la catégorie est le numéro {f?.Number} à {Kph(fastest.TopSpeedBest.Value)}."
                    : $" ; c'est la meilleure de la catégorie, le numéro {f?.Number} suit à {Kph(fastest.TopSpeedBest.Value)}.";
            }
            else
            {
                text += ".";
            }

            add(Topic.Speed, text, false);

            // Lignes droites et virages où l'écart avec le meilleur de la catégorie est le plus grand.
            var gaps = new List<(TrackSegment Seg, double Mine, double Best)>();
            foreach (TrackSegment seg in speeds.Segments)
            {
                if (!mine.Best.TryGetValue(seg.Id, out double v))
                {
                    continue;
                }

                double best = rivals.Select(r => r.Best.TryGetValue(seg.Id, out double rv) ? rv : 0).DefaultIfEmpty(0).Max();
                gaps.Add((seg, v, Math.Max(best, v)));
            }

            var straights = gaps.Where(g => g.Seg.Kind == "straight").ToList();
            if (straights.Count > 0)
            {
                add(Topic.Speed, "Vmax par ligne droite (vous / meilleur de la catégorie) : " + string.Join(", ", straights.Select(g => $"{g.Seg.Name.Replace(" (principale)", string.Empty)} {N(g.Mine, 0)} / {N(g.Best, 0)}")) + " km/h.", false);
            }

            var corners = gaps.Where(g => g.Seg.Kind == "corner").OrderByDescending(g => g.Best - g.Mine).ToList();
            if (corners.Count > 0)
            {
                var worst = corners[0];
                add(Topic.Speed, worst.Best - worst.Mine < 1
                    ? "En virage, vos vitesses de passage sont au niveau des meilleurs de la catégorie."
                    : $"Virage où vous passez le moins vite : {worst.Seg.Name.ToLowerInvariant()}, {N(worst.Mine, 0)} contre {Kph(worst.Best)}.", false);
            }

            if (deficit < TopSpeedDeficitKph)
            {
                return null;
            }

            // Déficit : la ligne droite où l'on perd le plus, et la sortie du virage qui la précède.
            var straight = straights.Where(g => g.Best - g.Mine >= 2).OrderByDescending(g => g.Best - g.Mine).FirstOrDefault();
            if (straight.Seg != null)
            {
                var ordered = speeds.Segments.OrderBy(s => s.Apex).ToList();
                int i = ordered.IndexOf(straight.Seg);
                TrackSegment before = Enumerable.Range(1, ordered.Count).Select(k => ordered[(i - k + ordered.Count) % ordered.Count]).FirstOrDefault(s => s.Kind == "corner");
                var exit = before == null ? default : gaps.FirstOrDefault(g => g.Seg == before);
                string straightName = straight.Seg.Name.Replace(" (principale)", " principale").ToLowerInvariant();
                if (exit.Seg != null && exit.Best - exit.Mine >= 3)
                {
                    add(Topic.Speed, $"Vous perdez {N(straight.Best - straight.Mine, 0)} km/h dans la {straightName} : vous sortez moins vite du {before.Name.ToLowerInvariant()} ({N(exit.Mine, 0)} contre {N(exit.Best, 0)}), soignez cette sortie.", true);
                    return new Proposal { Key = "exit-" + before.Id, Kind = "pilotage", Infinitive = $"soigner la sortie du {before.Name.ToLowerInvariant()}", Imperative = $"soignez la sortie du {before.Name.ToLowerInvariant()}", Note = $"sortie du {before.Name.ToLowerInvariant()} à soigner" };
                }

                if (snap.Weather.Wetness < 10)
                {
                    add(Topic.Speed, $"Vous perdez {N(straight.Best - straight.Mine, 0)} km/h dans la {straightName} avec une sortie de virage au niveau : c'est l'appui. Un cran d'aileron arrière en moins au prochain arrêt ?", true);
                    return new Proposal { Key = "wing-rear-less", Kind = "setup", Infinitive = "mettre un cran d'aileron arrière en moins", Imperative = "mettez un cran d'aileron arrière en moins", Note = "un cran d'aileron arrière en moins" };
                }
            }
            else if (snap.Weather.Wetness < 10)
            {
                add(Topic.Speed, $"{Kph(deficit)} de moins que le plus rapide : moins d'aileron arrière au prochain arrêt, ou une meilleure sortie des virages lents.", true);
                return new Proposal { Key = "wing-rear-less", Kind = "setup", Infinitive = "mettre un cran d'aileron arrière en moins", Imperative = "mettez un cran d'aileron arrière en moins", Note = "un cran d'aileron arrière en moins" };
            }

            return null;
        }

        /// <summary>
        /// Quand ni le hub ni le modèle n'ont de réponse : quelle donnée manque et pourquoi, en une phrase.
        /// </summary>
        public static string Missing(string question, RaceSnapshot snap, BriefingExtras extras = null)
        {
            string q = RadioCommands.Normalize(question);
            DriverRow me = snap?.Drivers?.FirstOrDefault(d => d.IsPlayer);
            if (snap == null || !snap.HasData)
            {
                return "Je n'ai pas encore de données : le jeu n'envoie rien pour l'instant.";
            }

            if (System.Text.RegularExpressions.Regex.IsMatch(q, @"\b(batterie|ers|deploi\w*|recolte|hybride|overtake|boost)\b"))
            {
                return snap.Player?.Hybrid == null
                    ? "Pas d'information de batterie : le jeu n'en envoie pas pour cette voiture (non hybride, ou donnée absente)."
                    : "Pour la batterie, je n'ai que la charge et son évolution sur le tour.";
            }

            if (System.Text.RegularExpressions.Regex.IsMatch(q, @"\b(aileron|carross\w*|barre|ressort|amortisseur|hauteur|differentiel|repartition|abs|tc|cartographie|setup)\b") && !q.Contains("degat"))
            {
                return "Le jeu ne m'envoie pas les réglages de la voiture : je vois la télémétrie, pas le setup.";
            }

            if (System.Text.RegularExpressions.Regex.IsMatch(q, @"\b(son|ses|leur|leurs|il a|ils ont)\b.*\b(essence|carburant|fuel|usure|setup|pression)\b"))
            {
                return "Le jeu ne donne pas l'essence ni l'usure des adversaires : seulement leurs pneus, leurs arrêts et leurs temps.";
            }

            if (System.Text.RegularExpressions.Regex.IsMatch(q, @"\b(tour|tours|chrono|temps)\b") && me?.BestLap is not > 0)
            {
                return "Pas encore de tour complet chronométré : il faut boucler un tour.";
            }

            if (System.Text.RegularExpressions.Regex.IsMatch(q, @"\b(vitesse|vmax|pointe)\b"))
            {
                return "Vitesse de pointe pas encore mesurée : il faut un tour complet sans passage au stand.";
            }

            if (System.Text.RegularExpressions.Regex.IsMatch(q, @"\b(meteo|pluie|temperature de piste|vent)\b") && snap.Weather?.Air == null)
            {
                return "Le jeu ne transmet pas la météo dans cette session.";
            }

            if (System.Text.RegularExpressions.Regex.IsMatch(q, @"\b(pression|pressions)\b") && snap.Player?.Wheels?.All(w => w.Pressure == null) != false)
            {
                return "Le jeu n'envoie pas les pressions des pneus.";
            }

            return "Je n'ai pas de donnée pour répondre à ça : ni le jeu ni mes calculs ne la fournissent.";
        }
    }
}
