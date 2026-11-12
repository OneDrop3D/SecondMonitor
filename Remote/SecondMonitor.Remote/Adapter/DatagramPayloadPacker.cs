namespace SecondMonitor.Remote.Adapter
{
    using System;
    using System.Diagnostics;

    using Comparators;

    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;

    using Models;

    using NLog;

    using PluginsConfiguration.Common.Controller;

    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.PluginsConfiguration.Common.DataModel;

    internal class DatagramPayloadPacker : IDatagramPayloadPacker
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ISimulatorSourceInfoComparator _simulatorSourceInfoComparator;
        private readonly bool _isNetworkConservationEnabled;
        private readonly Random _random;
        private readonly TimeSpan _packedDelay;
        private readonly TimeSpan _playerInfoDelay;
        private readonly TimeSpan _driversInfoDelay;

        private readonly Stopwatch _packageTimer;
        private readonly Stopwatch _playerInfoDelayTimer;
        private readonly Stopwatch _driversInfoDelayTimer;

        private string _lastSimulatorSourceName;
        private SimulatorSourceInfo _lastSimulatorSourceInfo;
        private SimulatorDataSet _lastDataSet;

        public DatagramPayloadPacker(IPluginSettingsProvider pluginSettingsProvider, ISimulatorSourceInfoComparator simulatorSourceInfoComparator)
        {
            _simulatorSourceInfoComparator = simulatorSourceInfoComparator;
            BroadcastLimitSettings broadcastLimitSettings = pluginSettingsProvider.RemoteConfiguration.BroadcastLimitSettings;
            _random = new Random();
            _isNetworkConservationEnabled = broadcastLimitSettings.IsEnabled;
            _packedDelay = TimeSpan.FromMilliseconds(broadcastLimitSettings.MinimumPackageInterval);
            _playerInfoDelay = TimeSpan.FromMilliseconds(broadcastLimitSettings.PlayerTimingPackageInterval);
            _driversInfoDelay = TimeSpan.FromMilliseconds(broadcastLimitSettings.OtherDriversTimingPackageInterval);

            if (!_isNetworkConservationEnabled)
            {
                Logger.Info("Network conservation is disabled");
                return;
            }

            Logger.Info("Network conservation is enabled");
            Logger.Info($"Package Delay {_packedDelay.TotalMilliseconds}");
            Logger.Info($"Player info Delay {_playerInfoDelay.TotalMilliseconds}");
            Logger.Info($"Other drivers info Delay {_driversInfoDelay.TotalMilliseconds}");

            _packageTimer = Stopwatch.StartNew();
            _playerInfoDelayTimer = Stopwatch.StartNew();
            _driversInfoDelayTimer = Stopwatch.StartNew();
        }

        public bool IsMinimalPackageDelayPassed()
        {
            return !_isNetworkConservationEnabled || _packageTimer.Elapsed > _packedDelay;
        }

        public DatagramPayload CreateHandshakeDatagramPayload()
        {
            return CreateHearthBeatDatagramPayload();
        }

        public DatagramPayload CreateHearthBeatDatagramPayload()
        {
            SimulatorDataSet dataSetToUse = _lastDataSet ?? new SimulatorDataSet("Remote");
            dataSetToUse.InputInfo = new InputInfo()
                { BrakePedalPosition = _random.NextDouble(), ClutchPedalPosition = _random.NextDouble(), ThrottlePedalPosition = _random.NextDouble() };

            return CreatePayload(dataSetToUse, DatagramPayloadKind.HearthBeat);
        }

        public DatagramPayload CreateRegularDatagramPayload(SimulatorDataSet simulatorData)
        {
            _lastDataSet = simulatorData;
            DatagramPayload datagramPayload = CreatePayload(simulatorData, DatagramPayloadKind.Normal);
            RemoveUnnecessaryData(datagramPayload);
            return datagramPayload;
        }

        public DatagramPayload CreateSessionStartedPayload(SimulatorDataSet simulatorData)
        {
            _lastDataSet = simulatorData;
            DatagramPayload datagramPayload = CreatePayload(simulatorData, DatagramPayloadKind.SessionStart);
            return datagramPayload;
        }

        private DatagramPayload CreatePayload(SimulatorDataSet simulatorDataSet, DatagramPayloadKind payloadKind)
        {
            return new DatagramPayload()
            {
                ContainsOtherDriversTiming = true,
                ContainsPlayersTiming = true,
                ContainsSimulatorSourceInfo = true,
                DriversInfo = simulatorDataSet.DriversInfo,
                InputInfo = simulatorDataSet.InputInfo,
                LeaderInfo = simulatorDataSet.LeaderInfo,
                PayloadKind = payloadKind,
                PlayerInfo = simulatorDataSet.PlayerInfo,
                SessionInfo = simulatorDataSet.SessionInfo,
                Source = simulatorDataSet.Source,
                SimulatorSourceInfo = simulatorDataSet.SimulatorSourceInfo
            };
        }

        private void RemoveUnnecessaryData(DatagramPayload datagramPayload)
        {
            if (!_isNetworkConservationEnabled)
            {
                return;
            }

            if (_packageTimer.Elapsed > _packedDelay)
            {
                _packageTimer.Restart();
            }

            if (_lastSimulatorSourceName != datagramPayload.Source || !_simulatorSourceInfoComparator.AreEqual(datagramPayload.SimulatorSourceInfo, _lastSimulatorSourceInfo))
            {
                _lastSimulatorSourceName = datagramPayload.Source;
                _lastSimulatorSourceInfo = datagramPayload.SimulatorSourceInfo;
            }
            else
            {
                datagramPayload.ContainsSimulatorSourceInfo = false;
                datagramPayload.SimulatorSourceInfo = null;
            }

            if (_driversInfoDelayTimer.Elapsed < _driversInfoDelay)
            {
                datagramPayload.ContainsOtherDriversTiming = false;
                datagramPayload.DriversInfo = Array.Empty<DriverInfo>();
                datagramPayload.LeaderInfo = null;
            }
            else
            {
                _driversInfoDelayTimer.Restart();
                _playerInfoDelayTimer.Restart();
                return;
            }

            if (_playerInfoDelayTimer.Elapsed > _playerInfoDelay)
            {
                _playerInfoDelayTimer.Restart();
            }
            else
            {
                datagramPayload.PlayerInfo = null;
                datagramPayload.ContainsPlayersTiming = false;
            }
        }
    }
}
