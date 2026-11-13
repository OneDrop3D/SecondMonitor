namespace SecondMonitor.Hub.Core.Music
{
    using System;

    /// <summary>Quand la musique est permise : toujours, pas en course, ou seulement en essais.</summary>
    public static class MusicPolicy
    {
        public const string Always = "toujours";
        public const string NotInRace = "pasEnCourse";
        public const string PracticeOnly = "essaisSeulement";

        /// <summary>
        /// Vrai si la musique est permise pendant cette session (« Practice », « Qualification », « Race », « WarmUp »…).
        /// Sans session (pas de jeu), toujours permise. <paramref name="reason"/> est la phrase dite au pilote sinon.
        /// </summary>
        public static bool Allowed(string policy, string sessionType, out string reason)
        {
            reason = null;
            bool known = !string.IsNullOrEmpty(sessionType) && sessionType != "Na";
            if (!known || string.IsNullOrEmpty(policy) || policy == Always)
            {
                return true;
            }

            if (policy == NotInRace && sessionType == "Race")
            {
                reason = "Pas de musique pendant la course : c'est le réglage choisi.";
                return false;
            }

            if (policy == PracticeOnly && sessionType != "Practice" && sessionType != "WarmUp")
            {
                reason = sessionType == "Race"
                    ? "Pas de musique pendant la course : seulement en essais, c'est le réglage choisi."
                    : "La musique est réservée aux essais : c'est le réglage choisi.";
                return false;
            }

            return true;
        }
    }

    public enum DuckStep
    {
        None,

        /// <summary>La radio commence : baisser ou mettre la musique en pause.</summary>
        Duck,

        /// <summary>La radio est finie (depuis un court instant) : remettre la musique comme avant.</summary>
        Restore,
    }

    /// <summary>
    /// Priorité à la radio : la musique s'efface quand le pilote appuie sur la radio (enregistrement, réflexion de l'IA)
    /// ou quand l'ingénieur parle, puis revient un peu après la fin (pas de va-et-vient entre deux messages).
    /// Logique pure (l'heure est fournie) : testable sans lecteur.
    /// </summary>
    public sealed class DuckPlanner
    {
        private bool _radio;
        private DateTime? _voiceUntil;
        private DateTime _lastActive = DateTime.MinValue;

        /// <summary>Attente après la fin de la radio avant de remettre la musique.</summary>
        public TimeSpan ReleaseDelay { get; set; } = TimeSpan.FromMilliseconds(1200);

        /// <summary>Sécurité si la fin de la voix n'est jamais signalée (écran fermé pendant qu'il parlait).</summary>
        public TimeSpan VoiceTimeout { get; set; } = TimeSpan.FromSeconds(45);

        public bool Ducked { get; private set; }

        public bool Active(DateTime now) => _radio || (_voiceUntil is DateTime until && until > now);

        /// <summary>Le pilote a appuyé sur la radio (enregistrement ou réponse en préparation).</summary>
        public void SetRadio(bool active, DateTime now)
        {
            if (_radio && !active)
            {
                _lastActive = now;
            }

            _radio = active;
        }

        /// <summary>La voix de l'ingénieur commence ou s'arrête sur le poste radio.</summary>
        public void SetVoice(bool playing, DateTime now)
        {
            if (playing)
            {
                _voiceUntil = now + VoiceTimeout;
            }
            else if (_voiceUntil != null)
            {
                _voiceUntil = null;
                _lastActive = now;
            }
        }

        public DuckStep Tick(DateTime now)
        {
            if (_voiceUntil is DateTime until && until <= now)
            {
                _voiceUntil = null;
                _lastActive = until;
            }

            bool active = Active(now);
            if (active)
            {
                _lastActive = now;
                if (!Ducked)
                {
                    Ducked = true;
                    return DuckStep.Duck;
                }

                return DuckStep.None;
            }

            if (Ducked && now - _lastActive >= ReleaseDelay)
            {
                Ducked = false;
                return DuckStep.Restore;
            }

            return DuckStep.None;
        }
    }
}
