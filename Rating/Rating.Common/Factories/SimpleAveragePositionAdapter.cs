namespace SecondMonitor.Rating.Common.Factories
{
    using System.Collections.Generic;
    using System.Linq;

    using SecondMonitor.DataModel.Summary;

    public class SimpleAveragePositionAdapter : IDriverAveragePositionAdapter
    {
        public IReadOnlyDictionary<string, double> GetAveragePositions(SessionSummary sessionSummary)
        {
            return sessionSummary.Drivers.ToDictionary(x => x.DriverId, x => x.AveragePosition).AsReadOnly();
        }
    }
}
