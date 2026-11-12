namespace SecondMonitor.Telemetry.TelemetryApplication.Checks.Evaluators.Result
{
    public abstract class AbstractWheelEvaluationResultViewModel : AbstractEvaluatorViewModel
    {
        protected AbstractWheelEvaluationResultViewModel(string evaluatorName) : base(evaluatorName)
        {
        }

        public EvaluationResult FrontLeftResult { get; set; }
        public EvaluationResult FrontRightResult { get; set; }
        public EvaluationResult RearLeftResult { get; set; }
        public EvaluationResult RearRightResult { get; set; }
    }
}