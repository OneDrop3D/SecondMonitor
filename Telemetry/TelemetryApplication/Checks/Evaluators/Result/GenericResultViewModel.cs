namespace SecondMonitor.Telemetry.TelemetryApplication.Checks.Evaluators.Result
{
    using System;
    using System.Linq;

    public class GenericResultViewModel : AbstractEvaluatorViewModel<EvaluationResult>
    {
        public GenericResultViewModel(string evaluatorName) : base(evaluatorName)
        {
        }

        public EvaluationResult EvaluationResult { get; private set; }

        protected override void ApplyModel(EvaluationResult model)
        {
            EvaluationResult = model;
            EvaluationResultKind = EvaluationResult.Items.Concat(EvaluationResult.Groups.SelectMany(x => x.Items)).Max(x => x.ItemResultKind);
        }

        public override EvaluationResult SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}