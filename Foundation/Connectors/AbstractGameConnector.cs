namespace SecondMonitor.Foundation.Connectors
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using NLog;
    using DataModel.Snapshot;

    public abstract class AbstractGameConnector : IRawTelemetryConnector
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly string[] _executables;
        private Task _daemonTask;
        private CancellationTokenSource _cancellationTokenSource;
        private Stopwatch _lastCheck;

        protected AbstractGameConnector(string[] executables)
        {
            _executables = executables;
            this.ProcessName = string.Empty;
            this.TickTime = 16;
        }

        public event EventHandler<DataEventArgs> DataLoaded;
        public event EventHandler<EventArgs> ConnectedEvent;
        public event EventHandler<EventArgs> Disconnected;
        public event EventHandler<DataEventArgs> SessionStarted;
        public event EventHandler<MessageArgs> DisplayMessage;

        public string Name => ConnectorName;
        public abstract bool IsConnected { get; }
        public bool IsEnabledByDefault => true;
        public Action ConnectorOptionsDelegate => null;
        public int TickTime { get; set; }

        protected string ProcessName { get; private set; }
        protected Process Process { get; private set; }

        protected bool ShouldDisconnect
        {
            get;
            set;
        }

        protected abstract string ConnectorName { get; }

        public bool IsProcessRunning()
        {
            if (!string.IsNullOrWhiteSpace(this.ProcessName))
            {
                if (_lastCheck.ElapsedMilliseconds < 5000)
                {
                    return true;
                }

                _lastCheck.Restart();
                if (Process.GetProcessesByName(this.ProcessName).Length > 0)
                {
                    return true;
                }

                this.Process = null;
                this.ProcessName = string.Empty;
                _lastCheck.Stop();
                return false;
            }

            foreach (var processName in _executables)
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes.Length <= 0)
                {
                    continue;
                }

                this.Process = processes[0];
                this.ProcessName = processName;
                _lastCheck = Stopwatch.StartNew();
                return true;
            }

            return false;
        }

        public virtual bool TryConnect()
        {
            return this.Connect();
        }

        public async Task FinnishConnectorAsync()
        {
            _cancellationTokenSource.Cancel();
            try
            {
                await _daemonTask;
            }
            catch (OperationCanceledException ex)
            {
                Logger.Info(ex, "task was cancelled");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in connector");
            }

            _daemonTask = null;
        }

        private bool Connect()
        {
            if (!this.IsProcessRunning())
            {
                return false;
            }

            try
            {
                this.OnConnection();
                if (!this.IsConnected)
                {
                    return false;
                }

                this.RaiseConnectedEvent();
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }

        protected abstract void OnConnection();

        protected abstract void ResetConnector();

        protected abstract Task DaemonMethod(CancellationToken cancellationToken);

        public void StartConnectorLoop()
        {
            if (_daemonTask != null)
            {
                throw new InvalidOperationException("Daemon is already running");
            }

            _cancellationTokenSource = new CancellationTokenSource();
            this.ResetConnector();
            this.ShouldDisconnect = false;
            _daemonTask = this.DaemonMethod(_cancellationTokenSource.Token);
        }

        protected void SendMessageToClients(string message)
        {
            this.SendMessageToClients(message, null);
        }

        protected void SendMessageToClients(string message, Action action)
        {
            MessageArgs args = new MessageArgs(message, action);
            this.DisplayMessage?.Invoke(this, args);
        }

        protected void RaiseDataLoadedEvent(SimulatorDataSet data)
        {
            DataEventArgs args = new DataEventArgs(data);
            this.DataLoaded?.Invoke(this, args);
        }

        protected void RaiseConnectedEvent()
        {
            EventArgs args = new EventArgs();
            this.ConnectedEvent?.Invoke(this, args);
        }

        protected void RaiseDisconnectedEvent()
        {
            EventArgs args = new EventArgs();
            this.Disconnected?.Invoke(this, args);
        }

        protected void RaiseSessionStartedEvent(SimulatorDataSet data)
        {
            DataEventArgs args = new DataEventArgs(data);
            EventHandler<DataEventArgs> handler = this.SessionStarted;
            handler?.Invoke(this, args);
        }
    }
}