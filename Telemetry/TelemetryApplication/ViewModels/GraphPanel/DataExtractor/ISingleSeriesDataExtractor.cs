namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel.DataExtractor
{
    using System;
    using DataModel.Telemetry;
    using Settings.DTO;
    using TelemetryManagement.DTO;
    using TelemetryManagement.Settings;

    public interface ISingleSeriesDataExtractor : IDisposable
    {
        event EventHandler<EventArgs> DataRefreshRequested;

        string ExtractorName { get; }

        double GetValue(Func<TimedTelemetrySnapshot, CarPropertiesDto, double> valueExtractFunction, TimedTelemetrySnapshot telemetrySnapshot, CarPropertiesDto carPropertiesDto, XAxisKind xAxisKind);

        bool ShowLapGraph(LapSummaryDto lapSummaryDto);
    }
}