namespace SecondMonitor.DataModel.Extensions
{
    using System;

    public static class IntExtension
    {
        public static int RoundOff(this int i)
        {
            return ((int)Math.Round(i / 10.0)) * 10;
        }
    }
}
