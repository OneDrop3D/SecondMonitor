namespace SecondMonitor.Remote.Adapter
{
    using System;
    using System.Linq;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using Models;

    using SecondMonitor.DataModel.Extensions;

    internal class DatagramPayloadUnPacker : IDatagramPayloadUnPacker
    {
        private DriverInfo _lastLeaderInfo = new();
        private DriverInfo _lastPlayerInfo = new();
        private DriverInfo[] _lastDriversInfo = Array.Empty<DriverInfo>();
        private SimulatorSourceInfo _lastSimulatorSourceInfo = new();

        public SimulatorDataSet UnpackDatagramPayload(DatagramPayload datagramPayload)
        {
            ExtractLastData(datagramPayload);
            return CreateSimulatorDataSet(datagramPayload);
        }

        private SimulatorDataSet CreateSimulatorDataSet(DatagramPayload datagramPayload)
        {
            SimulatorDataSet newSet = new SimulatorDataSet(datagramPayload.Source)
            {
                DriversInfo = datagramPayload.DriversInfo,
                InputInfo = datagramPayload.InputInfo,
                LeaderInfo = datagramPayload.LeaderInfo,
                PlayerInfo = datagramPayload.PlayerInfo,
                SessionInfo = datagramPayload.SessionInfo,
                SimulatorSourceInfo = datagramPayload.ContainsSimulatorSourceInfo ? datagramPayload.SimulatorSourceInfo : _lastSimulatorSourceInfo,
            };
            FillMissingInformation(newSet);
            SanitizeData(newSet);
            return newSet;
        }

        private void SanitizeData(SimulatorDataSet simulatorDataSet)
        {
            simulatorDataSet.PlayerInfo ??= new DriverInfo();
            simulatorDataSet.DriversInfo ??= Array.Empty<DriverInfo>();
            simulatorDataSet.LeaderInfo ??= new DriverInfo();
        }

        private void FillMissingInformation(SimulatorDataSet simulatorDataSet)
        {
            simulatorDataSet.SimulatorSourceInfo ??= _lastSimulatorSourceInfo;

            simulatorDataSet.PlayerInfo ??= _lastPlayerInfo;
            simulatorDataSet.LeaderInfo ??= _lastLeaderInfo;

            if (simulatorDataSet.DriversInfo == null)
            {
                simulatorDataSet.DriversInfo = _lastDriversInfo;
                DriverInfo playerFromDrivers = simulatorDataSet.DriversInfo.FirstOrDefault(x => x.IsPlayer);
                if (playerFromDrivers != null)
                {
                    simulatorDataSet.PlayerInfo = playerFromDrivers;
                }

                simulatorDataSet.LeaderInfo = simulatorDataSet.DriversInfo.FirstOrDefault(x => x.Position == 1);
            }

            //Sanitize the input
            if (simulatorDataSet.DriversInfo == null && _lastPlayerInfo == null)
            {
                return;
            }

            int indexOfPlayer = simulatorDataSet.DriversInfo.IndexOf(x => x.DriverSessionId == _lastPlayerInfo.DriverSessionId);
            if (indexOfPlayer != -1)
            {
                simulatorDataSet.DriversInfo.Where(x => x.DriverSessionId != _lastPlayerInfo.DriverSessionId).ForEach(x => x.IsPlayer = false);
                simulatorDataSet.DriversInfo[indexOfPlayer].IsPlayer = _lastPlayerInfo.IsPlayer;
                simulatorDataSet.PlayerInfo = _lastPlayerInfo;
            }
        }

        private void ExtractLastData(DatagramPayload datagramPayload)
        {
            if (datagramPayload.ContainsOtherDriversTiming)
            {
                _lastDriversInfo = datagramPayload.DriversInfo ?? _lastDriversInfo;
                _lastLeaderInfo = datagramPayload.LeaderInfo ?? datagramPayload.LeaderInfo;
            }

            if (datagramPayload.ContainsPlayersTiming)
            {
                _lastPlayerInfo = datagramPayload.PlayerInfo ?? _lastPlayerInfo;
            }

            if (datagramPayload.ContainsSimulatorSourceInfo)
            {
                _lastSimulatorSourceInfo = datagramPayload.SimulatorSourceInfo ?? _lastSimulatorSourceInfo;
            }
        }
    }
}