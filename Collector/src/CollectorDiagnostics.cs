namespace SecondMonitor.Collector
{
    using System;
    using System.Diagnostics;
    using System.Globalization;

    /// <summary>
    /// Mode diagnostic : ce que le collecteur coûte au PC de jeu (CPU, mémoire, ramasse-miettes, réseau)
    /// et à quel rythme il lit le jeu. Rien n'est calculé tant que le mode n'est pas activé.
    /// </summary>
    internal sealed class CollectorDiagnostics
    {
        private readonly CollectorRuntime _runtime;
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly object _lock = new();
        private double _lastSeconds;
        private TimeSpan _lastCpu;
        private long _lastAllocated;
        private long _lastWireBytes;
        private long _lastDatagrams;
        private long _lastReads;
        private long _lastSends;
        private int _lastGc0;
        private int _lastGc1;
        private int _lastGc2;
        private int _threads;
        private double _lastThreadsSeconds = double.MinValue;

        public CollectorDiagnostics(CollectorRuntime runtime)
        {
            _runtime = runtime;
            Sample();
        }

        /// <summary>Dernière mesure (null avant la deuxième).</summary>
        public DiagnosticSample Last { get; private set; }

        /// <summary>Nouvelle mesure, moyennée depuis la précédente (au moins une demi-seconde d'écart).</summary>
        public DiagnosticSample Sample()
        {
            lock (_lock)
            {
                double now = _clock.Elapsed.TotalSeconds;
                double dt = now - _lastSeconds;
                if (Last != null && dt < 0.5)
                {
                    return Last;
                }

                TimeSpan cpu = Environment.CpuUsage.TotalTime;
                long allocated = GC.GetTotalAllocatedBytes(false);
                long wire = _runtime.WireBytesSent;
                long datagrams = _runtime.DatagramsSent;
                long reads = _runtime.GameReads;
                long sends = _runtime.PackagesSent;
                int gc0 = GC.CollectionCount(0);
                int gc1 = GC.CollectionCount(1);
                int gc2 = GC.CollectionCount(2);

                // Le nombre de threads demande un instantané des processus (coûteux sous Windows) : toutes les 5 s au plus.
                if (now - _lastThreadsSeconds >= 5)
                {
                    _lastThreadsSeconds = now;
                    try
                    {
                        using Process self = Process.GetCurrentProcess();
                        _threads = self.Threads.Count;
                    }
                    catch (Exception)
                    {
                        _threads = 0;
                    }
                }

                if (_lastSeconds > 0 && dt > 0)
                {
                    double cpuCore = (cpu - _lastCpu).TotalSeconds / dt * 100;
                    Last = new DiagnosticSample(
                        CpuPercent: cpuCore / Environment.ProcessorCount,
                        CpuCorePercent: cpuCore,
                        WorkingSetMb: Environment.WorkingSet / 1048576.0,
                        GcHeapMb: GC.GetTotalMemory(false) / 1048576.0,
                        AllocatedKbPerSecond: (allocated - _lastAllocated) / dt / 1024,
                        Gc0: gc0 - _lastGc0,
                        Gc1: gc1 - _lastGc1,
                        Gc2: gc2 - _lastGc2,
                        Threads: _threads,
                        WireKbPerSecond: (wire - _lastWireBytes) / dt / 1024,
                        DatagramsPerSecond: (datagrams - _lastDatagrams) / dt,
                        GameReadsPerSecond: (reads - _lastReads) / dt,
                        SendsPerSecond: (sends - _lastSends) / dt);
                }

                _lastSeconds = now;
                _lastCpu = cpu;
                _lastAllocated = allocated;
                _lastWireBytes = wire;
                _lastDatagrams = datagrams;
                _lastReads = reads;
                _lastSends = sends;
                _lastGc0 = gc0;
                _lastGc1 = gc1;
                _lastGc2 = gc2;
                return Last;
            }
        }
    }

    /// <summary>Une mesure du mode diagnostic.</summary>
    internal sealed record DiagnosticSample(
        double CpuPercent,
        double CpuCorePercent,
        double WorkingSetMb,
        double GcHeapMb,
        double AllocatedKbPerSecond,
        int Gc0,
        int Gc1,
        int Gc2,
        int Threads,
        double WireKbPerSecond,
        double DatagramsPerSecond,
        double GameReadsPerSecond,
        double SendsPerSecond)
    {
        /// <summary>Résumé sur une ligne, en français (virgule décimale).</summary>
        public string Text =>
            $"CPU {N(CpuPercent, 2)} % du PC ({N(CpuCorePercent, 1)} % d'un cœur) · mémoire {N(WorkingSetMb, 0)} Mo (tas {N(GcHeapMb, 1)} Mo) · "
            + $"allocations {N(AllocatedKbPerSecond, 0)} Ko/s · GC {Gc0}/{Gc1}/{Gc2} · {Threads} threads · "
            + $"réseau {N(WireKbPerSecond, 1)} Ko/s ({N(DatagramsPerSecond, 0)} paquets UDP/s) · jeu lu {N(GameReadsPerSecond, 0)}×/s · envois {N(SendsPerSecond, 0)}/s";

        private static string N(double value, int decimals) =>
            value.ToString("F" + decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture).Replace('.', ',');
    }
}
