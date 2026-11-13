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

        /// <summary>Le pilote attend : l'ingénieur réfléchit et reviendra (le pilote peut poser une autre question entre-temps).</summary>
        public static string Thinking(bool queued, bool english) =>
            english
                ? queued ? "Copy, je finis la question d'avant et je reviens vers vous." : "Copy, je regarde ça et je reviens vers vous."
                : queued ? "Bien reçu, je finis la question d'avant et je reviens vers vous." : "Bien reçu, je regarde ça et je reviens vers vous.";

        private static string LowerFirst(string s) =>
            s.Length > 1 && char.IsUpper(s[0]) && !char.IsUpper(s[1]) ? char.ToLowerInvariant(s[0]) + s[1..] : s;
    }
}
