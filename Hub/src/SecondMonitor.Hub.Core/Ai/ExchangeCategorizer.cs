namespace SecondMonitor.Hub.Core.Ai
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Classe chaque échange radio dans une catégorie du carnet : règles d'abord (mots de la question, sujet reconnu),
    /// le modèle seulement en secours (voir <see cref="Parse"/>).
    /// </summary>
    public static class ExchangeCategorizer
    {
        public const string Strategy = "Stratégie";
        public const string RaceSettings = "Réglages de course";
        public const string CarSetup = "Réglages de la voiture";
        public const string RaceInfo = "Informations course";

        public static readonly string[] All = { Strategy, RaceSettings, CarSetup, RaceInfo };

        private static readonly Regex SetupWords = new(@"\b(aileron|carross\w*|barres?|antiroulis|anti roulis|ressorts?|amortisseurs?|hauteurs?|hauteur de caisse|pressions?|precharge|setup|set up|reglages? de la voiture|ecopes?|pincement|parallelisme|raideur|rake|sous ?vir\w*|survir\w*|rebonds?)\b", RegexOptions.Compiled);

        private static readonly Regex RaceSettingWords = new(@"\b(repartition|abs|tc|antipatinage|traction control|cartographie|carto|map|ers|batterie|deploi\w*|deploy\w*|recolte\w*|recuper\w*|regen|overtake|mode moteur|frein moteur|bloqu\w*|blocage|patin\w*|differentiel a l acceleration|brake bias|hybride)\b", RegexOptions.Compiled);

        private static readonly Regex StrategyWords = new(@"\b(essence|carburant|fuel|litres?|conso\w*|pneus?|gommes?|usure|stand|box|boxe|rentre\w*|arret|arrets|relais|undercut|overcut|ravitaill\w*|strategie|energie|pluie|wets?|inters?|slicks?|plein)\b", RegexOptions.Compiled);

        /// <summary>Catégorie d'après les mots de la question (null si rien de clair).</summary>
        public static string FromText(string text)
        {
            string t = RadioCommands.Normalize(text);
            if (t.Length == 0)
            {
                return null;
            }

            if (SetupWords.IsMatch(t))
            {
                return CarSetup;
            }

            if (RaceSettingWords.IsMatch(t))
            {
                return RaceSettings;
            }

            if (StrategyWords.IsMatch(t))
            {
                return Strategy;
            }

            return null;
        }

        public static string FromTopic(RaceBriefing.Topic topic) => topic switch
        {
            RaceBriefing.Topic.Pit or RaceBriefing.Topic.Fuel or RaceBriefing.Topic.Energy or RaceBriefing.Topic.Strategy
                or RaceBriefing.Topic.Rain or RaceBriefing.Topic.Opponents or RaceBriefing.Topic.Tyres or RaceBriefing.Topic.Damage => Strategy,
            RaceBriefing.Topic.Hybrid => RaceSettings,
            _ => RaceInfo,
        };

        /// <summary>Catégorie d'un échange question / réponse : règles d'abord, null si aucune règle ne tranche.</summary>
        public static string Categorize(string question, IReadOnlyList<RaceBriefing.Topic> topics)
        {
            string byText = FromText(question);
            if (topics != null && topics.Count > 0)
            {
                string byTopic = FromTopic(topics[0]);
                // Le sujet reconnu l'emporte, sauf si la question parle explicitement de setup (« mes pressions sont bonnes ? »).
                return byText == CarSetup ? CarSetup : byTopic;
            }

            return byText;
        }

        /// <summary>Catégorie d'une intervention spontanée de l'IA, d'après son sujet.</summary>
        public static string FromProactiveTopic(string topic) => topic switch
        {
            "stand" or "relais" or "stratégie" or "pluie" or "dégâts" => Strategy,
            "batterie" or "freins" => RaceSettings,
            "essais" => CarSetup,
            "pneus" => Strategy,
            _ => RaceInfo,
        };

        /// <summary>Réponse du modèle (« Réglages de course. ») ramenée à une catégorie connue, ou null.</summary>
        public static string Parse(string modelOutput)
        {
            string t = RadioCommands.Normalize(modelOutput);
            if (t.Length == 0)
            {
                return null;
            }

            if (t.Contains("voiture") || t.Contains("setup"))
            {
                return CarSetup;
            }

            if (t.Contains("reglages de course") || t.Contains("reglage de course") || t.Contains("reglages"))
            {
                return RaceSettings;
            }

            if (t.Contains("strateg"))
            {
                return Strategy;
            }

            if (t.Contains("information"))
            {
                return RaceInfo;
            }

            return null;
        }

        public static string Prompt(string question, string answer) =>
            "Classe cet échange radio de course dans UNE seule catégorie parmi : Stratégie (essence, pneus, arrêts au stand), " +
            "Réglages de course (ce qui se change en roulant : répartition de freinage, ABS, TC, cartographie, batterie), " +
            "Réglages de la voiture (setup au stand : sous-virage, survirage, ailerons, barres, pressions), Informations course (positions, écarts, chronos, météo). " +
            $"Réponds seulement par le nom de la catégorie.\nPilote : {question}\nIngénieur : {answer}";

        public static IEnumerable<string> Ordered => All.AsEnumerable();
    }
}
