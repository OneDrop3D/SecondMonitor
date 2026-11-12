namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.Histogram.Extractors
{
    using System;
    using Controllers.Settings;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using SecondMonitor.ViewModels.Settings;
    using TelemetryManagement.Settings;

    public class CamberHistogramExtractor : AbstractWheelHistogramDataExtractor
    {
        public CamberHistogramExtractor(ISettingsProvider settingsProvider, ISettingsController settingsController) : base(settingsProvider, settingsController)
        {
        }

        public double IdealCamber { get; set; } = 0;

        protected override bool IsZeroBandInMiddle => false;
        public override string YUnit => Angle.GetUnitsSymbol(AngleUnits);
        public override double DefaultBandSize => Angle.GetFromDegrees(0.10).GetValueInUnits(AngleUnits);
        protected override Func<SimulatorSourceInfo, WheelInfo, CarPropertiesDto, double> WheelValueExtractor => GetCamber;

        //TODO: 8.6 Figure ideal camber
        private double GetCamber(SimulatorSourceInfo simulatorSourceInfo, WheelInfo wheelInfo, CarPropertiesDto carPropertiesDto)
        {
            return -IdealCamber + wheelInfo.Camber.GetValueInUnits(AngleUnits);
        }
    }
}