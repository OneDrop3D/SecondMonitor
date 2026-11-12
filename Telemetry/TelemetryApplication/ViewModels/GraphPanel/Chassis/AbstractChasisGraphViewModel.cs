namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel.Chassis
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using DataModel.Telemetry;
    using OxyPlot;
    using OxyPlot.Series;

    using SecondMonitor.DataModel.Extensions;

    using TelemetryManagement.DTO;
    using TelemetryManagement.Settings;

    public abstract class AbstractChassisGraphViewModel : AbstractGraphViewModel
    {
        private const string FrontLabel = "Front";
        private const string RearLabel = "Rear";
        private bool _frontVisible;
        private bool _rearVisible;

        protected AbstractChassisGraphViewModel()
        {
            _frontVisible = true;
            _rearVisible = true;
        }

        public abstract override string Title { get; }

        public bool FrontVisible
        {
            get => _frontVisible;
            set
            {
                SetProperty(ref _frontVisible, value);
                UpdateSideVisibility();
            }
        }

        public bool RearVisible
        {
            get => _rearVisible;
            set
            {
                SetProperty(ref _rearVisible, value);
                UpdateSideVisibility();
            }
        }

        protected abstract override string YUnits { get; }

        protected abstract override double YTickInterval { get; }

        protected override bool CanYZoom => true;

        /*protected override void UpdateYMaximum(LapTelemetryDto lapTelemetry)
        {

        }*/

        protected abstract double GetFrontValue(SimulatorSourceInfo simulatorSourceInfo, CarInfo carInfo, CarPropertiesDto carPropertiesDto);

        protected abstract double GetRearValue(SimulatorSourceInfo simulatorSourceInfo, CarInfo carInfo, CarPropertiesDto carPropertiesDto);

        protected override List<LineSeries> GetLineSeries(LapSummaryDto lapSummary, CarPropertiesDto carPropertiesDto, List<TimedTelemetrySnapshot> dataPoints, OxyColor color)
        {
            LineSeries[] lineSeries = new LineSeries[2];
            string baseTitle = $"Lap {lapSummary.CustomDisplayName}";

            List<DataPoint> plotDataPoints = dataPoints.Select(x => new DataPoint(GetXValue(x), GetFrontValue(x.SimulatorSourceInfo, x.PlayerData.CarInfo, carPropertiesDto))).ToList();
            lineSeries[0] = CreateLineSeries(baseTitle + $" {FrontLabel}", color);
            lineSeries[0].IsVisible = FrontVisible;
            lineSeries[0].Points.AddRange(plotDataPoints);

            plotDataPoints = dataPoints.Select(x => new DataPoint(GetXValue(x), GetRearValue(x.SimulatorSourceInfo, x.PlayerData.CarInfo, carPropertiesDto))).ToList();
            double newMax = plotDataPoints.Max(x => x.Y);
            lineSeries[1] = CreateLineSeries(baseTitle + $" {RearLabel}", color);
            lineSeries[1].IsVisible = RearVisible;
            lineSeries[1].Points.AddRange(plotDataPoints);

            if (newMax > YMaximum)
            {
                YMaximum = newMax;
            }

            return lineSeries.ToList();
        }

        protected override string GetDataInfo(TimedTelemetrySnapshot timedTelemetrySnapshot, CarPropertiesDto carPropertiesDto)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Front: {GetFrontValue(timedTelemetrySnapshot.SimulatorSourceInfo, timedTelemetrySnapshot.PlayerData.CarInfo, carPropertiesDto).ToStringScalableDecimals()}");
            sb.AppendLine($"Rear: {GetRearValue(timedTelemetrySnapshot.SimulatorSourceInfo, timedTelemetrySnapshot.PlayerData.CarInfo, carPropertiesDto).ToStringScalableDecimals()}");
            return sb.ToString();
        }

        private void UpdateSideVisibility()
        {
            foreach (List<LineSeries> lineSeries in LoadedSeries.Values.Select(x => x.LineSeries))
            {
                foreach (LineSeries series in lineSeries)
                {
                    if (series.Title.Contains(FrontLabel))
                    {
                        series.IsVisible = FrontVisible;
                    }

                    if (series.Title.Contains(RearLabel))
                    {
                        series.IsVisible = RearVisible;
                    }
                }
            }

            InvalidatePlot();
        }
    }
}