namespace SecondMonitor.PCarsConnector.Enums
{
    using System.ComponentModel;

    public enum EFlagReason
    {
        [Description("No Reason")]
        FlagReasonNone = 0,

        [Description("Solo Crash")]
        FlagReasonSoloCrash,

        [Description("Vehicle Crash")]
        FlagReasonVehicleCrash,

        [Description("Vehicle Obstruction")]
        FlagReasonVehicleObstruction,

        // -------------
        FlagReasonMax
    }
}
