namespace SecondMonitor.Telemetry.TelemetryApplication.Checks.Evaluators.Result
{
    using System.Collections.Generic;
    using System.Linq;

    public class EvaluationResult
    {
        private readonly List<EvaluationResultItem> _items;
        private readonly Dictionary<string, EvaluationResultGroup> _groups;

        public EvaluationResult(string title)
        {
            Title = title;
            _items = new List<EvaluationResultItem>();
            _groups = new Dictionary<string, EvaluationResultGroup>();
        }

        public string Title { get; }

        public IReadOnlyCollection<EvaluationResultItem> Items => _items.AsReadOnly();

        public IReadOnlyCollection<EvaluationResultGroup> Groups => _groups.Values.ToList();

        public void AddItem(string label, string value, EvaluationResultKind resultKind)
        {
            _items.Add(new EvaluationResultItem(label, value, resultKind));
        }

        public void AddSeparator()
        {
            _items.Add(new EvaluationSeparatorItem());
        }

        public void AddSeparator(string groupName)
        {
            if (!_groups.TryGetValue(groupName, out EvaluationResultGroup evaluationResultGroup))
            {
                evaluationResultGroup = new EvaluationResultGroup(groupName);
                _groups.Add(groupName, evaluationResultGroup);
            }

            evaluationResultGroup.AddSeparator();
        }

        public void AddItem(string groupName, string label, string value, EvaluationResultKind resultKind)
        {
            if (!_groups.TryGetValue(groupName, out EvaluationResultGroup evaluationResultGroup))
            {
                evaluationResultGroup = new EvaluationResultGroup(groupName);
                _groups.Add(groupName, evaluationResultGroup);
            }

            evaluationResultGroup.AddItem(label, value, resultKind);
        }
    }
}