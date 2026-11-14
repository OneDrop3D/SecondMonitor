namespace SecondMonitor.Hub.Core.Ai
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using static SecondMonitor.Hub.Core.Ai.RaceBriefing;

    /// <summary>
    /// Spécialistes de l'ingénieur : un domaine par question (pneus, stratégie, voiture, course). Chacun ne reçoit que
    /// les faits et les connaissances de son domaine, sans l'historique des autres sujets : un contexte court,
    /// donc une réponse plus rapide et plus juste, puis le contexte est vidé jusqu'à la question suivante.
    /// Chaque domaine peut utiliser son propre modèle (réglages : AiSettings.Specialists).
    /// </summary>
    public static class Specialists
    {
        public const string Tyres = "pneus";
        public const string Strategy = "stratégie";
        public const string Car = "voiture";
        public const string Race = "course";

        public static readonly string[] All = { Tyres, Strategy, Car, Race };

        private static readonly Dictionary<string, Topic[]> Domains = new()
        {
            [Tyres] = new[] { Topic.Tyres, Topic.Rain, Topic.Weather },
            [Strategy] = new[] { Topic.Pit, Topic.Strategy, Topic.Fuel, Topic.Energy, Topic.Hybrid, Topic.Weather, Topic.Rain, Topic.Session, Topic.Opponents, Topic.Damage },
            [Car] = new[] { Topic.Controls, Topic.Car, Topic.Driving, Topic.Damage, Topic.Speed, Topic.Performance, Topic.Tyres, Topic.Hybrid, Topic.Energy },
            [Race] = new[] { Topic.Position, Topic.Gap, Topic.Pace, Topic.Laps, Topic.Session, Topic.Rules, Topic.Performance, Topic.Opponents },
        };

        /// <summary>Domaine de la question : celui du premier sujet reconnu (voiture par défaut pour une remarque sur le comportement).</summary>
        public static string For(IReadOnlyList<Topic> topics)
        {
            Topic? first = topics?.Cast<Topic?>().FirstOrDefault();
            return first switch
            {
                Topic.Tyres or Topic.Rain => Tyres,
                Topic.Pit or Topic.Strategy or Topic.Fuel or Topic.Energy or Topic.Hybrid or Topic.Weather or Topic.Opponents => Strategy,
                Topic.Position or Topic.Gap or Topic.Pace or Topic.Laps or Topic.Session or Topic.Rules => Race,
                _ => Car,
            };
        }

        public static string Label(string domain) => domain switch
        {
            Tyres => "les pneus",
            Strategy => "la stratégie",
            Race => "la course",
            _ => "la voiture",
        };

        /// <summary>Rôle et connaissances du spécialiste (ajoutés à la consigne commune).</summary>
        public static string Role(string domain) => domain switch
        {
            Tyres =>
                "TU ES LE SPÉCIALISTE PNEUS. Températures de surface (intérieur, milieu, extérieur), carcasse, pressions à chaud, usure. " +
                "Pneu trop chaud : glisse, graining puis usure ; trop froid : pas de grip, blocages. Intérieur bien plus chaud que l'extérieur : trop de carrossage ; " +
                "milieu plus chaud que les bords : pression trop haute. Arrière qui chauffe : patinage en sortie (TC, accélérateur) ; avant qui chauffe : sous-virage, freinages appuyés.",
            Strategy =>
                "TU ES LE SPÉCIALISTE STRATÉGIE. Arrêts, carburant, énergie virtuelle, batterie, météo, undercut et overcut. " +
                "L'énergie virtuelle et l'essence se remettent à chaque arrêt : celle qui finit en premier fixe le relais. Les calculs sont déjà faits : recopie-les.",
            Race =>
                "TU ES LE SPÉCIALISTE COURSE. Position, écarts, rythme, tours, règlement (limites de piste, pénalités). Le classement qui compte est celui de la catégorie du pilote.",
            _ =>
                "TU ES LE SPÉCIALISTE VOITURE. Équilibre, réglages en roulant (répartition, migration, TC, ABS, cartographie, regen), pilotage (pédales, freinages, sorties), dégâts. " +
                "Propose d'abord un réglage en roulant avec la valeur actuelle et la nouvelle (« TC de 4 à 5 »), puis le setup au stand. Ne propose jamais un réglage que la voiture n'a pas. " +
                string.Join(" ", CarKnowledge.ControlHelp.Values),
        };

        /// <summary>Faits du domaine seulement (plus la voiture et la mémoire de course).</summary>
        public static string Facts(string domain, BriefingResult brief)
        {
            var sb = new StringBuilder("FAITS (chiffres exacts calculés par l'ordinateur) :");
            var topics = (brief.Topics ?? new List<Topic>()).Concat(Domains.TryGetValue(domain, out Topic[] d) ? d : new Topic[0]).Distinct();
            foreach (Topic t in topics)
            {
                if (brief.Sections != null && brief.Sections.TryGetValue(t, out List<string> lines))
                {
                    foreach (string line in lines)
                    {
                        sb.Append("\n- ").Append(line);
                    }
                }
            }

            if (brief.CarFacts != null)
            {
                sb.Append("\n- ").Append(brief.CarFacts);
            }

            if (brief.MemoryFacts != null)
            {
                sb.Append("\n\n").Append(brief.MemoryFacts);
            }

            return sb.ToString();
        }
    }
}
