namespace SecondMonitor.Rating.Common.Factories
{
    using System.Collections.Generic;
    using System.Linq;
    using DataModel;
    using SecondMonitor.DataModel.Summary;

    public class SessionFinishStateFactory(IDriverAveragePositionAdapter driverAveragePositionAdapter) : ISessionFinishStateFactory
    {
        public SessionFinishState Create(SessionSummary sessionSummary)
        {
            IReadOnlyDictionary<string, double> averagePositions = driverAveragePositionAdapter.GetAveragePositions(sessionSummary);
            return new SessionFinishState(sessionSummary.TrackInfo.TrackFullName, sessionSummary.Drivers.Select(x => 
                new DriverFinishState(x.DriverId, x.IsPlayer, x.DriverLongName, x.CarName, x.ClassName, x.FinishingPosition, averagePositions.GetValueOrDefault(x.DriverId, double.MaxValue))));
        }
    }
}