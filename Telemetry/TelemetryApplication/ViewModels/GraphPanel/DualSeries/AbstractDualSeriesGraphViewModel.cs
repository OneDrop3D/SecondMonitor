namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel.DualSeries
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using DataModel.Telemetry;
    using OxyPlot;
    using OxyPlot.Series;
    using TelemetryManagement.DTO;
    using TelemetryManagement.Settings;

    public abstract class AbstractDualSeriesGraphViewModel : AbstractGraphViewModel
    {
        protected abstract string Series1Title { get; }

        protected abstract string Series2Title { get; }

        protected abstract Func<TimedTelemetrySnapshot, double> Series1ExtractFunc { get; }

        protected abstract Func<TimedTelemetrySnapshot, double> Series2ExtractFunc { get; }

        protected abstract OxyColor Series1Color { get; }

        protected abstract OxyColor Series2Color { get; }

        protected override List<LineSeries> GetLineSeries(LapSummaryDto lapSummary, CarPropertiesDto carPropertiesDto, List<TimedTelemetrySnapshot> dataPoints, OxyColor color)
        {
            LineSeries[] lineSeries = new LineSeries[2];
            string baseTitle = $"Lap {lapSummary.CustomDisplayName}";

            List<DataPoint> plotDataPoints = dataPoints.Select(x => new DataPoint(GetXValue(x), Series1ExtractFunc(x))).ToList();
            lineSeries[0] = CreateLineSeries($"{baseTitle} {Series1Title}", Series1Color);
            lineSeries[0].Points.AddRange(plotDataPoints);

            double newMax = plotDataPoints.Max(x => x.Y);
            double newMin = plotDataPoints.Min(x => x.Y);

            plotDataPoints = dataPoints.Select(x => new DataPoint(GetXValue(x), Series2ExtractFunc(x))).ToList();
            lineSeries[1] = CreateLineSeries($"{baseTitle} {Series2Title}", Series2Color);
            lineSeries[1].Points.AddRange(plotDataPoints);

            newMax = Math.Max(newMax, plotDataPoints.Max(x => x.Y));
            newMin = Math.Min(newMin, plotDataPoints.Min(x => x.Y));

            if (newMax > YMaximum)
            {
                YMaximum = newMax;
            }

            if (newMin < YMinimum || YMinimum == 0)
            {
                YMinimum = newMin;
            }

            var series = lineSeries.ToList();
            OnSeriesCreated(series);
            return series;
        }

        protected override void ApplyNewLineColor(List<LineSeries> series, OxyColor newColor)
        {
        }

        protected virtual void OnSeriesCreated(List<LineSeries> lineSeries)
        {
        }

        protected override string GetDataInfo(TimedTelemetrySnapshot timedTelemetrySnapshot, CarPropertiesDto carPropertiesDto)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{Series1Title}: {Series1ExtractFunc(timedTelemetrySnapshot):G4}");
            sb.AppendLine($"{Series1Title}: {Series2ExtractFunc(timedTelemetrySnapshot):G4}");
            return sb.ToString();
        }
    }
}