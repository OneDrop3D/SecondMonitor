namespace SecondMonitor.Telemetry.TelemetryApplication.Checks.Evaluators
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels;

    public class EvaluatorResultViewModel : AbstractViewModel
    {
        private IEvaluationViewModel _evaluationViewModel;

        public EvaluatorResultViewModel()
        {
            Issues = new ObservableCollection<IssueSummary>();
        }

        public IEvaluationViewModel EvaluationViewModel
        {
            get => _evaluationViewModel;
            set => SetProperty(ref _evaluationViewModel, value);
        }

        public ObservableCollection<IssueSummary> Issues { get; }

        public void SetIssues(IEnumerable<IssueSummary> issueSummaries)
        {
            Issues.Clear();
            issueSummaries.OrderByDescending(x => x.SummarySeverity).ForEach(Issues.Add);
        }
    }
}