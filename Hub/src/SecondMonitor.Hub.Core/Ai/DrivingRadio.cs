namespace SecondMonitor.Hub.Core.Ai
{
    using System.Collections.Generic;
    using System.Linq;
    using SecondMonitor.Hub.Core.Race;
    using static SecondMonitor.Hub.Core.Ai.RadioFormat;

    /// <summary>
    /// Phrases radio des événements de pilotage (<see cref="DrivingWatch"/>) : très courtes, dites tout de suite
    /// (limites de piste, temps annulé, pénalité, départ), et l'analyse d'une usure brutale avec le réglage à essayer
    /// et une question au pilote (« Qu'est-ce qui s'est passé ? »).
    /// </summary>
    public static class DrivingRadio
    {
        /// <summary>Message radio pour un lot d'événements arrivés ensemble (limites + temps annulé = une seule phrase).</summary>
        public static List<RadioLine> Lines(IReadOnlyList<DrivingEvent> events, RaceSnapshot snap)
        {
            var lines = new List<RadioLine>();
            if (events == null || events.Count == 0)
            {
                return lines;
            }

            bool invalidated = events.Any(e => e.Type == "tour-annulé");
            DrivingEvent limits = events.LastOrDefault(e => e.Type == "limites");
            if (limits != null)
            {
                lines.Add(new RadioLine(Limits(limits, invalidated), "limites", false));
            }
            else if (invalidated)
            {
                DrivingEvent inv = events.First(e => e.Type == "tour-annulé");
                lines.Add(new RadioLine(snap?.SessionType == "Race" ? $"Tour annulé{At(inv.Corner)}." : $"Temps annulé{At(inv.Corner)}.", "limites", false));
            }

            foreach (DrivingEvent e in events.Where(e => e.Type == "pénalité"))
            {
                int n = (int)(e.Points ?? 1);
                lines.Add(new RadioLine(n > 1 ? $"Pénalité reçue : {n} à purger." : "Pénalité reçue, à purger.", "pénalité", false));
            }

            foreach (DrivingEvent e in events.Where(e => e.Type == "départ"))
            {
                lines.Add(new RadioLine(e.Detail == "moteur" ? "Moteur coupé : démarre le moteur." : "Moteur prêt : passe la première.", "départ", false));
            }

            foreach (DrivingEvent e in events.Where(e => e.Type == "usure" && e.Spike != null))
            {
                lines.Add(new RadioLine(Wear(e.Spike, snap), "usure", true));
            }

            return lines;
        }

        private static string Limits(DrivingEvent e, bool invalidated)
        {
            string where = e.Corner == null ? string.Empty : $" {e.Corner.ToLowerInvariant()}";
            string what = e.Detail == "coupé" ? "coupé, lève un peu le pied" : "trop large";
            string text = $"Track limits{where} : {what}.";
            if (invalidated)
            {
                text += " Temps annulé.";
            }

            if (e.Points is double pts && e.PointsPerPenalty is double max && max > 0)
            {
                double left = max - pts;
                text += left <= 1.01
                    ? $" {N(pts, 0)} sur {N(max, 0)} : la prochaine, c'est la pénalité."
                    : $" {N(pts, 0)} sur {N(max, 0)}.";
            }

            return text;
        }

        /// <summary>« Usure anormale arrière droit : plus 2,5 % en 3 s, virage 5, en sortie. Patinage probable : TC de 4 à 5. Qu'est-ce qui s'est passé ? »</summary>
        public static string Wear(WearSpike s, RaceSnapshot snap)
        {
            CarContext ctx = CarContext.From(snap);
            var (cause, proposal) = CarKnowledge.AnalyseWear(s, snap?.Player?.Controls, ctx);
            string where = s.Corner == null ? string.Empty : $", {s.Corner.ToLowerInvariant()}";
            string phase = s.Phase == "ligne droite" ? string.Empty : $", en {(s.Phase == "freinage" ? "freinage" : s.Phase == "milieu" ? "milieu de virage" : s.Phase + " de virage")}";
            string text = $"Usure anormale {Wheel(s.Wheel)} : plus {N(s.DeltaPct, 1)} % en {N(s.Seconds, 0)} secondes{where}{phase}. {Cap(cause)}.";
            if (proposal != null)
            {
                text += $" Essaie : {proposal}.";
            }

            return text + " Qu'est-ce qui s'est passé ?";
        }

        private static string At(string corner) => corner == null ? string.Empty : $", {corner.ToLowerInvariant()}";
    }

    /// <summary>Une phrase radio : texte, sujet, et si elle attend une réponse du pilote.</summary>
    public sealed record RadioLine(string Text, string Topic, bool IsQuestion);
}
