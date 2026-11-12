namespace SecondMonitor.PCarsConnector.Enums
{
    using System;
    using System.ComponentModel;

    [Flags]
    public enum ECarFlags
    {
        [Description("None")]
        None = 0,
        [Description("Headlight")]
        CarHeadlight = 1,
        [Description("Engine Active")]
        CarEngineActive = 2,
        [Description("Engine Warning")]
        CarEngineWarning = 4,
        [Description("Speed Limiter")]
        CarSpeedLimiter = 8,
        [Description("ABS")]
        CarAbs = 16,
        [Description("Handbrake")]
        CarHandbrake = 32
    }
}
