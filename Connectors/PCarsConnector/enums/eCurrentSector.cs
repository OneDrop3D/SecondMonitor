namespace SecondMonitor.PCarsConnector.Enums
{
    using System.ComponentModel;

    public enum ECurrentSector
    {
        [Description("Invalid Sector")]
        SectorInvalid = 0,

        [Description("Sector Start")]
        SectorStart,

        [Description("Sector 1")]
        SectorSector1,

        [Description("Sector 2")]
        SectorSector2,

        [Description("Sector 3")]
        SectorFinish,

        [Description("Sector Stop??")]
        SectorStop,

        // -------------
        SectorMax
    }
}