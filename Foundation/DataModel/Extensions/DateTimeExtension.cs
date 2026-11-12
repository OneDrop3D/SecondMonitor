namespace SecondMonitor.DataModel.Extensions
{
    using System;

    public static class DateTimeExtension
    {
        public static DateTime Next(this DateTime from, DayOfWeek dayOfWeek)
        {
            int start = (int)from.DayOfWeek;
            int target = (int)dayOfWeek;
            if (target <= start)
            {
                target += 7;
            }

            return from.AddDays(target - start);
        }

        public static DateTime Previous(this DateTime from, DayOfWeek dayOfWeek)
        {
            int start = (int)from.DayOfWeek;
            int target = (int)dayOfWeek;
            if (target >= start)
            {
                target -= 7;
            }

            return from.AddDays(target - start);
        }
    }
}