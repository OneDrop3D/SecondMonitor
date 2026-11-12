namespace SecondMonitor.Rating.Common.Factories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using SecondMonitor.DataModel.Summary;

    internal class DriverAverageByLapTimes
    {
        public DriverAverageByLapTimes(Driver driver, int requiredLaps)
        {
            Driver = driver;
            LapTimes = new List<TimeSpan>(requiredLaps);
            TotalTimes = new List<TimeSpan>(requiredLaps);
            Positions = new List<int>(requiredLaps);

            TimeSpan replacementTime = TimeSpan.FromSeconds(driver.Laps.Average(x => x.LapTime.TotalSeconds));
            TimeSpan totalTime = TimeSpan.Zero;

            for (int i = 0; i < requiredLaps; i++)
            {
                TimeSpan lapTimeToUse;
                if (i < driver.Laps.Count)
                {
                    Lap driverLap = driver.Laps[i];
                    lapTimeToUse = driverLap.LapTime;
                    if (driverLap.IsPitExitLap)
                    {
                        PitStopSummary pitStop = driver.PitStopSummaries.FirstOrDefault(x => x.ExitLapNumber == driverLap.LapNumber);
                        lapTimeToUse -= pitStop?.TotalRaceTimeLost ?? TimeSpan.Zero;
                    }
                }
                else
                {
                    lapTimeToUse = replacementTime;
                }

                LapTimes.Add(lapTimeToUse);
                totalTime += lapTimeToUse;
                TotalTimes.Add(totalTime);
            }
        }

        public Driver Driver { get; }

        public List<TimeSpan> LapTimes { get; }

        public List<TimeSpan> TotalTimes { get; }

        public List<int> Positions { get; }
    }
}
