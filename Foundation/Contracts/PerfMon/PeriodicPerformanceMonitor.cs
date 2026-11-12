namespace SecondMonitor.Contracts.PerfMon
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using NLog;

    public class PeriodicPerformanceMonitor
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly TimeSpan _interval;
        private readonly PerformanceCounter _appCpuCounter;
        private readonly PerformanceCounter _totalCpuCounter;
        private readonly PerformanceCounter _memCounter;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private Task _periodicTask;
        private float _appHighestUsage;
        private float _totalHighestUsage;
        private bool _isInitialized;

        public PeriodicPerformanceMonitor(TimeSpan interval)
        {
            _interval = interval;
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _appCpuCounter = new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName, true);
                _totalCpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _memCounter = new PerformanceCounter("Process", "Working Set", Process.GetCurrentProcess().ProcessName, true);
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to initialzied performance monitor");
            }
        }

        public void Start()
        {
            if (!_isInitialized)
            {
                return;
            }

            try
            {
                _appCpuCounter.NextValue();
                _totalCpuCounter.NextValue();
                _periodicTask = Task.Run(() => PeriodicTask(_cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                _isInitialized = false;
                Logger.Warn(ex);
            }
        }

        public async Task Finish()
        {
            try
            {
                _cancellationTokenSource.Cancel();
                if (!_isInitialized)
                {
                    return;
                }

                await _periodicTask;
                _appCpuCounter.Close();
                _memCounter.Close();
                _totalCpuCounter.Close();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _isInitialized = false;
                Logger.Warn(ex);
            }
        }

        public void ResetHighestCpuUsage()
        {
            Logger.Info($"PERF MON: Reseting App Highest Usage: {_appHighestUsage:F2}%");
            Logger.Info($"PERF MON: Reseting Total Highest Usage: {_totalHighestUsage:F2}%");
            _appHighestUsage = 0;
            _totalHighestUsage = 0;
        }

        private async Task PeriodicTask(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(_interval, ct);
                    float cpuUsage = _appCpuCounter.NextValue() / Environment.ProcessorCount;
                    float totalCpuUsage = _totalCpuCounter.NextValue();
                    _appHighestUsage = Math.Max(_appHighestUsage, cpuUsage);
                    _totalHighestUsage = Math.Max(_totalHighestUsage, totalCpuUsage);
                    float memory = _memCounter.NextValue() / 1024 / 1024;
                    Logger.Info($"PERF MON: App Cpu Usage: {cpuUsage:F2}%, Highest Usage: {_appHighestUsage:F2}%");
                    Logger.Info($"PERF MON: Total Cpu Usage: {totalCpuUsage:F2}%, Highest Usage: {_totalHighestUsage:F2}%");
                    Logger.Info($"PERF MON: Memory Usage: {memory:F0}MB");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex);
                _isInitialized = false;
            }
        }
    }
}