namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using DataAdapters;
    using DataModel.BasicProperties;
    using DataModel.Telemetry;
    using OxyPlot;
    using OxyPlot.Series;
    using TelemetryManagement.DTO;
    using TelemetryManagement.Settings;

    public class CrossTyreLoadChartViewModel : AbstractGraphViewModel
    {
        private readonly TyreLoadAdapter _tyreLoadAdapter;
        private string _title = "Cross Tyre Load";

        public CrossTyreLoadChartViewModel(TyreLoadAdapter tyreLoadAdapter)
        {
            _tyreLoadAdapter = tyreLoadAdapter;
        }

        public override bool IsCarSettingsDependant => true;

        public override string Title => _title;
        protected override string YUnits => Force.GetUnitSymbol(UnitsCollection.ForceUnits);
        protected override double YTickInterval => Force.GetFromNewtons(2500).GetValueInUnits(UnitsCollection.ForceUnits);

        protected override bool CanYZoom => true;

        protected override List<LineSeries> GetLineSeries(LapSummaryDto lapSummary, CarPropertiesDto carPropertiesDto, List<TimedTelemetrySnapshot> dataPoints, OxyColor color)
        {
            LineSeries[] lineSeries = new LineSeries[2];
            string baseTitle = $"Lap {lapSummary.CustomDisplayName}";

            List<DataPoint> plotDataPoints = dataPoints.Select(x => new DataPoint(GetXValue(x), GetFlRrValue(x, carPropertiesDto))).ToList();
            lineSeries[0] = CreateLineSeries(baseTitle + " (FL + RR)", OxyColors.Green);
            lineSeries[0].Points.AddRange(plotDataPoints);

            plotDataPoints = dataPoints.Select(x => new DataPoint(GetXValue(x), GetFrRlValue(x, carPropertiesDto))).ToList();
            lineSeries[1] = CreateLineSeries(baseTitle + " (FR + RL)", OxyColors.Blue);
            lineSeries[1].Points.AddRange(plotDataPoints);

            YMaximum = 100;
            YMinimum = 0;

            return lineSeries.ToList();
        }

        private double GetFrRlValue(TelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto)
        {
            return _tyreLoadAdapter.GetQuantityFromWheel(snapshot.SimulatorSourceInfo, snapshot.PlayerData.CarInfo.WheelsInfo.FrontRight, carPropertiesDto).GetValueInUnits(UnitsCollection.ForceUnits) +
                   _tyreLoadAdapter.GetQuantityFromWheel(snapshot.SimulatorSourceInfo, snapshot.PlayerData.CarInfo.WheelsInfo.RearLeft, carPropertiesDto).GetValueInUnits(UnitsCollection.ForceUnits);
        }

        private double GetFlRrValue(TelemetrySnapshot snapshot, CarPropertiesDto carPropertiesDto)
        {
            return _tyreLoadAdapter.GetQuantityFromWheel(snapshot.SimulatorSourceInfo, snapshot.PlayerData.CarInfo.WheelsInfo.FrontLeft, carPropertiesDto).GetValueInUnits(UnitsCollection.ForceUnits) +
                   _tyreLoadAdapter.GetQuantityFromWheel(snapshot.SimulatorSourceInfo, snapshot.PlayerData.CarInfo.WheelsInfo.RearRight, carPropertiesDto).GetValueInUnits(UnitsCollection.ForceUnits);
        }

        protected override string GetDataInfo(TimedTelemetrySnapshot timedTelemetrySnapshot, CarPropertiesDto carPropertiesDto)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"FL + RR: {GetFlRrValue(timedTelemetrySnapshot, carPropertiesDto):G4}");
            sb.AppendLine($"FR + RL: {GetFrRlValue(timedTelemetrySnapshot, carPropertiesDto):G4}");
            return sb.ToString();
        }

        protected override void PreviewOnLapLoaded(LapTelemetryDto lapTelemetryDto, CarPropertiesDto carPropertiesDto)
        {
            var firstDataPoint = lapTelemetryDto.DataPoints.FirstOrDefault();
            string newTitle;
            if (firstDataPoint == null || !_tyreLoadAdapter.IsQuantityComputed(firstDataPoint.SimulatorSourceInfo))
            {
                newTitle = "Cross Tyre Load";
            }
            else
            {
                newTitle = "Cross Suspension Load (C)";
            }

            if (newTitle != _title)
            {
                _title = newTitle;
                NotifyPropertyChanged(nameof(Title));
            }

            base.PreviewOnLapLoaded(lapTelemetryDto, carPropertiesDto);
        }

        protected override void ApplyNewLineColor(List<LineSeries> series, OxyColor newColor)
        {
        }
    }
}