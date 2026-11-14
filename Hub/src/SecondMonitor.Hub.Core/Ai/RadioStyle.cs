namespace SecondMonitor.Hub.Core.Ai
{
    using System.Text.RegularExpressions;

    /// <summary>
    /// Ton radio d'une équipe de course : les accusés de réception en anglais (« Copy », « Copy that »),
    /// et les phrases d'attente quand l'ingénieur doit réfléchir (« Copy, je regarde et je reviens vers vous »).
    /// </summary>
    public static class RadioStyle
    {
        private static readonly Regex FrenchAck = new(@"^(bien reçu|reçu|compris|d'accord|ok)\s*[,.:!]\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Ajoute « Copy » devant une réponse à une remarque ou une consigne du pilote (pas devant une réponse chiffrée à une question).
        /// « Bien reçu, on reste dehors » → « Copy, on reste dehors » ; « Noté pour les pneus » → « Copy, noté pour les pneus ».
        /// </summary>
        public static string WithCopy(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || Regex.IsMatch(text, @"^(copy|understood|roger|radio check)\b", RegexOptions.IgnoreCase))
            {
                return text;
            }

            Match m = FrenchAck.Match(text);
            if (m.Success)
            {
                string rest = text[m.Length..];
                return rest.Length == 0 ? "Copy." : "Copy, " + LowerFirst(rest);
            }

            return text.StartsWith("Noté", System.StringComparison.Ordinal) ? "Copy, " + LowerFirst(text) : "Copy. " + text;
        }

        private static int _turn;

        private static readonly string[] Waiting = { "Une seconde, je vérifie.", "Je regarde et je te dis.", "Reçu, je regarde ça.", "Deux secondes, je cherche." };

        private static readonly string[] WaitingEnglish = { "Copy, je regarde.", "Une seconde, je vérifie.", "Je regarde et je te dis.", "Deux secondes, je cherche." };

        private static readonly string[] Queued = { "Je finis la question d'avant et je te dis.", "Une seconde, je termine l'autre question." };

        private static readonly string[] Unclear = { "Pas compris, tu peux répéter ?", "Radio pas claire, répète ?", "J'ai mal entendu, redis-moi ?" };

        /// <summary>Le pilote attend : l'ingénieur réfléchit et reviendra (le pilote peut poser une autre question entre-temps). Formules variées.</summary>
        public static string Thinking(bool queued, bool english) =>
            Pick(queued ? Queued : english ? WaitingEnglish : Waiting);

        /// <summary>Phrase incompréhensible (dictée ratée) : on demande de répéter au lieu de répondre à côté.</summary>
        public static string NotUnderstood() => Pick(Unclear);

        /// <summary>
        /// « Copy » à chaque phrase finit par agacer : en tête de réponse, il devient tour à tour « Copy », « Reçu », « Compris »
        /// ou disparaît. Sans « Copy » (réglage en français), jamais de « Copy ».
        /// </summary>
        public static string Vary(string text, bool english)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            Match m = Regex.Match(text, @"^copy( that)?\s*([,.:!])\s*", RegexOptions.IgnoreCase);
            if (!m.Success)
            {
                return text;
            }

            string rest = text[m.Length..];
            string[] heads = english ? new[] { "Copy", "Reçu", null, "Compris" } : new[] { "Reçu", null, "Compris", null };
            string head = heads[Next() % heads.Length];
            if (head == null || rest.Length == 0)
            {
                return rest.Length == 0 ? (head ?? "Reçu") + "." : Cap(rest);
            }

            return m.Groups[2].Value == "," ? $"{head}, {rest}" : $"{head}. {rest}";
        }

        private static string Pick(string[] options) => options[Next() % options.Length];

        private static int Next() => System.Threading.Interlocked.Increment(ref _turn) & int.MaxValue;

        private static string Cap(string s) => s.Length == 0 ? s : char.ToUpper(s[0], System.Globalization.CultureInfo.GetCultureInfo("fr-FR")) + s[1..];

        private static string LowerFirst(string s) =>
            s.Length > 1 && char.IsUpper(s[0]) && !char.IsUpper(s[1]) ? char.ToLowerInvariant(s[0]) + s[1..] : s;
    }
}
