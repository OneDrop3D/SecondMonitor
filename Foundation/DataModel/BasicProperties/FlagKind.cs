namespace SecondMonitor.DataModel.BasicProperties
{
    using System;

    [Flags]
    public enum FlagKind
    {
        None = 0,
        YellowSector1 = 1,
        YellowSector2 = 1 << 1,
        YellowSector3 = 1 << 2,
        FullCourseYellow = 1 << 3,
        VirtualSafetyCar = 1 << 4,
        SafetyCar = 1 << 5,
        Red = 1 << 6
    }
}