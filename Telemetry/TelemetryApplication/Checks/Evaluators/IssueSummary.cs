namespace SecondMonitor.Telemetry.TelemetryApplication.Checks.Evaluators
{
    public class IssueSummary
    {
        public IssueSummary(EvaluationResultKind summarySeverity, string header, string summary)
        {
            SummarySeverity = summarySeverity;
            Header = header;
            Summary = summary;
        }

        public EvaluationResultKind SummarySeverity { get; }

        public string Header { get; }

        public string Summary { get; }
    }
}