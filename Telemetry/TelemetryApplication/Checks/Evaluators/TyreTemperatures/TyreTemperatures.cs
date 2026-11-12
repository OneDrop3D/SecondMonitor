namespace SecondMonitor.Telemetry.TelemetryApplication.Checks.Evaluators.TyreTemperatures
{
    using System.Linq;
    using DataModel;
    using DataModel.BasicProperties;
    using DataModel.Snapshot.Systems;
    using DataModel.Telemetry;
    using Result;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.Settings;
    using Statistics;
    using TelemetryManagement.DTO;

    public class TyreTemperatures : AbstractTelemetryEvaluator<TyreTemperaturesViewModel>
    {
        private const string TyreTempOverheating =
            " tyres are temporarily crossing the upper operation temperature boundary. While this will most likely not cause immediate problems, it is important to know that driving any more aggresively, or any increase in track temperature will cause the tyre to overheat more. \r\n\nOverheating tyres have lower grip and wear more quickly. It may also cause blistering (i.e., in ACC), which further reduce tyre life, and also grip, even if the tyre returns back to normal temperatures.\r\n\nDriving style will always have the biggest impact on tyre temperatures, but there are few setup changes that can be done to lower the temperatures.\r\n\nMoving the toe closer to zero will reduce tyre friction and thus reducing temperatures. Softening the suspension will cause the tyre to squish less over bumps and during load movement, also reducing the temperatures. Increasing tyre pressures will make the tyres more stiffs, thus reducing the amount of deformation they go through, thus reducing temperatures. Just be careful to keep your tyres within the ideal pressure window.\r\n\nAlso, it might be not possible to reduce tyre temperatures sufficiently, especially on hot summer days. Then you need to find balance between pace and tyre longevity";

        private const string TyreOverheating =
            " tyres are overheating.\r\n\nOverheating tyres have lower grip and wear more quickly. It may also cause blistering (i.e., in ACC), which further reduce tyre life, and also grip, even if the tyre returns back to normal temperatures.\r\n\nDriving style will always have the biggest impact on tyre temperatures, but there are few setup changes that can be done to lower the temperatures.\r\n\nMoving the toe closer to zero will reduce tyre friction and thus reducing temperatures. Softening the suspension will cause the tyre to squish less over bumps and during load movement, also reducing the temperatures. Increasing tyre pressures will make the tyres more stiffs, thus reducing the amount of deformation they go through, thus reducing temperatures. Just be careful to keep your tyres within the ideal pressure window.\r\n\nAlso, it might be not possible to reduce tyre temperatures sufficiently, especially on hot summer days. Then you need to find balance between pace and tyre longevity";

        private const string TyreCold =
            " tyres are getting cold.\r\n\nCold tyres will have lower grip. In some simulators (i.e. ACC), working cold tyres hard can cause graining, which reduce tyre life and tyre grip. The grip reduction will preserve even when tyres get heated up. \r\n\nDriving style will always have the biggest impact on tyre temperatures, but there are few setup changes that can be done to increase the temperatures.\r\n\nMoving the toe further from zero will increase tyre friction and thus increase temperatures. Harder suspension force tyres to absorb more energy from bumps and weight transfers, increasing their temperatures. Lowering tyre pressures will make the tyres to deform more, also increasing tyre temperatures. Just be careful that the pressures stay within the ideal range.\r\n\nOn cold days it might even not be possible to get enough energy into the tyres to warm them up properly. There you don’t have any other option, then to drive on cold tyres.\r\n\n";

        private const string TyreCamberTempHot =
            "The temperature difference between outer and inner part of the tyre is too big.\r\n\nThis usually indicates too aggressive camber on the tyre, which cause the inner part of the tyre to work significantly more than the outer. Lowering the camper will most likely fix this issue. Try to keep the temperature difference to around 10 degrees maximum.\r\n\n";

        private const string TyreCamberTempCold =
            "There is very small temperatures difference between outer and inner part.\r\n\nThis not a problem by itself, but indicates that it is possible to add more camber to tyres, should you require more grip in high-speed corners.\r\n\n";

        private static readonly string[] SimWithoutCamberEvaluation = { SimulatorsNameMap.AccSimName, SimulatorsNameMap.F12019, SimulatorsNameMap.F12020, SimulatorsNameMap.F12021 };

        private TyreTemperaturesStatistics _frontLeftStatistics;
        private TyreTemperaturesStatistics _frontRightStatistics;
        private TyreTemperaturesStatistics _rearLeftStatistics;
        private TyreTemperaturesStatistics _rearRightStatistics;
        private bool _camberEvaluationDisabled;

        public TyreTemperatures(IViewModelFactory viewModelFactory, ISettingsProvider settingsProvider) : base(viewModelFactory, settingsProvider)
        {
        }

        public override void ProcessDataPoints(TimedTelemetrySnapshot telemetrySnapshot)
        {
            var wheelsInfo = telemetrySnapshot.PlayerData.CarInfo.WheelsInfo;
            _frontLeftStatistics.ProcessDataPoint(wheelsInfo.FrontLeft);
            _frontRightStatistics.ProcessDataPoint(wheelsInfo.FrontRight);
            _rearLeftStatistics.ProcessDataPoint(wheelsInfo.RearLeft);
            _rearRightStatistics.ProcessDataPoint(wheelsInfo.RearRight);
        }

        public override void FinishEvaluation()
        {
            ViewModel.FrontLeftResult = CreateEvaluationResults(_frontLeftStatistics, "Front Left");
            ViewModel.FrontRightResult = CreateEvaluationResults(_frontRightStatistics, "Front Right");
            ViewModel.RearLeftResult = CreateEvaluationResults(_rearLeftStatistics, "Rear Left");
            ViewModel.RearRightResult = CreateEvaluationResults(_rearRightStatistics, "Rear Right");
        }

        private EvaluationResult CreateEvaluationResults(TyreTemperaturesStatistics tyreStats, string title)
        {
            EvaluationResult evaluationResult = new EvaluationResult(title);

            evaluationResult.AddItem("Cold Tyres Limit:", Temperature.FromCelsius(tyreStats.CenterPortionStatistics.LowLimit).GetValueInUnits(TemperatureUnits).ToStringScalableDecimals() + Temperature.GetUnitSymbol(TemperatureUnits), EvaluationResultKind.None);
            evaluationResult.AddItem("Hot Tyres Limit:", Temperature.FromCelsius(tyreStats.CenterPortionStatistics.HighLimit).GetValueInUnits(TemperatureUnits).ToStringScalableDecimals() + Temperature.GetUnitSymbol(TemperatureUnits), EvaluationResultKind.None);

            QuantityWithLimitsStatistics coldestPart = tyreStats.GetColdestPart();
            QuantityWithLimitsStatistics hottestPart = tyreStats.GetHottestPart();

            double maximalTemperatureExceededPercentage = ComputeTimeSpentPercentage(hottestPart.PointsOverLimit);
            double minimalTemperatureExceededPercentage = ComputeTimeSpentPercentage(coldestPart.PointsBelowLimit);
            double averageTemperature = (tyreStats.LeftPortionStatistics.Average +
                                         tyreStats.RightPortionStatistics.Average +
                                         tyreStats.CenterPortionStatistics.Average +
                                         tyreStats.CorePortionStatistics.Average) / 4.0;

            double tdMax = hottestPart.MaxValue - hottestPart.HighLimit;
            double tdMin = coldestPart.MinValue - coldestPart.LowLimit;

            EvaluationResultKind minimalEvaluationResult = EvaluationResultKind.Ok;
            EvaluationResultKind maximalEvaluationResult = EvaluationResultKind.Ok;
            EvaluationResultKind averageResultKind = EvaluationResultKind.Ok;

            if (averageTemperature > hottestPart.HighLimit)
            {
                averageResultKind = EvaluationResultKind.Error;
            }
            else if (averageTemperature < coldestPart.LowLimit)
            {
                averageResultKind = EvaluationResultKind.Information;
            }

            if (maximalTemperatureExceededPercentage > 25)
            {
                AddIssue(EvaluationResultKind.Error, title + " Tyres very hot", title + TyreOverheating);
                minimalEvaluationResult = EvaluationResultKind.None;
                maximalEvaluationResult = EvaluationResultKind.Error;
            }
            else if (maximalTemperatureExceededPercentage > 0)
            {
                AddIssue(EvaluationResultKind.Warning, title + " tyres hot", title + TyreTempOverheating);
                minimalEvaluationResult = EvaluationResultKind.None;
                maximalEvaluationResult = EvaluationResultKind.Warning;
            }
            else if (minimalTemperatureExceededPercentage > 0)
            {
                AddIssue(EvaluationResultKind.Information, title + " tyres cold", title + TyreCold);
                maximalEvaluationResult = EvaluationResultKind.None;
                minimalEvaluationResult = EvaluationResultKind.Information;
            }

            evaluationResult.AddSeparator();

            evaluationResult.AddItem("Minimal Temperature:", Temperature.FromCelsius(coldestPart.MinValue).GetValueInUnits(TemperatureUnits).ToStringScalableDecimals() + Temperature.GetUnitSymbol(TemperatureUnits), minimalEvaluationResult);
            evaluationResult.AddItem("Maximum Temperature:", Temperature.FromCelsius(hottestPart.MaxValue).GetValueInUnits(TemperatureUnits).ToStringScalableDecimals() + Temperature.GetUnitSymbol(TemperatureUnits), maximalEvaluationResult);
            evaluationResult.AddItem("Average Temperature:", Temperature.FromCelsius(averageTemperature).GetValueInUnits(TemperatureUnits).ToStringScalableDecimals() + Temperature.GetUnitSymbol(TemperatureUnits), averageResultKind);

            evaluationResult.AddSeparator();
            evaluationResult.AddItem("% of Lap with Hot Tyres:", maximalTemperatureExceededPercentage.ToStringScalableDecimals() + "%", maximalEvaluationResult);
            evaluationResult.AddItem("% of Lap with Cold Tyres:", minimalTemperatureExceededPercentage.ToStringScalableDecimals() + "%", minimalEvaluationResult);

            evaluationResult.AddSeparator();

            evaluationResult.AddItem("Td to Hot:", Temperature.FromCelsius(tdMax).GetValueInUnits(TemperatureUnits).ToStringScalableDecimals() + Temperature.GetUnitSymbol(TemperatureUnits), maximalEvaluationResult);
            evaluationResult.AddItem("Td to Cold:", Temperature.FromCelsius(tdMin).GetValueInUnits(TemperatureUnits).ToStringScalableDecimals() + Temperature.GetUnitSymbol(TemperatureUnits), minimalEvaluationResult);

            if (_camberEvaluationDisabled)
            {
                return evaluationResult;
            }

            double outerInnerAverage = tyreStats.OuterInnerDiffStatistics.Average;

            EvaluationResultKind averageOuterInnerEvaluationResult = EvaluationResultKind.Ok;
            if (outerInnerAverage > 12)
            {
                AddIssue(EvaluationResultKind.Error, title + " o-i difference too big.", TyreCamberTempHot);
                averageOuterInnerEvaluationResult = EvaluationResultKind.Error;
            }
            else if (outerInnerAverage > 10)
            {
                AddIssue(EvaluationResultKind.Warning, title + " o-i difference too big.", TyreCamberTempHot);
                averageOuterInnerEvaluationResult = EvaluationResultKind.Warning;
            }
            else if (outerInnerAverage < 2)
            {
                AddIssue(EvaluationResultKind.Information, title + " o-i difference small.", TyreCamberTempCold);
                averageOuterInnerEvaluationResult = EvaluationResultKind.Information;
            }

            evaluationResult.AddSeparator();
            evaluationResult.AddItem("Outer - Inner Temperature Differences:", string.Empty, EvaluationResultKind.None);
            evaluationResult.AddItem("Minimal O-I difference:", Temperature.FromCelsius(tyreStats.OuterInnerDiffStatistics.MinValue).GetValueInUnits(TemperatureUnits).ToStringScalableDecimals() + Temperature.GetUnitSymbol(TemperatureUnits), averageOuterInnerEvaluationResult);
            evaluationResult.AddItem("Maximum O-I difference:", Temperature.FromCelsius(tyreStats.OuterInnerDiffStatistics.MaxValue).GetValueInUnits(TemperatureUnits).ToStringScalableDecimals() + Temperature.GetUnitSymbol(TemperatureUnits), averageOuterInnerEvaluationResult);
            evaluationResult.AddItem("Average O-I difference:", Temperature.FromCelsius(outerInnerAverage).GetValueInUnits(TemperatureUnits).ToStringScalableDecimals() + Temperature.GetUnitSymbol(TemperatureUnits), averageOuterInnerEvaluationResult);

            return evaluationResult;
        }

        protected override void StartEvaluationInternal(LapTelemetryDto lapTelemetryDto)
        {
            _camberEvaluationDisabled = SimWithoutCamberEvaluation.Contains(lapTelemetryDto.LapSummary.Simulator);

            var wheelsInfo = lapTelemetryDto.DataPoints[0].PlayerData.CarInfo.WheelsInfo;
            _frontLeftStatistics = CreateTyreStatistics(wheelsInfo.FrontLeft);
            _frontRightStatistics = CreateTyreStatistics(wheelsInfo.FrontRight);
            _rearLeftStatistics = CreateTyreStatistics(wheelsInfo.RearLeft);
            _rearRightStatistics = CreateTyreStatistics(wheelsInfo.RearRight);
        }

        private static TyreTemperaturesStatistics CreateTyreStatistics(WheelInfo wheel)
        {
            return new TyreTemperaturesStatistics(wheel);
        }
    }
}