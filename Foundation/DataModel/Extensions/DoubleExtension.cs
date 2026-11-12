namespace SecondMonitor.DataModel.Extensions
{
    using System;

    public static class DoubleExtension
    {
        public static double ConvertRange(this double value, // value to convert
            double originalStart, double originalEnd, // original range
            double newStart, double newEnd)
        {
            double scale = (newEnd - newStart) / (originalEnd - originalStart);
            return (newStart + ((value - originalStart) * scale));
        }

        public static double ConvertRange(this float value, // value to convert
            double originalStart, double originalEnd, // original range
            double newStart, double newEnd) => ConvertRange((double)value, originalStart, originalEnd, newStart, newEnd);

        public static double Lerp(double double1, double double2, double lambda)
        {
            return double1 + ((double2 - double1) * lambda);
        }
        
        public static string ToStringScalableDecimals(this double valueD)
        {
            double valueAbs = Math.Abs(valueD);
            return valueD == 0 ? "0" : valueAbs < 0 ? valueD.ToString("F3") : valueAbs < 10 ? valueD.ToString("F2") : valueAbs < 100 ? valueD.ToString("F1") : valueD.ToString("F0");
        }

        public static string ToStringLimitDecimals(this double valueD)
        {
            return valueD == 0 ? "0" : valueD < 0 ? valueD.ToString("G4") : valueD.ToString("F0");
        }

        public static bool IsBetween(this double value, double min, double max, bool inclusive = false)
        {
            return inclusive ? min <= value && value <= max : min < value & value < max;
        }
    }
}