namespace SecondMonitor.Telemetry.TelemetryApplication.Checks.Evaluators.Result
{
    using System.Collections.Generic;

    public class EvaluationResultGroup
    {
        private readonly List<EvaluationResultItem> _items;

        public EvaluationResultGroup(string title)
        {
            Title = title;
            _items = new List<EvaluationResultItem>();
        }

        public string Title { get; }

        public IReadOnlyCollection<EvaluationResultItem> Items => _items.AsReadOnly();

        public void AddItem(string label, string value, EvaluationResultKind resultKind)
        {
            _items.Add(new EvaluationResultItem(label, value, resultKind));
        }

        public void AddSeparator()
        {
            _items.Add(new EvaluationSeparatorItem());
        }
    }
}