namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel.DataExtractor
{
    using System;
    using DataModel.Telemetry;
    using Settings.DTO;
    using TelemetryManagement.DTO;
    using TelemetryManagement.Settings;

    public class SimpleSingleSeriesDataExtractor : ISingleSeriesDataExtractor
    {
        public event EventHandler<EventArgs> DataRefreshRequested
        {
            add { } remove { }
        }

        public string ExtractorName => "Normal";

        public double GetValue(Func<TimedTelemetrySnapshot, CarPropertiesDto, double> valueExtractFunction, TimedTelemetrySnapshot telemetrySnapshot, CarPropertiesDto carPropertiesDto, XAxisKind xAxisKind)
        {
            return valueExtractFunction(telemetrySnapshot, carPropertiesDto);
        }

        public bool ShowLapGraph(LapSummaryDto lapSummaryDto)
        {
            return true;
        }

        public void Dispose()
        {
        }
    }
}