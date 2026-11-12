namespace SecondMonitor.Telemetry.TelemetryApplication.Checks.Evaluators.BrakeTemperatures
{
    using DataModel.BasicProperties;
    using DataModel.Snapshot.Systems;
    using DataModel.Telemetry;
    using Result;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.Settings;
    using Statistics;
    using TelemetryManagement.DTO;

    public class BrakeTemperatures : AbstractTelemetryEvaluator<BrakeTemperaturesViewModel>
    {
        private const string BrakesTooCold = " brakes are getting too cold during the lap. Cold brakes do not offer optimal performance. So you might not be getting full braking performance the first meters it takes the brakes to get into temperature." +
            "\n\nClose the braking ducts (if possible in car setup) a little to increase the brakes temperatures. If you're unable to find a setup without having cold or hot brakes, cold brakes are preferable.";

        private const string BrakesTooHotShort = " brakes are getting too hot at end of braking zones. Hot brakes do not offer optimal braking performance." +
            "\n\nOpenning the braking ducts (if possible in car setup) a little to increase brake cooling might be beneficial. If you're unable to find a setup without having cold or hot brakes, cold brakes are preferable.";

        private const string BrakesTooHotLong = " brakes are getting too hot for extensive period of time. Too hot brakes might lead to brake fade, which significantly reduces braking performance. It might also degrate the brakes longetivity." +
                                                 "\n\nOpenning the braking ducts (if possible in car setup) a little to increase brake cooling might be beneficial. If you're unable to find a setup without having cold or hot brakes, cold brakes are preferable.";

        private QuantityWithLimitsStatistics _frontLeftStatistics;
        private QuantityWithLimitsStatistics _frontRightStatistics;
        private QuantityWithLimitsStatistics _rearLeftStatistics;
        private QuantityWithLimitsStatistics _rearRightStatistics;

        public BrakeTemperatures(IViewModelFactory viewModelFactory, ISettingsProvider settingsProvider) : base(viewModelFactory, settingsProvider)
        {
        }

        public override void ProcessDataPoints(TimedTelemetrySnapshot telemetrySnapshot)
        {
            var wheelsInfo = telemetrySnapshot.PlayerData.CarInfo.WheelsInfo;
            _frontLeftStatistics.ProcessDataPoint(wheelsInfo.FrontLeft.BrakeTemperature.ActualQuantity.InCelsius);
            _frontRightStatistics.ProcessDataPoint(wheelsInfo.FrontRight.BrakeTemperature.ActualQuantity.InCelsius);
            _rearLeftStatistics.ProcessDataPoint(wheelsInfo.RearLeft.BrakeTemperature.ActualQuantity.InCelsius);
            _rearRightStatistics.ProcessDataPoint(wheelsInfo.RearRight.BrakeTemperature.ActualQuantity.InCelsius);
        }

        public override void FinishEvaluation()
        {
            ViewModel.FrontLeftResult = CreateEvaluationResults(_frontLeftStatistics, "Front Left");
            ViewModel.FrontRightResult = CreateEvaluationResults(_frontRightStatistics, "Front Right");
            ViewModel.RearLeftResult = CreateEvaluationResults(_rearLeftStatistics, "Rear Left");
            ViewModel.RearRightResult = CreateEvaluationResults(_rearRightStatistics, "Rear Right");
        }

        private EvaluationResult CreateEvaluationResults(QuantityWithLimitsStatistics quantityStatistics, string title)
        {
            EvaluationResult evaluationResult = new EvaluationResult(title);
            double maximalTemperatureExceededPercentage = ComputeTimeSpentPercentage(quantityStatistics.PointsOverLimit);
            double minimalTemperatureExceededPercentage = ComputeTimeSpentPercentage(quantityStatistics.PointsBelowLimit);
            double averageTemperature = quantityStatistics.Average;

            double tdMax = quantityStatistics.MaxValue - quantityStatistics.HighLimit;
            double tdMin = quantityStatistics.MinValue - quantityStatistics.LowLimit;

            EvaluationResultKind minimalEvaluationResult = EvaluationResultKind.Ok;
            EvaluationResultKind maximalEvaluationResult = EvaluationResultKind.Ok;
            EvaluationResultKind averageResultKind = EvaluationResultKind.Ok;

            if (averageTemperature > quantityStatistics.HighLimit)
            {
                averageResultKind = EvaluationResultKind.Error;
            }
            else if (averageTemperature < quantityStatistics.LowLimit)
            {
                averageResultKind = EvaluationResultKind.Information;
            }

            if (maximalTemperatureExceededPercentage > 10)
            {
                AddIssue(EvaluationResultKind.Error, title + " brakes very Hot", title + BrakesTooHotLong);
                minimalEvaluationResult = EvaluationResultKind.None;
                maximalEvaluationResult = EvaluationResultKind.Error;
            }
            else if (maximalTemperatureExceededPercentage > 0)
            {
                AddIssue(EvaluationResultKind.Warning, title + " brakes hot", title + BrakesTooHotShort);
                minimalEvaluationResult = EvaluationResultKind.None;
                maximalEvaluationResult = EvaluationResultKind.Warning;
            }
            else if (minimalTemperatureExceededPercentage > 0)
            {
                AddIssue(EvaluationResultKind.Information, title + " brakes cold", title + BrakesTooCold);
                maximalEvaluationResult = EvaluationResultKind.None;
                minimalEvaluationResult = EvaluationResultKind.Information;
            }

            evaluationResult.AddItem("Cold Brakes Limit:", Temperature.FromCelsius(quantityStatistics.LowLimit).GetValueInUnits(TemperatureUnits).ToStringScalableDecimals() + Temperature.GetUnitSymbol(TemperatureUnits), EvaluationResultKind.None);
            evaluationResult.AddItem("Hot Brakes Limit:", Temperature.FromCelsius(quantityStatistics.HighLimit).GetValueInUnits(TemperatureUnits).ToStringScalableDecimals() + Temperature.GetUnitSymbol(TemperatureUnits), EvaluationResultKind.None);

            evaluationResult.AddSeparator();

            evaluationResult.AddItem("Minimal Temperature:", Temperature.FromCelsius(quantityStatistics.MinValue).GetValueInUnits(TemperatureUnits).ToStringScalableDecimals() + Temperature.GetUnitSymbol(TemperatureUnits), minimalEvaluationResult);
            evaluationResult.AddItem("Maximum Temperature:", Temperature.FromCelsius(quantityStatistics.MaxValue).GetValueInUnits(TemperatureUnits).ToStringScalableDecimals() + Temperature.GetUnitSymbol(TemperatureUnits), maximalEvaluationResult);
            evaluationResult.AddItem("Average Temperature:", Temperature.FromCelsius(quantityStatistics.Average).GetValueInUnits(TemperatureUnits).ToStringScalableDecimals() + Temperature.GetUnitSymbol(TemperatureUnits), averageResultKind);

            evaluationResult.AddSeparator();

            evaluationResult.AddItem("% of Lap with Hot Brakes:", maximalTemperatureExceededPercentage.ToStringScalableDecimals() + "%", maximalEvaluationResult);
            evaluationResult.AddItem("% of Lap with Cold Brakes:", minimalTemperatureExceededPercentage.ToStringScalableDecimals() + "%", minimalEvaluationResult);

            evaluationResult.AddSeparator();

            evaluationResult.AddItem("Td to Hot:", Temperature.FromCelsius(tdMax).GetValueInUnits(TemperatureUnits).ToStringScalableDecimals() + Temperature.GetUnitSymbol(TemperatureUnits), maximalEvaluationResult);
            evaluationResult.AddItem("Td to Cold:", Temperature.FromCelsius(tdMin).GetValueInUnits(TemperatureUnits).ToStringScalableDecimals() + Temperature.GetUnitSymbol(TemperatureUnits), minimalEvaluationResult);

            return evaluationResult;
        }

        protected override void StartEvaluationInternal(LapTelemetryDto lapTelemetryDto)
        {
            var wheelsInfo = lapTelemetryDto.DataPoints[0].PlayerData.CarInfo.WheelsInfo;
            _frontLeftStatistics = GetWheelStatistics(wheelsInfo.FrontLeft);
            _frontRightStatistics = GetWheelStatistics(wheelsInfo.FrontRight);
            _rearLeftStatistics = GetWheelStatistics(wheelsInfo.RearLeft);
            _rearRightStatistics = GetWheelStatistics(wheelsInfo.RearRight);
        }

        private static QuantityWithLimitsStatistics GetWheelStatistics(WheelInfo wheel)
        {
            return new QuantityWithLimitsStatistics(
                wheel.BrakeTemperature.IdealQuantity.InCelsius - wheel.BrakeTemperature.IdealQuantityWindow.InCelsius,
                wheel.BrakeTemperature.IdealQuantity.InCelsius + wheel.BrakeTemperature.IdealQuantityWindow.InCelsius + 100,
                wheel.BrakeTemperature.IdealQuantity.InCelsius);
        }
    }
}