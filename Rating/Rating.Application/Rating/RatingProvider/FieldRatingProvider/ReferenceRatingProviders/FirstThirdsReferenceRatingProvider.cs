namespace SecondMonitor.Rating.Application.Rating.RatingProvider.FieldRatingProvider.ReferenceRatingProviders
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DataModel.Summary;

    public class FirstThirdsReferenceRatingProvider : IReferenceRatingProvider
    {
        public TimeSpan GetReferenceTime(IEnumerable<Driver> orderedDrivers)
        {
            List<Driver> orderedDriversEnumerated = orderedDrivers.ToList();
            List<Driver> driversWithTime = orderedDriversEnumerated.Where(x => x.BestPersonalLap != null && x.IsPlayer == false).ToList();
            return TimeSpan.FromSeconds(driversWithTime.Take(driversWithTime.Count / 3).Average(x => x.BestPersonalLap.LapTime.TotalSeconds));
        }

        public int GetReferenceDriverIndex(int fieldSize)
        {
            return fieldSize / 3;
        }
    }
}