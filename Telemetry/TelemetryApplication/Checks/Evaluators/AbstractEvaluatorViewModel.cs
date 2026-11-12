namespace SecondMonitor.Telemetry.TelemetryApplication.Checks.Evaluators
{
    using SecondMonitor.ViewModels;

    public abstract class AbstractEvaluatorViewModel : AbstractViewModel, IEvaluationViewModel
    {
        private EvaluationResultKind _evaluationResultKind;

        protected AbstractEvaluatorViewModel(string evaluatorName)
        {
            EvaluatorName = evaluatorName;
        }

        public string EvaluatorName { get; }

        public EvaluationResultKind EvaluationResultKind
        {
            get => _evaluationResultKind;
            set => SetProperty(ref _evaluationResultKind, value);
        }
    }
}