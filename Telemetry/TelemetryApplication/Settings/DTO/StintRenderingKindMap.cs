namespace SecondMonitor.Telemetry.TelemetryApplication.Settings.DTO
{
    using Contracts;

    public class StintRenderingKindMap : AbstractHumanReadableMap<StintRenderingKind>
    {
        public StintRenderingKindMap()
        {
            Translations.Add(StintRenderingKind.None, "None (All Laps in single chart)");
            Translations.Add(StintRenderingKind.SingleChart, "Separate series for each stint");
            Translations.Add(StintRenderingKind.MultipleCharts, "Separate chart for each stint");
        }
    }
}