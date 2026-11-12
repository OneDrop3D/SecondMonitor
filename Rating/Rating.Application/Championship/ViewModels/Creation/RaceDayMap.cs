namespace SecondMonitor.Rating.Application.Championship.ViewModels.Creation
{
    using System;

    using SecondMonitor.Contracts;
    using SecondMonitor.DataModel.Championship;

    public class RaceDayMap : AbstractHumanReadableMap<RaceDay>
    {
        public RaceDayMap()
        {
            Translations.Add(RaceDay.Monday, "Monday");
            Translations.Add(RaceDay.Tuesday, "Tuesday");
            Translations.Add(RaceDay.Wednesday, "Wednesday");
            Translations.Add(RaceDay.Thursday, "Thursday");
            Translations.Add(RaceDay.Friday, "Friday");
            Translations.Add(RaceDay.Saturday, "Saturday");
            Translations.Add(RaceDay.Sunday, "Sunday");
            Translations.Add(RaceDay.Any, "Any");
            Translations.Add(RaceDay.Random, "Random");
        }

        public static bool IsDayOfWeek(RaceDay raceDay)
        {
            return raceDay is not RaceDay.Any and RaceDay.Random;
        }

        public static DayOfWeek GetDayOfWeek(RaceDay raceDay)
        {
            return raceDay switch
            {
                RaceDay.Monday => DayOfWeek.Monday,
                RaceDay.Tuesday => DayOfWeek.Tuesday,
                RaceDay.Wednesday => DayOfWeek.Wednesday,
                RaceDay.Thursday => DayOfWeek.Thursday,
                RaceDay.Friday => DayOfWeek.Friday,
                RaceDay.Saturday => DayOfWeek.Saturday,
                RaceDay.Sunday => DayOfWeek.Sunday,
                RaceDay.Any or RaceDay.Random => throw new ArgumentOutOfRangeException(nameof(raceDay), raceDay, null),
                _ => throw new ArgumentOutOfRangeException(nameof(raceDay), raceDay, null)
            };
        }
    }
}
