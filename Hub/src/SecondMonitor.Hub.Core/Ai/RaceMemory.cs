namespace SecondMonitor.Hub.Core.Ai
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Mémoire de la course en cours : retours du pilote (« l'arrière glisse », « la piste sèche au virage 3 »),
    /// réponses aux questions de l'IA et décisions prises ensemble (relais allongé…). Remise à zéro à chaque session.
    /// </summary>
    public sealed class RaceMemory
    {
        private readonly object _lock = new();
        private readonly List<MemoryEntry> _entries = new();
        private PendingQuestion _pending;
        private int _nextId = 1;

        /// <summary>Arrêt décidé avec le pilote (remplace l'arrêt conseillé tant qu'il n'est pas passé).</summary>
        public int? PlannedStopLap { get; set; }

        /// <summary>Ce que le pilote a demandé pour le prochain arrêt (pneus, réparation, carburant).</summary>
        public PitOrder PitPlan { get; set; }

        /// <summary>L'ingénieur a déjà proposé de régler lui-même le menu des stands pendant cette session.</summary>
        public bool PitAutoAsked { get; set; }

        /// <summary>Décidé avec le pilote : finir sans s'arrêter.</summary>
        public bool NoStopPlanned { get; set; }

        /// <summary>Fil de la conversation : dernière proposition de l'ingénieur, réglages acceptés ou refusés.</summary>
        public ConversationThread Thread { get; } = new();

        public IReadOnlyList<MemoryEntry> Entries
        {
            get
            {
                lock (_lock)
                {
                    return _entries.ToList();
                }
            }
        }

        public MemoryEntry Add(string source, string topic, string text, int? lap, string symptom = null, string tyres = null)
        {
            lock (_lock)
            {
                var e = new MemoryEntry { Id = _nextId++, Source = source, Topic = topic, Text = text.Trim(), Lap = lap, At = DateTime.Now, Symptom = symptom, Tyres = tyres };
                _entries.Add(e);
                if (_entries.Count > 100)
                {
                    _entries.RemoveAt(0);
                }

                return e;
            }
        }

        public bool Remove(int id)
        {
            lock (_lock)
            {
                return _entries.RemoveAll(e => e.Id == id) > 0;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _entries.Clear();
                _pending = null;
                PlannedStopLap = null;
                PitPlan = null;
                PitAutoAsked = false;
                NoStopPlanned = false;
            }

            Thread.Clear();
        }

        public void Ask(PendingQuestion question)
        {
            lock (_lock)
            {
                _pending = question;
            }
        }

        /// <summary>Question de l'IA encore en attente de réponse (moins de <paramref name="maxAgeSeconds"/> s).</summary>
        public PendingQuestion Pending(double maxAgeSeconds = 240)
        {
            lock (_lock)
            {
                return _pending != null && (DateTime.UtcNow - _pending.AskedAtUtc).TotalSeconds <= maxAgeSeconds ? _pending : null;
            }
        }

        public void ClearPending()
        {
            lock (_lock)
            {
                _pending = null;
            }
        }

        /// <summary>Texte ajouté à la fiche de faits de l'IA.</summary>
        public string Facts()
        {
            List<MemoryEntry> list = Entries.TakeLast(12).ToList();
            string decisions = Thread.Facts();
            if (list.Count == 0 && PlannedStopLap == null && !NoStopPlanned && decisions == null)
            {
                return null;
            }

            var sb = new StringBuilder("MÉMOIRE DE LA COURSE (retours du pilote et décisions) :");
            if (NoStopPlanned)
            {
                sb.Append("\n- Décision avec le pilote : finir sans s'arrêter.");
            }

            if (PlannedStopLap.HasValue)
            {
                sb.Append($"\n- Arrêt décidé avec le pilote : tour {PlannedStopLap}.");
            }

            foreach (MemoryEntry e in list)
            {
                sb.Append($"\n- {(e.Lap.HasValue ? $"Tour {e.Lap}" : e.At.ToString("HH:mm"))}, {e.Source} : {e.Text}");
            }

            if (decisions != null)
            {
                sb.Append("\n- " + decisions);
            }

            return sb.ToString();
        }
    }

    public sealed class MemoryEntry
    {
        public int Id { get; set; }

        public DateTime At { get; set; }

        public int? Lap { get; set; }

        /// <summary>« pilote », « ingénieur », « IA ».</summary>
        public string Source { get; set; }

        public string Topic { get; set; }

        public string Text { get; set; }

        /// <summary>Symptôme reconnu (« understeer », « lock-front »…) pour un retour du pilote sur la voiture.</summary>
        public string Symptom { get; set; }

        /// <summary>État des pneus au moment du retour : « neufs », « mi-usure », « usés ».</summary>
        public string Tyres { get; set; }
    }

    public sealed class PendingQuestion
    {
        public string Key { get; set; }

        public string Topic { get; set; }

        public string Text { get; set; }

        public DateTime AskedAtUtc { get; set; } = DateTime.UtcNow;

        public int Lap { get; set; }

        /// <summary>Pour un relais à allonger : arrêt prévu et arrêt proposé.</summary>
        public int? PlannedLap { get; set; }

        public int? ProposedLap { get; set; }
    }
}
