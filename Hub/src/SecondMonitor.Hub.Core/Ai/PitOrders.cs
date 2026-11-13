namespace SecondMonitor.Hub.Core.Ai
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using SecondMonitor.Hub.Core.Race;

    /// <summary>Ce que le pilote demande pour son prochain arrêt.</summary>
    public sealed class PitOrder
    {
        /// <summary>Le pilote annonce qu'il rentre maintenant (« box », « je rentre »).</summary>
        public bool BoxNow { get; set; }

        /// <summary>Gomme demandée (« soft », « medium », « hard », « pluie », « neufs » sans gomme), ou null.</summary>
        public string Tyres { get; set; }

        /// <summary>Pas de changement de pneus.</summary>
        public bool NoTyres { get; set; }

        public bool Repair { get; set; }

        /// <summary>Carburant / énergie : « plein », « jusqu'à la fin », ou null.</summary>
        public string Fuel { get; set; }

        public bool IsEmpty => !BoxNow && Tyres == null && !NoTyres && !Repair && Fuel == null;
    }

    /// <summary>
    /// Demandes d'arrêt au stand dites à la radio : « je boxe », « je veux des softs neufs », « réparation », « fais le plein ».
    /// La demande est confirmée en une phrase et gardée dans la mémoire de course (le menu des stands du jeu se règle
    /// encore dans le jeu : l'ingénieur rappelle ce qu'il faut y mettre).
    /// </summary>
    public static class PitOrders
    {
        private static readonly Regex Request = new(
            @"\b(je (veux|voudrais|prends)|on (met|mets|prend|fait)|mets moi|mets nous|donne moi|il me faut|prepare|prevois|au prochain (arret|passage|box)|quand je (vais )?box\w*|quand je rentre|pour le box|pour l arret)\b",
            RegexOptions.Compiled);

        private static readonly Regex Box = new(@"\b(box box|je box\w*|je rentre|on rentre|je rentre au stand|je passe au stand|box ce tour|je vais au stand|l equipe je box\w*)\b", RegexOptions.Compiled);

        /// <summary>Retourne la demande, ou null si la phrase ne parle pas d'un arrêt à préparer.</summary>
        public static PitOrder Parse(string text)
        {
            string t = RadioCommands.Normalize(text);
            if (t.Length == 0 || RadioCommands.IsQuestion(text) && !Request.IsMatch(t))
            {
                return null;
            }

            var order = new PitOrder { BoxNow = Box.IsMatch(t) };
            bool request = Request.IsMatch(t) || order.BoxNow;
            string compound = Regex.Match(t, @"\b(softs?|tendres?|mediums?|mediu?ms?|durs?|hards?|pluies?|wets?|inters?|intermediaires?)\b").Value;
            bool tyreWords = Regex.IsMatch(t, @"\b(pneus?|gommes?|trains?|softs?|mediums?|hards?|wets?|inters?)\b");
            if (Regex.IsMatch(t, @"\b(pas de pneus|sans pneus|garde (les|mes) pneus|on garde les pneus|pas besoin de pneus)\b"))
            {
                order.NoTyres = true;
            }
            else if (tyreWords && (request || Regex.IsMatch(t, @"\b(neufs?|nouveaux?|nouvelles?|frais)\b")))
            {
                order.Tyres = compound switch
                {
                    "" => "neufs",
                    var c when c.StartsWith("soft") || c.StartsWith("tendre") => "Soft",
                    var c when c.StartsWith("med") => "Medium",
                    var c when c.StartsWith("dur") || c.StartsWith("hard") => "Hard",
                    var c when c.StartsWith("inter") => "Intermédiaires",
                    _ => "Pluie",
                };
            }

            if (Regex.IsMatch(t, @"\b(repar\w*|repare|reparation|reparer|rachat de la voiture|degats? a reparer)\b") && (request || Regex.IsMatch(t, @"\b(aussi|et|la)\b")))
            {
                order.Repair = true;
            }

            if (Regex.IsMatch(t, @"\b(le plein|fais le plein|plein d essence|plein d energie)\b"))
            {
                order.Fuel = "plein";
            }
            else if (request && Regex.IsMatch(t, @"\b(jusqu a la fin|pour finir|assez pour finir)\b"))
            {
                order.Fuel = "jusqu'à la fin";
            }

            return order.IsEmpty ? null : order;
        }

        /// <summary>Réponse radio et note de mémoire. Le plan d'arrêt est fusionné avec la demande précédente (pneus puis réparation).</summary>
        public static string Confirm(PitOrder order, RaceMemory memory, RaceSnapshot snap, int? lap)
        {
            PitOrder plan = memory?.PitPlan ?? new PitOrder();
            plan.Tyres = order.Tyres ?? (order.NoTyres ? null : plan.Tyres);
            plan.NoTyres = order.NoTyres || (plan.NoTyres && order.Tyres == null);
            plan.Repair |= order.Repair;
            plan.Fuel = order.Fuel ?? plan.Fuel;
            if (memory != null)
            {
                memory.PitPlan = plan;
                if (order.BoxNow && lap.HasValue)
                {
                    memory.PlannedStopLap = lap + 1;
                }
            }

            var parts = new List<string>();
            if (plan.Tyres != null)
            {
                parts.Add(plan.Tyres == "neufs" ? "pneus neufs" : $"{plan.Tyres} neufs");
            }
            else if (plan.NoTyres)
            {
                parts.Add("on garde les pneus");
            }

            if (plan.Repair)
            {
                bool damaged = snap?.Player?.Damage?.Any(d => d.Percent >= 1) == true;
                parts.Add(damaged ? "réparation" : "réparation (aucun dégât signalé pour l'instant)");
            }

            if (plan.Fuel != null)
            {
                parts.Add(plan.Fuel == "plein" ? "le plein" : "carburant jusqu'à la fin");
            }

            string list = parts.Count == 0 ? string.Empty : " " + RadioFormat.Cap(string.Join(", ", parts)) + ".";
            string box = order.BoxNow ? "Copy, box ce tour." : "Copy, noté pour l'arrêt.";
            memory?.Add("pilote", "stand", $"demande pour l'arrêt :{list}".TrimEnd(), lap);
            return box + list + (snap?.Source?.Contains("LM Ultimate", System.StringComparison.OrdinalIgnoreCase) == true && parts.Count > 0
                ? " Vérifiez le menu des stands avant l'entrée."
                : string.Empty);
        }
    }
}
