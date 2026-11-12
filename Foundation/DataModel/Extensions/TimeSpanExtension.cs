namespace SecondMonitor.DataModel.Extensions
{
    using System;

    public static class TimeSpanExtension
    {
        public const string DefaultFormat = "mm\\:ss\\.fff";

        public const string SecondOnly = "ss\\.fff";

        public static string FormatToDefault(this TimeSpan timeSpan)
        {
            return timeSpan.ToString(DefaultFormat);
        }

        public static string FormatTimeSpanOnlySeconds(this TimeSpan timeSpan, bool includeSign = true)
        {
            if (!includeSign)
            {
                return timeSpan.ToString(SecondOnly);
            }

            if (timeSpan < TimeSpan.Zero)
            {
                return "-" + timeSpan.ToString(SecondOnly);
            }
            else
            {
                return "+" + timeSpan.ToString(SecondOnly);
            }
        }

        public static string FormatToMinutesSeconds(this TimeSpan timeSpan)
        {
            return ((int)(timeSpan.TotalSeconds / 60)) + ":"
                + ((int)timeSpan.TotalSeconds % 60).ToString("00");
        }

        public static string FormatToHoursMinutesSeconds(this TimeSpan timeSpan)
        {
            return timeSpan.Hours <= 0 ? timeSpan.FormatToMinutesSeconds() : $"{timeSpan.Hours:0}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
        }

        public static string FormatToHoursMinutesSecondsUnitSuffix(this TimeSpan timeSpan)
        {
            return timeSpan.Hours <= 0 ? $"{((int)(timeSpan.TotalSeconds / 60))}min {((int)timeSpan.TotalSeconds % 60):00}sec" : $"{timeSpan.Hours:0}h {timeSpan.Minutes:00}min {timeSpan.Seconds:00}sec";
        }
    }
}