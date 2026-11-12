namespace SecondMonitor.Telemetry.TelemetryApplication.Checks.Evaluators.Engine
{
    using System;
    using DataModel;
    using DataModel.BasicProperties;
    using DataModel.Telemetry;
    using Result;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.Settings;
    using TelemetryManagement.DTO;

    public class EngineTemperature : AbstractTelemetryEvaluator<GenericResultViewModel>
    {
        private OptimalQuantity<Temperature> _maximumOilTemperature;
        private OptimalQuantity<Temperature> _maximumWaterTemperature;

        public EngineTemperature(IViewModelFactory viewModelFactory, ISettingsProvider settingsProvider) : base(viewModelFactory, settingsProvider, "Engine Temperature",
            new[] { SimulatorsNameMap.PCars2SimName, SimulatorsNameMap.Ams2SimName }, Array.Empty<string>())
        {
        }

        public override void ProcessDataPoints(TimedTelemetrySnapshot telemetrySnapshot)
        {
            if (telemetrySnapshot.PlayerData.CarInfo.OilSystemInfo.OptimalOilTemperature.ActualQuantity > _maximumOilTemperature.ActualQuantity)
            {
                _maximumOilTemperature = telemetrySnapshot.PlayerData.CarInfo.OilSystemInfo.OptimalOilTemperature;
            }

            if (telemetrySnapshot.PlayerData.CarInfo.WaterSystemInfo.OptimalWaterTemperature.ActualQuantity > _maximumWaterTemperature.ActualQuantity)
            {
                _maximumWaterTemperature = telemetrySnapshot.PlayerData.CarInfo.WaterSystemInfo.OptimalWaterTemperature;
            }
        }

        public override void FinishEvaluation()
        {
            EvaluationResult evaluationResult = new EvaluationResult("Engine Temps");

            EvaluationResultKind waterTemperatureResult = AddEvaluation("Water Temperature:", _maximumWaterTemperature, evaluationResult);
            EvaluationResultKind oilTemperatureResult = AddEvaluation("Oil Temperature:", _maximumOilTemperature, evaluationResult);

            if (waterTemperatureResult == EvaluationResultKind.Information && oilTemperatureResult == EvaluationResultKind.Information)
            {
                AddIssue(EvaluationResultKind.Information, "Water & Oil Temperature", "Water and Oil temperature is low, you can safely decrease radiator opening. This should reduce drag. Be aware that ambient temperature changes might require opening the radiator again.");
            }

            ViewModel.FromModel(evaluationResult);
        }

        private EvaluationResultKind AddEvaluation(string title, OptimalQuantity<Temperature> maximumTemperature, EvaluationResult evaluationResult)
        {
            double temperatureInC = maximumTemperature.ActualQuantity.InCelsius;
            double thresholdWaterTemperature = maximumTemperature.IdealQuantity.InCelsius + maximumTemperature.IdealQuantityWindow.InCelsius;
            double overheatingTemperature = thresholdWaterTemperature + maximumTemperature.IdealQuantityWindow.InCelsius;
            double coldEngineTemperature = maximumTemperature.IdealQuantity.InCelsius - maximumTemperature.IdealQuantityWindow.InCelsius;

            EvaluationResultKind evaluationResultKind = EvaluationResultKind.Ok;
            if (temperatureInC > overheatingTemperature)
            {
                evaluationResultKind = EvaluationResultKind.Error;
                AddIssue(evaluationResultKind, title, "Temperature is too high, increase the radiator opening to avoid damage to the engine.");
            }
            else if (temperatureInC > thresholdWaterTemperature)
            {
                evaluationResultKind = EvaluationResultKind.Warning;
                AddIssue(evaluationResultKind, title, "Temperature is slightly high, short runs should be OK, but for prolonged runs, increase the radiator opening to avoid damage to the engine.");
            }
            else if (temperatureInC < coldEngineTemperature)
            {
                evaluationResultKind = EvaluationResultKind.Information;
            }

            evaluationResult.AddItem(title, "Maximum Temperature:", maximumTemperature.ActualQuantity.GetFormattedWithUnits(0, TemperatureUnits), evaluationResultKind);
            evaluationResult.AddItem(title, "Target Temperature:", Temperature.FromCelsius(thresholdWaterTemperature).GetFormattedWithUnits(0, TemperatureUnits), EvaluationResultKind.None);

            return evaluationResultKind;
        }

        protected override void StartEvaluationInternal(LapTelemetryDto lapTelemetryDto)
        {
            _maximumOilTemperature = new OptimalQuantity<Temperature>();
            _maximumWaterTemperature = new OptimalQuantity<Temperature>();
        }
    }
}