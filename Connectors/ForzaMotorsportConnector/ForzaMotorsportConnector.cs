namespace SecondMonitor.ForzaMotorsportConnector
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    using DataConvertor;

    using DataModel;
    //using DataModel.BasicProperties;

    using Foundation.Connectors;

    using NLog;

    //using NLog;

    using PluginsConfiguration.Common.Controller;

    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.ForzaMotorsportConnector.DataModels;

    public class ForzaMotorsportConnector : AbstractGameConnector
    {
        private static readonly string[] ForzaMotorsportExecutables = { "forza_steamworks_release_final", "forza_gaming.desktop.x64_release_final" };

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ForzaMotorsportDataConvertor _forzaMotorsportDataConvertor;
        private readonly ForzaMotorsportUdpReceiver _udpReceiver;

        private Task _udpReceiverTask;
        private CancellationTokenSource _udpCancellationTokenSource;

        private bool _isConnected;

        private DateTime? _lastUDPPacketReceivedAt;
        private SessionType _lastSessionType;
        private TimeSpan _lastSessionTime;

        public ForzaMotorsportConnector(IPluginSettingsProvider pluginSettingsProvider)
            : base(ForzaMotorsportExecutables)
        {
            _forzaMotorsportDataConvertor = new ForzaMotorsportDataConvertor(pluginSettingsProvider.ForzaMotorsportConfiguration);
            _lastSessionTime = TimeSpan.MaxValue;
            _udpReceiver = new ForzaMotorsportUdpReceiver(pluginSettingsProvider.ForzaMotorsportConfiguration);
        }

        public override bool IsConnected => _isConnected;

        protected override string ConnectorName => SimulatorsNameMap.ForzaMotorsportSimName;

        public override bool TryConnect()
        {
            if (!this.IsProcessRunning() && !_udpReceiver.IsPackageReceived())
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

        protected override void OnConnection()
        {
            _isConnected = true;
            _udpCancellationTokenSource = new CancellationTokenSource();
            _udpReceiverTask = _udpReceiver.ReceiveLoop(OnNewUdpData, _udpCancellationTokenSource.Token);
        }

        protected override void ResetConnector()
        {
            _forzaMotorsportDataConvertor.Reset();
            _lastSessionTime = TimeSpan.MaxValue;
            _lastSessionType = SessionType.Na;
            _lastUDPPacketReceivedAt = null;
        }

        public bool IsReceivingPackets()
        {
            if (_lastUDPPacketReceivedAt.HasValue)
            {
                return (DateTime.UtcNow - _lastUDPPacketReceivedAt.Value).TotalSeconds <= 30;
            }
            else
            {
                return true;
            }
        }

        protected override async Task DaemonMethod(CancellationToken cancellationToken)
        {
            while (IsProcessRunning() || IsReceivingPackets())
            {
                await Task.Delay(1000, cancellationToken);
            }

            await _udpCancellationTokenSource.CancelAsync();
            try
            {
                await _udpReceiverTask;
            }
            catch (OperationCanceledException)
            {
            }

            Disconnect();
            RaiseDisconnectedEvent();
        }

        private void OnNewUdpData(ForzaMotorsportUDPTelemetryData data)
        {
            if (_lastUDPPacketReceivedAt == null)
            {
                System.Threading.Thread.Sleep(1000);
            }

            _lastUDPPacketReceivedAt = DateTime.UtcNow;

            try
            {
                var dataSet = _forzaMotorsportDataConvertor.CreateSimulatorDataSet(data);

                if (data.IsRaceOn == 0)
                {
                    return;
                }

                if (_lastSessionType != dataSet.SessionInfo.SessionType || _lastSessionTime > dataSet.SessionInfo.SessionTime)
                {
                    _forzaMotorsportDataConvertor.Reset();
                    RaiseSessionStartedEvent(dataSet);
                }
                else
                {
                    RaiseDataLoadedEvent(dataSet);
                }

                _lastSessionType = dataSet.SessionInfo.SessionType;
                _lastSessionTime = dataSet.SessionInfo.SessionTime;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void Disconnect()
        {
            _isConnected = false;
        }
    }
}
