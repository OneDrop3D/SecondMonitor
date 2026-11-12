namespace SecondMonitor.Telemetry.TelemetryApplication.Checks.Evaluators
{
    using SecondMonitor.ViewModels;

    public interface IEvaluationViewModel : IViewModel
    {
        string EvaluatorName { get; }
        EvaluationResultKind EvaluationResultKind { get; set; }
    }
}