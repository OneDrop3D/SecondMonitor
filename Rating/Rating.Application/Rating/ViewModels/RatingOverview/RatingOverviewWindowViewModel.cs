namespace SecondMonitor.Rating.Application.Rating.ViewModels.RatingOverview
{
    using System.Collections.ObjectModel;
    using System.Linq;
    using Common.DataModel;
    using Contracts.Commands;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.Tabs;
    using SecondMonitor.ViewModels.Text;

    public class RatingOverviewWindowViewModel : AbstractViewModel<Ratings>, IRatingOverviewWindowViewModel
    {
        private readonly IViewModelFactory _viewModelFactory;

        public RatingOverviewWindowViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
            SimulatorRatings = new ObservableCollection<TabItemViewModel>();
        }

        public ObservableCollection<TabItemViewModel> SimulatorRatings { get; set; }

        public IRelayCommandWithParameter<(ISimulatorRatingsViewModel SimulatorRating, IClassRatingViewModel ClassRating)> OpenClassHistoryCommand { get; set; }

        protected override void ApplyModel(Ratings model)
        {
            SimulatorRatings.Clear();
            foreach (SimulatorRating modelSimulatorsRating in model.SimulatorsRatings.OrderBy(x => x.SimulatorName))
            {
                ISimulatorRatingsViewModel newHistoryViewModel = _viewModelFactory.CreateAndApply<ISimulatorRatingsViewModel, SimulatorRating>(modelSimulatorsRating);
                newHistoryViewModel.OpenClassHistoryCommand = new RelayCommandWithParameter<IClassRatingViewModel>(x => OpenClassHistory(newHistoryViewModel, x));

                TabItemViewModel newTabItemViewModel = _viewModelFactory.Create<TabItemViewModel>();
                newTabItemViewModel.TabContent = newHistoryViewModel;
                newTabItemViewModel.Title = _viewModelFactory.CreateAndApply<TitleViewModel, string>(newHistoryViewModel.SimulatorName);

                SimulatorRatings.Add(newTabItemViewModel);
            }
        }

        private void OpenClassHistory(ISimulatorRatingsViewModel simulatorRatingsViewModel, IClassRatingViewModel classRatingViewModel)
        {
            OpenClassHistoryCommand.Execute((simulatorRatingsViewModel, classRatingViewModel));
        }

        public override Ratings SaveToNewModel()
        {
            throw new System.NotSupportedException();
        }
    }
}