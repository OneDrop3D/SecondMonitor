namespace SecondMonitor.Rating.Common.Factories
{
    using System.Collections.Generic;
    using System.Linq;
    using NLog;

    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.DataModel.Snapshot.Drivers;
    using SecondMonitor.DataModel.Summary;

    public class PitCompensateAveragePositionAdapter(IDriverAveragePositionAdapter backupAdapter) : IDriverAveragePositionAdapter
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public IReadOnlyDictionary<string, double> GetAveragePositions(SessionSummary sessionSummary)
        {
            Driver player = sessionSummary.Drivers.FirstOrDefault(x => x.IsPlayer);

            if (sessionSummary.SessionType != SessionType.Race || player == null || sessionSummary.PitStops.Count == 0)
            {
                return backupAdapter.GetAveragePositions(sessionSummary);
            }

            List<Driver> eligibleDrivers = sessionSummary.Drivers.Where(x => x.ClassId == player.ClassId && x.FinishStatus is DriverFinishStatus.Finished or DriverFinishStatus.None).ToList();
            int lapsToConsider = eligibleDrivers.Min(x => x.Laps.Count);
            List<DriverAverageByLapTimes> driverAverageByLapTimesList = eligibleDrivers.Select(x => new DriverAverageByLapTimes(x, lapsToConsider)).ToList();

            for (int i = 0; i < lapsToConsider; i++)
            {
                List<(DriverAverageByLapTimes item, int index)> driverAverageByLapTimesListOrdered = driverAverageByLapTimesList.OrderBy(x => x.TotalTimes[i])
                    .Select((item, index) => (item, index))
                    .ToList();
                driverAverageByLapTimesListOrdered.ForEach(x => x.item.Positions.Add(x.index + 1));
            }

            Dictionary<string, double> driverToAvgPosition =
                driverAverageByLapTimesList.ToDictionary(x => x.Driver.DriverId, x => x.Positions.Average(position => (double)position));

            sessionSummary.Drivers.Where(x => x.ClassId != player.ClassId).ForEach(x => driverToAvgPosition.Add(x.DriverId, double.MaxValue));

            logger.Info("Average Positions for Rating: ");
            logger.Info(string.Join("\n", driverToAvgPosition.OrderBy(x => x.Value).Select(x => $"Name: {x.Key}, Pos: {x.Value}")));

            return driverToAvgPosition;
        }
    }
}