namespace SecondMonitor.F12024Connector
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using NLog;

    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.F12024Connector.DataModels;
    using SecondMonitor.Foundation.Connectors;
    using SecondMonitor.PluginsConfiguration.Common.Controller;

    public class F12024Connector : AbstractGameConnector
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly F12024UdpReceiver _udpReceiver;
        private readonly F12024DataConvertor _f12024DataConvertor;
        private bool _isConnected;
        private CancellationTokenSource _udpCancellationTokenSource;
        private Task _udpReceiverTask;
        private SessionType _lastSessionType;
        private TimeSpan _lastSessionTime;

        public F12024Connector(IPluginSettingsProvider pluginSettingsProvider) : base(new[] { "F1_24" })
        {
            _udpReceiver = new F12024UdpReceiver(OnSessionStarted, OnDataLoaded, pluginSettingsProvider.F12019Configuration.Port);
            _f12024DataConvertor = new F12024DataConvertor(pluginSettingsProvider.F12019Configuration);
        }

        protected override string ConnectorName => F12024DataConvertor.ConnectorName;

        public override bool IsConnected => _isConnected;

        protected override void OnConnection()
        {
            _isConnected = true;
            _udpCancellationTokenSource = new CancellationTokenSource();
            _udpReceiverTask = _udpReceiver.ReceiveLoop(_udpCancellationTokenSource.Token);
        }

        protected override void ResetConnector()
        {
        }

        protected override async Task DaemonMethod(CancellationToken cancellationToken)
        {
            RaiseSessionStartedEvent(new SimulatorDataSet(F12024DataConvertor.ConnectorName));
            while (IsProcessRunning())
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

        private void OnSessionStarted(AllPacketsComposition rawData)
        {
            try
            {
                _f12024DataConvertor.Reset();
                var convertedData = _f12024DataConvertor.ConvertData(rawData);
                _lastSessionType = convertedData.SessionInfo.SessionType;
                _lastSessionTime = convertedData.SessionInfo.SessionTime;
                RaiseSessionStartedEvent(convertedData);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void OnDataLoaded(AllPacketsComposition rawData)
        {
            try
            {
                var convertedData = _f12024DataConvertor.ConvertData(rawData);
                if (_lastSessionType != convertedData.SessionInfo.SessionType && _lastSessionTime - convertedData.SessionInfo.SessionTime > TimeSpan.FromSeconds(5))
                {
                    _f12024DataConvertor.Reset();
                    RaiseSessionStartedEvent(convertedData);
                }
                else
                {
                    RaiseDataLoadedEvent(convertedData);
                }

                _lastSessionType = convertedData.SessionInfo.SessionType;
                _lastSessionTime = convertedData.SessionInfo.SessionTime;
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