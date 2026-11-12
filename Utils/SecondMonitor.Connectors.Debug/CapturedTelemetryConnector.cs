namespace SecondMonitor.Connectors.Debug
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.Versioning;
    using System.Threading;
    using System.Threading.Tasks;
    using ProtoBuf;
    using DataModel.Snapshot;
    using SecondMonitor.Debug;
    using SecondMonitor.Foundation.Connectors;

    [SupportedOSPlatform("windows7.0")]
    public class CapturedTelemetryConnector : IRawTelemetryConnector
    {
        private CancellationTokenSource _loopToken;
        private Task _loopTask;

        public event EventHandler<DataEventArgs> DataLoaded;
        public event EventHandler<EventArgs> ConnectedEvent;
        public event EventHandler<EventArgs> Disconnected;
        public event EventHandler<DataEventArgs> SessionStarted;
        public event EventHandler<MessageArgs> DisplayMessage;
        public string Name => "Raw Telemetry Replay";
        public bool IsConnected { get; private set; }
        public bool IsEnabledByDefault => false;
        public Action ConnectorOptionsDelegate => null;
        public bool TryConnect()
        {
            IsConnected = true;
            _loopToken = new CancellationTokenSource();

            EventArgs args = EventArgs.Empty;
            this.ConnectedEvent?.Invoke(this, args);

            return true;
        }

        public Task FinnishConnectorAsync()
        {
            _loopToken.Cancel();
            return _loopTask;
        }

        public void StartConnectorLoop()
        {
            _loopTask = Task.Run(ReplayThreadExecutor, _loopToken.Token);
        }
        
        private async Task ReplayThreadExecutor()
        {
            await Task.Delay(2000);

            var tempDirectory = new DirectoryInfo(Path.GetTempPath());
            var fileInfo = tempDirectory.EnumerateFiles("*.smtmp", SearchOption.TopDirectoryOnly)
                .OrderByDescending(x => x.Name)
                .First();
            using (var fileStream = fileInfo.OpenRead())
            {
                var header = Serializer.DeserializeWithLengthPrefix<DataHeader>(fileStream, PrefixStyle.Base128);

                var sessionStarted = Serializer.DeserializeWithLengthPrefix<DataPacket>(fileStream, PrefixStyle.Fixed32);
                var simulatorDataSet = new SimulatorDataSet(sessionStarted.Source)
                {
                    InputInfo = sessionStarted.InputInfo,
                    SessionInfo = sessionStarted.SessionInfo,
                    DriversInfo = sessionStarted.DriversInfo,
                    PlayerInfo = sessionStarted.PlayerInfo,
                    LeaderInfo = sessionStarted.LeaderInfo,
                    SimulatorSourceInfo = sessionStarted.SimulatorSourceInfo
                };

                SessionStarted?.Invoke(this, new DataEventArgs(simulatorDataSet));

                await Task.Delay(1000);

                while (!_loopToken.IsCancellationRequested && fileStream.Position < fileStream.Length)
                {
                    var stopwatch = Stopwatch.StartNew();

                    var dataLoaded = Serializer.DeserializeWithLengthPrefix<DataPacket>(fileStream, PrefixStyle.Fixed32);
                    simulatorDataSet = new SimulatorDataSet(dataLoaded.Source)
                    {
                        InputInfo = dataLoaded.InputInfo,
                        SessionInfo = dataLoaded.SessionInfo,
                        DriversInfo = dataLoaded.DriversInfo,
                        PlayerInfo = dataLoaded.PlayerInfo,
                        LeaderInfo = dataLoaded.LeaderInfo,
                        SimulatorSourceInfo = dataLoaded.SimulatorSourceInfo
                    };

                    DataLoaded?.Invoke(this, new DataEventArgs(simulatorDataSet));

                    stopwatch.Stop();
                    var sleepTime = Math.Max(header.UpdateInterval - stopwatch.ElapsedMilliseconds, 0L);

                    if (sleepTime > 0L)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(sleepTime));
                    }
                }
            }

            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }
}