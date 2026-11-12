namespace SecondMonitor.Telemetry.TelemetryApplication.Checks.Evaluators
{
    using System.Collections.Generic;
    using DataModel.Telemetry;
    using TelemetryManagement.DTO;

    public interface ITelemetryEvaluator
    {
        IEvaluationViewModel EvaluationViewModel { get; }

        IReadOnlyCollection<IssueSummary> Issues { get; }

        bool SupportsSimulator(string simulatorName);

        void StartEvaluation(LapTelemetryDto lapTelemetryDto);

        void ProcessDataPoints(TimedTelemetrySnapshot telemetrySnapshot);

        void FinishEvaluation();
    }
}