namespace SecondMonitor.Telemetry.TelemetryApplication.Checks.Evaluators.TyrePressures
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

    public class TyrePressures : AbstractTelemetryEvaluator<TyrePressuresViewModel>
    {
        private const string MinorOverInflationMessage =
            " average pressure is crossing the upper ideal limit. Further increase in pressure will cause overinflation.  Overinflating tyres will make the middle portion of the tyre tread to bulge out, reducing the contact patch size, reducing grip. This also make the middle portion of the tyre warmer than the rest, because it is providing the majority of the grip. Reduce tyre pressure roughly by the amount showed in the ‘Pd of Average to Ideal’. This is an iterative process, and keep in mind that most setup and weather changes will result in tyre pressure changes. Tyre pressure should be evaluated after each session.";

        private const string MajorOverInflationMessage =
            " is heavily overinflated. Overinflating the tyres will make the middle portion of the tyre tread to bulge out, reducing the contact patch size, reducing grip. This also make the middle portion of the tyre warmer than the rest, because it is providing the majority of the grip. Reduce tyre pressure roughly by the amount showed in the ‘Pd of Average to Ideal’. This is an iterative process, and keep in mind that most setup and weather changes will result in tyre pressure changes. Tyre pressure should be evaluated after each session.";

        private const string MinorUnderInflationMessage =
            " average pressure is crossing the lower ideal limit. Further decrease in pressure will cause underinflation.  Underinflating tyres will make the middle portion of the tyre tread to bulge in, reducing the contact patch size, reducing grip. This also make the middle portion of the tyre colder than the rest, because it has very little of contact with the road. Underinflated tyres have more rolling resistance, and are also more susceptible to punctures on curbing.  Increase tyre pressure roughly by the amount showed in the ‘Pd of Average to Ideal’. This is an iterative process, and keep in mind that most setup and weather changes will result in tyre pressure changes. Tyre pressure should be evaluated after each session.";

        private const string MajorUnderInflationMessage = " is heavily underinflated. Further decrease in pressure will cause underinflation.  Underinflating tyres will make the middle portion of the tyre tread to bulge in, reducing the contact patch size, reducing grip. This also make the middle portion of the tyre colder than the rest, because it has very little of contact with the road. Underinflated tyres have more rolling resistance, and are also more susceptible to punctures on curbing.  Increase tyre pressure roughly by the amount showed in the ‘Pd of Average to Ideal’. This is an iterative process, and keep in mind that most setup and weather changes will result in tyre pressure changes. Tyre pressure should be evaluated after each session.";

        private MovingIdealQuantityStatistics _frontLeftStatistics;
        private MovingIdealQuantityStatistics _frontRightStatistics;
        private MovingIdealQuantityStatistics _rearLeftStatistics;
        private MovingIdealQuantityStatistics _rearRightStatistics;
        private bool _hasIdealPressureDefined;

        public TyrePressures(IViewModelFactory viewModelFactory, ISettingsProvider settingsProvider) : base(viewModelFactory, settingsProvider)
        {
        }

        public override void ProcessDataPoints(TimedTelemetrySnapshot telemetrySnapshot)
        {
            var wheelsInfo = telemetrySnapshot.PlayerData.CarInfo.WheelsInfo;
            ProcessDataPoint(wheelsInfo.FrontLeft, _frontLeftStatistics);
            ProcessDataPoint(wheelsInfo.FrontRight, _frontRightStatistics);
            ProcessDataPoint(wheelsInfo.RearLeft, _rearLeftStatistics);
            ProcessDataPoint(wheelsInfo.RearRight, _rearRightStatistics);
        }

        private void ProcessDataPoint(WheelInfo wheel, MovingIdealQuantityStatistics quantityStatistics)
        {
            quantityStatistics.ProcessDataPoint(wheel.TyrePressure.ActualQuantity.InKpa,
                wheel.TyrePressure.IdealQuantity.InKpa - (wheel.TyrePressure.IdealQuantityWindow.InKpa * 1.5),
                wheel.TyrePressure.IdealQuantity.InKpa + (wheel.TyrePressure.IdealQuantityWindow.InKpa * 1.5),
                wheel.TyrePressure.IdealQuantity.InKpa);
        }

        public override void FinishEvaluation()
        {
            ViewModel.FrontLeftResult = CreateEvaluationResults(_frontLeftStatistics, "Front Left");
            ViewModel.FrontRightResult = CreateEvaluationResults(_frontRightStatistics, "Front Right");
            ViewModel.RearLeftResult = CreateEvaluationResults(_rearLeftStatistics, "Rear Left");
            ViewModel.RearRightResult = CreateEvaluationResults(_rearRightStatistics, "Rear Right");
        }

        private EvaluationResult CreateEvaluationResults(MovingIdealQuantityStatistics quantityStatistics, string title)
        {
            EvaluationResult evaluationResult = new EvaluationResult(title);
            double maximalPressureExceededPercentage = ComputeTimeSpentPercentage(quantityStatistics.PointsOverLimit);
            double minimalPressureExceededPercentage = ComputeTimeSpentPercentage(quantityStatistics.PointsBelowLimit);
            double idealValue = quantityStatistics.IdealValueAverage;
            double averagePressure = quantityStatistics.Average;

            double operationalWindowRange = quantityStatistics.HighLimitAverage - quantityStatistics.LowLimitAverage;

            double pdMax = averagePressure - quantityStatistics.HighLimitAverage;
            double pdMin = averagePressure - quantityStatistics.LowLimitAverage;
            double pdAverage = quantityStatistics.DistanceToIdealAverage;

            EvaluationResultKind minimalEvaluationResult = EvaluationResultKind.Ok;
            EvaluationResultKind maximalEvaluationResult = EvaluationResultKind.Ok;
            EvaluationResultKind averageResultKind = EvaluationResultKind.Ok;

            if (!_hasIdealPressureDefined)
            {
                evaluationResult.AddItem("Minimum Pressure:", Pressure.FromKiloPascals(quantityStatistics.MinValue).GetValueInUnits(PressureUnits).ToStringScalableDecimals() + Pressure.GetUnitSymbol(PressureUnits),  minimalEvaluationResult);
                evaluationResult.AddItem("Maximum Pressure:", Pressure.FromKiloPascals(quantityStatistics.MaxValue).GetValueInUnits(PressureUnits).ToStringScalableDecimals() + Pressure.GetUnitSymbol(PressureUnits), maximalEvaluationResult);
                evaluationResult.AddItem("Average Pressure:", Pressure.FromKiloPascals(quantityStatistics.Average).GetValueInUnits(PressureUnits).ToStringScalableDecimals() + Pressure.GetUnitSymbol(PressureUnits), averageResultKind);
                return evaluationResult;
            }

            if (pdAverage > operationalWindowRange * 2)
            {
                AddIssue(EvaluationResultKind.Error, title + " seriously overinflated.", title + MajorOverInflationMessage);
                minimalEvaluationResult = EvaluationResultKind.None;
                maximalEvaluationResult = EvaluationResultKind.Error;
                averageResultKind = EvaluationResultKind.Error;
            }
            else if (pdAverage < -operationalWindowRange * 2)
            {
                AddIssue(EvaluationResultKind.Error, title + " seriously underinflated.", title + MajorUnderInflationMessage);
                minimalEvaluationResult = EvaluationResultKind.None;
                maximalEvaluationResult = EvaluationResultKind.Error;
                averageResultKind = EvaluationResultKind.Error;
            }
            else if (pdAverage > operationalWindowRange)
            {
                AddIssue(EvaluationResultKind.Warning, title + " overinflated.", title + MinorOverInflationMessage);
                minimalEvaluationResult = EvaluationResultKind.Warning;
                maximalEvaluationResult = EvaluationResultKind.None;
                averageResultKind = EvaluationResultKind.Warning;
            }
            else if (pdAverage < -operationalWindowRange)
            {
                AddIssue(EvaluationResultKind.Warning, title + " underinflated.", title + MinorUnderInflationMessage);
                minimalEvaluationResult = EvaluationResultKind.Warning;
                maximalEvaluationResult = EvaluationResultKind.None;
                averageResultKind = EvaluationResultKind.Warning;
            }

            evaluationResult.AddItem("Ideal Tyre Pressure:", Pressure.FromKiloPascals(idealValue).GetValueInUnits(PressureUnits).ToStringScalableDecimals() + Pressure.GetUnitSymbol(PressureUnits), EvaluationResultKind.None);
            evaluationResult.AddItem("Lower Operational Limit:", Pressure.FromKiloPascals(quantityStatistics.LowLimitAverage).GetValueInUnits(PressureUnits).ToStringScalableDecimals() + Pressure.GetUnitSymbol(PressureUnits), EvaluationResultKind.None);
            evaluationResult.AddItem("Upper Operational Limit:", Pressure.FromKiloPascals(quantityStatistics.HighLimitAverage).GetValueInUnits(PressureUnits).ToStringScalableDecimals() + Pressure.GetUnitSymbol(PressureUnits), EvaluationResultKind.None);

            evaluationResult.AddSeparator();

            evaluationResult.AddItem("Minimum Pressure:", Pressure.FromKiloPascals(quantityStatistics.MinValue).GetValueInUnits(PressureUnits).ToStringScalableDecimals() + Pressure.GetUnitSymbol(PressureUnits),  minimalEvaluationResult);
            evaluationResult.AddItem("Maximum Pressure:", Pressure.FromKiloPascals(quantityStatistics.MaxValue).GetValueInUnits(PressureUnits).ToStringScalableDecimals() + Pressure.GetUnitSymbol(PressureUnits), maximalEvaluationResult);
            evaluationResult.AddItem("Average Pressure:", Pressure.FromKiloPascals(quantityStatistics.Average).GetValueInUnits(PressureUnits).ToStringScalableDecimals() + Pressure.GetUnitSymbol(PressureUnits), averageResultKind);

            evaluationResult.AddSeparator();

            evaluationResult.AddItem("% of Lap with Overinflated Tyres:", maximalPressureExceededPercentage.ToStringScalableDecimals() + "%", maximalEvaluationResult);
            evaluationResult.AddItem("% of Lap with Underinflated Tyres:", minimalPressureExceededPercentage.ToStringScalableDecimals() + "%", minimalEvaluationResult);

            evaluationResult.AddSeparator();

            evaluationResult.AddItem("Pd of Average to Ideal:", Pressure.FromKiloPascals(pdAverage).GetValueInUnits(PressureUnits).ToStringScalableDecimals() + Pressure.GetUnitSymbol(PressureUnits), maximalEvaluationResult);
            evaluationResult.AddItem("Pd of Average to Overinflated limit:", Pressure.FromKiloPascals(pdMax).GetValueInUnits(PressureUnits).ToStringScalableDecimals() + Pressure.GetUnitSymbol(PressureUnits), maximalEvaluationResult);
            evaluationResult.AddItem("Pd of Average to Underinflated limit:", Pressure.FromKiloPascals(pdMin).GetValueInUnits(PressureUnits).ToStringScalableDecimals() + Pressure.GetUnitSymbol(PressureUnits), maximalEvaluationResult);

            return evaluationResult;
        }

        protected override void StartEvaluationInternal(LapTelemetryDto lapTelemetryDto)
        {
            var wheelsInfo = lapTelemetryDto.DataPoints[0].PlayerData.CarInfo.WheelsInfo;
            _frontLeftStatistics = new MovingIdealQuantityStatistics();
            _frontRightStatistics = new MovingIdealQuantityStatistics();
            _rearLeftStatistics = new MovingIdealQuantityStatistics();
            _rearRightStatistics = new MovingIdealQuantityStatistics();

            _hasIdealPressureDefined = !(wheelsInfo.FrontLeft.TyrePressure.IdealQuantity.IsZero || wheelsInfo.RearLeft.TyrePressure.IdealQuantity.IsZero);
        }
    }
}