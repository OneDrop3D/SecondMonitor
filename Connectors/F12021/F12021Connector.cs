namespace SecondMonitor.F12021Connector
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModels;
    using Foundation.Connectors;
    using NLog;
    using PluginsConfiguration.Common.Controller;

    public class F12021Connector : AbstractGameConnector
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly F12021UdpReceiver _udpReceiver;
        private readonly F12021DataConvertor _f12021DataConvertor;
        private bool _isConnected;
        private CancellationTokenSource _udpCancellationTokenSource;
        private Task _udpReceiverTask;
        private SessionType _lastSessionType;
        private TimeSpan _lastSessionTime;

        public F12021Connector(IPluginSettingsProvider pluginSettingsProvider) : base(new[] { "F1_2021_dx12" })
        {
            _udpReceiver = new F12021UdpReceiver(OnSessionStarted, OnDataLoaded, pluginSettingsProvider.F12019Configuration.Port);
            _f12021DataConvertor = new F12021DataConvertor(pluginSettingsProvider.F12019Configuration);
        }

        protected override string ConnectorName => F12021DataConvertor.ConnectorName;

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
            RaiseSessionStartedEvent(new SimulatorDataSet(F12021DataConvertor.ConnectorName));
            while (IsProcessRunning())
            {
                await Task.Delay(1000, cancellationToken);
            }

            _udpCancellationTokenSource.Cancel();
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
                _f12021DataConvertor.Reset();
                var convertedData = _f12021DataConvertor.ConvertData(rawData);
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
                var convertedData = _f12021DataConvertor.ConvertData(rawData);
                if (_lastSessionType != convertedData.SessionInfo.SessionType && _lastSessionTime - convertedData.SessionInfo.SessionTime > TimeSpan.FromSeconds(5))
                {
                    _f12021DataConvertor.Reset();
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