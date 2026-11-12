namespace SecondMonitor.Telemetry.TelemetryApplication.Checks
{
    using System.Collections.ObjectModel;
    using Evaluators;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.Tabs;

    public class TelemetryCheckWindowViewModel : AbstractViewModel
    {
        private readonly IViewModelFactory _viewModelFactory;

        public TelemetryCheckWindowViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
            TabItems = new ObservableCollection<TabItemViewModel>();
        }

        public ObservableCollection<TabItemViewModel> TabItems { get; }

        public void AddEvaluationResult(ITelemetryEvaluator telemetryEvaluator)
        {
            EvaluatorResultViewModel evaluatorResult = _viewModelFactory.Create<EvaluatorResultViewModel>();
            evaluatorResult.EvaluationViewModel = telemetryEvaluator.EvaluationViewModel;
            evaluatorResult.SetIssues(telemetryEvaluator.Issues);

            EvaluationTitleViewModel evaluationTitleViewModel = _viewModelFactory.Create<EvaluationTitleViewModel>();
            evaluationTitleViewModel.Title = telemetryEvaluator.EvaluationViewModel.EvaluatorName;
            evaluationTitleViewModel.EvaluationResultKind = telemetryEvaluator.EvaluationViewModel.EvaluationResultKind;

            TabItemViewModel tabItemViewModel = _viewModelFactory.Create<TabItemViewModel>();
            tabItemViewModel.Title = evaluationTitleViewModel;
            tabItemViewModel.TabContent = evaluatorResult;

            TabItems.Add(tabItemViewModel);
        }
    }
}