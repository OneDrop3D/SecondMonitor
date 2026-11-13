namespace SecondMonitor.Collector
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Valeur relue au plus toutes les N secondes. La fenêtre se met à jour chaque seconde, mais les adresses réseau
    /// (coûteuses à lire sous Windows) ou le démarrage automatique (registre) changent rarement.
    /// </summary>
    internal sealed class Throttled<T>
    {
        private readonly Func<T> _read;
        private readonly long _everyMs;
        private readonly Stopwatch _age = new();
        private T _value;

        public Throttled(Func<T> read, TimeSpan every)
        {
            _read = read;
            _everyMs = (long)every.TotalMilliseconds;
        }

        public T Value
        {
            get
            {
                if (!_age.IsRunning || _age.ElapsedMilliseconds >= _everyMs)
                {
                    _value = _read();
                    _age.Restart();
                }

                return _value;
            }
        }

        /// <summary>Relire à la prochaine demande (la valeur vient de changer).</summary>
        public void Invalidate() => _age.Reset();
    }
}
