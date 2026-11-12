namespace SecondMonitor.Rating.Common.Factories
{
    using System.Collections.Generic;

    using SecondMonitor.DataModel.Summary;

    public interface IDriverAveragePositionAdapter
    {
        IReadOnlyDictionary<string, double> GetAveragePositions(SessionSummary sessionSummary);
    }
}
