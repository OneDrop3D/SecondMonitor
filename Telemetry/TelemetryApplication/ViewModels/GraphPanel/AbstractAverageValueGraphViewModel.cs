namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel
{
    using System.Collections.Generic;
    using DataModel.Telemetry;
    using OxyPlot;
    using OxyPlot.Series;
    using TelemetryManagement.DTO;
    using TelemetryManagement.Settings;

    public abstract class AbstractAverageValueGraphViewModel : AbstractGraphViewModel
    {
        protected Dictionary<TimedTelemetrySnapshot, double> _values;

        protected AbstractAverageValueGraphViewModel()
        {
            _values = new Dictionary<TimedTelemetrySnapshot, double>();
        }

        protected override List<LineSeries> GetLineSeries(LapSummaryDto lapSummary, CarPropertiesDto carPropertiesDto, List<TimedTelemetrySnapshot> dataPoints, OxyColor color)
        {
            LineSeries newLineSeries = CreateLineSeries($"Lap {lapSummary.CustomDisplayName}", color);
            List<DataPoint> points = new List<DataPoint>(dataPoints.Count);
            DataPoint oldPoint = new DataPoint();
            for (int i = 0; i < dataPoints.Count - 1; i++)
            {
                var dp1 = dataPoints[i];
                var dp2 = dataPoints[i + 1];

                double value = GetYValue(dp1, dp2, carPropertiesDto);

                DataPoint oxyPoint = new DataPoint((GetXValue(dp1) + GetXValue(dp2)) / 2, value);
                if (i != 0 && !IsValid(oldPoint, oxyPoint))
                {
                    continue;
                }

                oldPoint = oxyPoint;
                points.Add(oxyPoint);
                _values.Add(dp1, value);
            }

            newLineSeries.Points.AddRange(points);

            /*LineSeries newLineSeries2 = CreateLineSeries($"Lap {lapSummary.CustomDisplayName} Angle", OxyColors.Wheat);
            List<DataPoint> points2 = new List<DataPoint>(dataPoints.Length - 1);
            for (int i = 0; i < dataPoints.Length - 1; i++)
            {
                var dp1 = dataPoints[i];
                var dp2 = dataPoints[i + 1];

                double angle = (dp1.PlayerData.WorldPosition.X.InMeters - dp2.PlayerData.WorldPosition.X.InMeters);
                DataPoint oxyPoint = new DataPoint((GetXValue(dp1) + GetXValue(dp2)) / 2,  angle );
                points2.Add(oxyPoint);
            }

            newLineSeries2.Points.AddRange(points2);*/
            List<LineSeries> series = new List<LineSeries>(1) { newLineSeries };
            return series;
        }

        protected override string GetDataInfo(TimedTelemetrySnapshot timedTelemetrySnapshot, CarPropertiesDto carPropertiesDto)
        {
            return _values.TryGetValue(timedTelemetrySnapshot, out double value) ? $"Y :{value:G4}" : string.Empty;
        }

        protected abstract double GetYValue(TimedTelemetrySnapshot dp1, TimedTelemetrySnapshot dp2, CarPropertiesDto carPropertiesDto);

        protected abstract bool IsValid(DataPoint oldPoint, DataPoint newPoint);
    }
}