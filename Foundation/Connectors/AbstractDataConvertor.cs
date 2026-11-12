namespace SecondMonitor.Foundation.Connectors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DataModel.BasicProperties;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Snapshot.Drivers;

    public abstract class AbstractDataConvertor
    {
        private readonly List<(DriverInfo DriverInfo, DateTime StartOfYellow)> _driversCausingYellow = new();

        protected static void ComputeDistanceToPlayer(DriverInfo player, DriverInfo driverInfo, double trackLength)
        {
            if (player == null)
            {
                return;
            }

            if (driverInfo.FinishStatus == DriverFinishStatus.Dq || driverInfo.FinishStatus == DriverFinishStatus.Dnf ||
                driverInfo.FinishStatus == DriverFinishStatus.Dnq)
            {
                driverInfo.DistanceToPlayer = double.MaxValue;
                return;
            }

            double playerLapDistance = player.LapDistance;

            double distanceToPlayer = playerLapDistance - driverInfo.LapDistance;
            if (distanceToPlayer < -(trackLength / 2))
            {
                distanceToPlayer = distanceToPlayer + trackLength;
            }

            if (distanceToPlayer > (trackLength / 2))
            {
                distanceToPlayer = distanceToPlayer - trackLength;
            }

            driverInfo.DistanceToPlayer = distanceToPlayer;
        }
 
        protected static void PopulateClassPositions(SimulatorDataSet dataSet)
        {
            List<IGrouping<string, DriverInfo>> classPotions = dataSet.DriversInfo.GroupBy(x => x.CarClassId).ToList();

            dataSet.SessionInfo.IsMultiClass = classPotions.Count > 1;

            foreach (IGrouping<string, DriverInfo> classPotion in classPotions)
            {
                int position = 1;
                foreach (DriverInfo driverInfo in classPotion.OrderBy(x => x.Position))
                {
                    driverInfo.PositionInClass = position;
                    position++;
                }
            }
        }

        protected void PopulateDriversCausingYellow(SimulatorDataSet data)
        {
            if (data.SessionInfo.ActiveFlags.HasFlag(FlagKind.YellowSector1) || data.SessionInfo.ActiveFlags.HasFlag(FlagKind.YellowSector2) || data.SessionInfo.ActiveFlags.HasFlag(FlagKind.YellowSector3))
            {
                data.DriversInfo.Where(driverInfo => !driverInfo.InPits && driverInfo.FinishStatus == DriverFinishStatus.None && driverInfo.Speed.InKph < 1)
                    .ForEach(x =>
                    {
                        _driversCausingYellow.Add((x, DateTime.UtcNow));
                    });
            }

            foreach ((DriverInfo DriverInfo, DateTime StartOfYellow) driverYellowInfo in _driversCausingYellow.ToList())
            {
                DriverInfo driverCausingYellow = data.DriversInfo.FirstOrDefault(x => x.DriverSessionId == driverYellowInfo.DriverInfo.DriverSessionId);
                if (driverCausingYellow != null)
                {
                    driverCausingYellow.IsCausingYellow = true;
                }

                if ((DateTime.UtcNow - driverYellowInfo.StartOfYellow).TotalSeconds > 5)
                {
                }
            }
        }
    }
}