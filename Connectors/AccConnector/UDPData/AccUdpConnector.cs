namespace SecondMonitor.AccConnector.UDPData
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using PluginsConfiguration.Common.DataModel;
    using Structs;

    public class AccUdpConnector
    {
        private readonly AccConfiguration _accConfiguration;
        private AccUdpRemoteClient _accUdpRemoteClient;
        private DateTime _lastCarRefresh;

        public AccUdpConnector(AccConfiguration accConfiguration)
        {
            _accConfiguration = accConfiguration;
            Reset();
        }

        public FullUdpData FullUdpData { get; private set; }

        public bool IsConnected { get; private set; }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            await Task.Run(Connect, cancellationToken);
            while (!IsConnected && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);
            }
        }

        public void Connect()
        {
            _accUdpRemoteClient = new AccUdpRemoteClient("127.0.0.1", _accConfiguration.Port, "Second Monitor", _accConfiguration.ConnectionPassword, _accConfiguration.ConnectionCommandPassword, _accConfiguration.PoolInterval);
            _accUdpRemoteClient.MessageHandler.OnBroadcastingEvent += MessageHandlerOnOnBroadcastingEvent;
            _accUdpRemoteClient.MessageHandler.OnConnectionStateChanged += MessageHandlerOnOnConnectionStateChanged;
            _accUdpRemoteClient.MessageHandler.OnEntrylistUpdate += MessageHandlerOnOnEntryListUpdate;
            _accUdpRemoteClient.MessageHandler.OnRealtimeCarUpdate += MessageHandlerOnOnRealtimeCarUpdate;
            _accUdpRemoteClient.MessageHandler.OnTrackDataUpdate += MessageHandlerOnOnTrackDataUpdate;
            _accUdpRemoteClient.MessageHandler.OnRealtimeUpdate += MessageHandlerOnOnRealtimeUpdate;
        }

        public void Disconnect()
        {
            _accUdpRemoteClient?.Shutdown();
        }

        public void Reset()
        {
            _lastCarRefresh = DateTime.Now;
            FullUdpData = new FullUdpData();
        }

        private void MessageHandlerOnOnRealtimeUpdate(string sender, RealtimeUpdate update)
        {
            if (FullUdpData.CurrentTrack.TrackId == -240)
            {
                _accUdpRemoteClient.MessageHandler.RequestTrackData();
            }

            if (update.Phase == SessionPhase.FormationLap || update.Phase == SessionPhase.NONE || update.Phase == SessionPhase.PreFormation || update.Phase == SessionPhase.PreSession || update.Phase == SessionPhase.Session)
            {
                FullUdpData.LeaderFinished = false;
            }

            FullUdpData.LastRealtimeUpdate = update;
        }

        private void MessageHandlerOnOnTrackDataUpdate(string sender, TrackData trackUpdate)
        {
            FullUdpData.CurrentTrack = trackUpdate;
        }

        private void MessageHandlerOnOnRealtimeCarUpdate(string sender, RealtimeCarUpdate carUpdate)
        {
            if (!TryGetCarData(carUpdate.CarIndex, out FullCarData fullCarData))
            {
                return;
            }

            IsConnected = true;
            fullCarData.UpdateRealtimeUpdate(carUpdate, FullUdpData.LastRealtimeUpdate);
        }

        private void MessageHandlerOnOnEntryListUpdate(string sender, CarInfo car)
        {
            if (FullUdpData.CarsDictionary.TryGetValue(car.CarIndex, out FullCarData fullCarData))
            {
                fullCarData.UpdateCarInfo(car);
                return;
            }

            FullUdpData.LeaderFinished = false;
            FullUdpData.CarsDictionary.Add(car.CarIndex, new FullCarData(car));
        }

        private void MessageHandlerOnOnConnectionStateChanged(int connectionId, bool connectionSuccess, bool isReadOnly, string error)
        {
            IsConnected = connectionSuccess;
        }

        private void MessageHandlerOnOnBroadcastingEvent(string sender, BroadcastingEvent evt)
        {
            if (evt.CarId < 0 || !TryGetCarData(evt.CarId, out FullCarData fullCarData))
            {
                return;
            }

            switch (evt.Type)
            {
                case BroadcastingCarEventType.None:
                    break;
                case BroadcastingCarEventType.GreenFlag:
                    fullCarData.CausingYellow = false;
                    break;
                case BroadcastingCarEventType.SessionOver:
                    break;
                case BroadcastingCarEventType.PenaltyCommMsg:
                    break;
                case BroadcastingCarEventType.Accident:
                    fullCarData.CausingYellow = true;
                    break;
                case BroadcastingCarEventType.LapCompleted:
                    if (FullUdpData.LastRealtimeUpdate.Phase == SessionPhase.SessionOver && fullCarData.LastUpdate.Position == 1)
                    {
                        FullUdpData.LeaderFinished = true;
                    }

                    if (FullUdpData.LeaderFinished)
                    {
                        fullCarData.IsFinished = true;
                    }

                    break;
                case BroadcastingCarEventType.BestSessionLap:
                    break;
                case BroadcastingCarEventType.BestPersonalLap:
                    break;
                default:
                    break;
            }
        }

        private bool TryGetCarData(int carId, out FullCarData fullCarData)
        {
            if (FullUdpData.CarsDictionary.TryGetValue(carId, out fullCarData))
            {
                return true;
            }

            if ((DateTime.Now - _lastCarRefresh).TotalSeconds > 2)
            {
                _lastCarRefresh = DateTime.Now;
                _accUdpRemoteClient.RefreshCarList();
            }

            return false;
        }
    }
}
