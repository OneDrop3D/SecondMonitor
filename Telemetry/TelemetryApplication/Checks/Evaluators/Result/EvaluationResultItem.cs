namespace SecondMonitor.Telemetry.TelemetryApplication.Checks.Evaluators.Result
{
    public class EvaluationResultItem
    {
        public EvaluationResultItem(string label, string value, EvaluationResultKind itemResultKind)
        {
            Label = label;
            Value = value;
            ItemResultKind = itemResultKind;
        }

        public string Label { get; }

        public string Value { get; }

        public EvaluationResultKind ItemResultKind { get; }
    }
}