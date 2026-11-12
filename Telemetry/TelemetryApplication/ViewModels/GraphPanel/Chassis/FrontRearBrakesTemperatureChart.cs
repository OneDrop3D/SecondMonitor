namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel.Chassis
{
    using System.Collections.Generic;
    using System.Linq;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Systems;
    using OxyPlot;
    using OxyPlot.Annotations;
    using TelemetryManagement.DTO;
    using TelemetryManagement.Settings;

    public class FrontRearBrakesTemperatureChart : AbstractChassisGraphViewModel
    {
        private LineAnnotation _hotTempAnnotation;
        private LineAnnotation _coldTempAnnotation;

        public override string Title => "Front / Rear Brakes Temperatures";
        protected override string YUnits => Temperature.GetUnitSymbol(UnitsCollection.TemperatureUnits);
        protected override double YTickInterval => 100;

        protected override double GetFrontValue(SimulatorSourceInfo simulatorSourceInfo, CarInfo carInfo, CarPropertiesDto carPropertiesDto)
        {
            return Temperature.FromCelsius((carInfo.WheelsInfo.FrontLeft.BrakeTemperature.ActualQuantity.InCelsius + carInfo.WheelsInfo.FrontRight.BrakeTemperature.ActualQuantity.InCelsius) / 2).GetValueInUnits(UnitsCollection.TemperatureUnits);
        }

        protected override double GetRearValue(SimulatorSourceInfo simulatorSourceInfo, CarInfo carInfo, CarPropertiesDto carPropertiesDto)
        {
            return Temperature.FromCelsius((carInfo.WheelsInfo.RearLeft.BrakeTemperature.ActualQuantity.InCelsius + carInfo.WheelsInfo.RearRight.BrakeTemperature.ActualQuantity.InCelsius) / 2).GetValueInUnits(UnitsCollection.TemperatureUnits);
        }

        protected override void PreviewOnLapLoaded(LapTelemetryDto lapTelemetryDto, CarPropertiesDto carPropertiesDto)
        {
            base.PreviewOnLapLoaded(lapTelemetryDto, carPropertiesDto);
            var telemetrySnapshot = lapTelemetryDto.DataPoints.FirstOrDefault(x => x.PlayerData?.CarInfo.WheelsInfo.FrontLeft.BrakeTemperature.IdealQuantity.IsZero == false && !x.PlayerData.CarInfo.WheelsInfo.FrontLeft.BrakeTemperature.IdealQuantityWindow.IsZero);
            if (telemetrySnapshot == null)
            {
                RemoveLineAnnotations();
                return;
            }

            AddLineAnnotations();
            OptimalQuantity<Temperature> brakeTemperature = telemetrySnapshot.PlayerData.CarInfo.WheelsInfo.FrontLeft.BrakeTemperature;
            _hotTempAnnotation.Y = Temperature.FromCelsius(100).GetValueInUnits(UnitsCollection.TemperatureUnits) + Temperature.FromCelsius(brakeTemperature.IdealQuantity.InCelsius + brakeTemperature.IdealQuantityWindow.InCelsius).GetValueInUnits(UnitsCollection.TemperatureUnits);
            _coldTempAnnotation.Y = Temperature.FromCelsius(brakeTemperature.IdealQuantity.InCelsius - brakeTemperature.IdealQuantityWindow.InCelsius).GetValueInUnits(UnitsCollection.TemperatureUnits);
        }

        private void AddLineAnnotations()
        {
            if (_hotTempAnnotation != null && _coldTempAnnotation != null)
            {
                return;
            }

            _hotTempAnnotation = new LineAnnotation() { Color = OxyColor.Parse("#ff0000"), StrokeThickness = 2, Type = LineAnnotationType.Horizontal, LineStyle = LineStyle.Solid };
            _coldTempAnnotation = new LineAnnotation() { Color = OxyColor.Parse("#668cff"), StrokeThickness = 2, Type = LineAnnotationType.Horizontal, LineStyle = LineStyle.Solid };
            AddLineAnnotations(new List<Annotation>() { _hotTempAnnotation, _coldTempAnnotation });
        }

        private void RemoveLineAnnotations()
        {
            if (_hotTempAnnotation == null || _coldTempAnnotation == null)
            {
                return;
            }

            RemoveLineAnnotations(new List<Annotation>() { _hotTempAnnotation, _coldTempAnnotation });
        }
    }
}