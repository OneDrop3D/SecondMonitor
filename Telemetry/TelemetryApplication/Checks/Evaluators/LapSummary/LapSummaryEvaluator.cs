namespace SecondMonitor.Telemetry.TelemetryApplication.Checks.Evaluators.LapSummary
{
    using System;
    using DataModel.BasicProperties;
    using DataModel.Snapshot.Systems;
    using DataModel.Telemetry;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.Settings;
    using TelemetryManagement.DTO;

    public class LapSummaryEvaluator : AbstractTelemetryEvaluator<LapSummaryViewModel>
    {
        private const string NoTimeOnFullBrake =
            "During the lap, there was no datapoint where the brake is fully depressed. If you're pressing the brake pedal fully, then this indicates that the brake pedals are not calibrated correctly, and never report full brake depression to the brake. " +
            "\n\nIf you're locking the tyres, then lowering the maximal brake pressure in the car setup should be considered, as there is no use of brake pressures that exceed tyres grip.";

        private const string TooLittleTimeOnFullBrake =
            "Only less than 10% of braking is done with full brakes. This can indicate that you're not braking hard enough, so losing times in braking zones." +
                "\n\nAnother reason might be the tyres locking. In such cases it might be beneficial to try to lower the maximal brake pressure in the car setup. ";

        private Velocity _topSpeed;
        private int _throttlePointsCount;
        private int _fullThrottlePointsCount;

        private int _brakePointsCount;
        private int _fullBrakePointsCount;
        private bool _wasRain;

        public LapSummaryEvaluator(IViewModelFactory viewModelFactory, ISettingsProvider settingsProvider) : base(viewModelFactory, settingsProvider)
        {
        }

        protected override void StartEvaluationInternal(LapTelemetryDto lapTelemetryDto)
        {
            _topSpeed = Velocity.Zero;
            var firstDataPoint = lapTelemetryDto.DataPoints[0];
            var lastDataPoint = lapTelemetryDto.DataPoints[lapTelemetryDto.DataPoints.Count - 1];

            ViewModel.TotalFuel = firstDataPoint.PlayerData.CarInfo.FuelSystemInfo.FuelRemaining.GetValueInUnits(VolumeUnits).ToStringScalableDecimals() + Volume.GetUnitSymbol(VolumeUnits);
            ViewModel.TotalFuelConsumed = (firstDataPoint.PlayerData.CarInfo.FuelSystemInfo.FuelRemaining.GetValueInUnits(VolumeUnits) - lastDataPoint.PlayerData.CarInfo.FuelSystemInfo.FuelRemaining.GetValueInUnits(VolumeUnits)).ToStringScalableDecimals() + Volume.GetUnitSymbol(VolumeUnits);
            ViewModel.LapTime = lapTelemetryDto.LapSummary.LapTime.FormatToDefault();

            ViewModel.AverageSpeed = Velocity.FromMs(lapTelemetryDto.LapSummary.LayoutLength / lapTelemetryDto.LapSummary.LapTimeSeconds).GetValueInUnits(VelocityUnits).ToStringScalableDecimals() + Velocity.GetUnitSymbol(VelocityUnits);

            ViewModel.FrontLeftTyreWear = GetTyreWear(x => x.FrontLeft, firstDataPoint, lastDataPoint).ToStringScalableDecimals();
            ViewModel.FrontRightTyreWear = GetTyreWear(x => x.FrontRight, firstDataPoint, lastDataPoint).ToStringScalableDecimals();
            ViewModel.RearLeftTyreWear = GetTyreWear(x => x.RearLeft, firstDataPoint, lastDataPoint).ToStringScalableDecimals();
            ViewModel.RearRightTyreWear = GetTyreWear(x => x.RearRight, firstDataPoint, lastDataPoint).ToStringScalableDecimals();

            _wasRain = firstDataPoint.WeatherInfo.RainIntensity > 0;

            if (firstDataPoint.WeatherInfo.AirTemperature.IsZero || firstDataPoint.WeatherInfo.TrackTemperature.IsZero)
            {
                return;
            }

            ViewModel.AirTemperature = firstDataPoint.WeatherInfo.AirTemperature.GetValueInUnits(TemperatureUnits).ToStringScalableDecimals() + Temperature.GetUnitSymbol(TemperatureUnits);
            ViewModel.TrackTemperature = firstDataPoint.WeatherInfo.TrackTemperature.GetValueInUnits(TemperatureUnits).ToStringScalableDecimals() + Temperature.GetUnitSymbol(TemperatureUnits);
            ViewModel.RainLevel = firstDataPoint.WeatherInfo.RainIntensity.ToString() + "%";
        }

        public override void ProcessDataPoints(TimedTelemetrySnapshot telemetrySnapshot)
        {
            var input = telemetrySnapshot.InputInfo;
            if (_topSpeed < telemetrySnapshot.PlayerData.Speed)
            {
                _topSpeed = telemetrySnapshot.PlayerData.Speed;
            }

            if (input.ThrottlePedalPosition > 0.01)
            {
                _throttlePointsCount++;
            }

            if (input.ThrottlePedalPosition > 0.99)
            {
                _fullThrottlePointsCount++;
            }

            if (input.BrakePedalPosition > 0.01)
            {
                _brakePointsCount++;
            }

            if (input.BrakePedalPosition > 0.99)
            {
                _fullBrakePointsCount++;
            }
        }

        public override void FinishEvaluation()
        {
            double spentOnBrake = ComputeTimeSpentPercentage(_brakePointsCount);
            double spentOnFullBrake = ComputeTimeSpentPercentage(_fullBrakePointsCount);

            ViewModel.TopSpeed = _topSpeed.GetValueInUnits(VelocityUnits).ToStringScalableDecimals() + Velocity.GetUnitSymbol(VelocityUnits);
            ViewModel.TimeSpentOnThrottle = ComputeTimeSpentPercentage(_throttlePointsCount).ToStringScalableDecimals() + "%";
            ViewModel.TimeSpentOnFullThrottle = ComputeTimeSpentPercentage(_fullThrottlePointsCount).ToStringScalableDecimals() + "%";
            ViewModel.TimeSpentOnBrake = spentOnBrake.ToStringScalableDecimals() + "%";
            ViewModel.TimeSpentOnFullBrake = spentOnFullBrake.ToStringScalableDecimals() + "%";

            double fullBrakeRatio = spentOnFullBrake / spentOnBrake;
            
            ViewModel.FullBrakePercentage = (fullBrakeRatio * 100).ToStringScalableDecimals() + "%";

            if (fullBrakeRatio == 0 && !_wasRain)
            {
                ViewModel.BrakeEvaluationResultKind = EvaluationResultKind.Error;
                AddIssue(EvaluationResultKind.Error, "No Full Brake Application", NoTimeOnFullBrake);
            } else if (fullBrakeRatio < 0.1 && !_wasRain)
            {
                ViewModel.BrakeEvaluationResultKind = EvaluationResultKind.Warning;
                AddIssue(EvaluationResultKind.Warning, "Low Full Brake Application", TooLittleTimeOnFullBrake);
            }
        }

        private static double GetTyreWear(Func<Wheels, WheelInfo> selectionFunc, TimedTelemetrySnapshot firstSnapshot, TimedTelemetrySnapshot lastSnapshot)
        {
            return (selectionFunc(lastSnapshot.PlayerData.CarInfo.WheelsInfo).TyreWear.ActualWear
                   - selectionFunc(firstSnapshot.PlayerData.CarInfo.WheelsInfo).TyreWear.ActualWear) * 100;
        }
    }
}