namespace SecondMonitor.Rating.Application.Rating.ViewModels.RatingOverview
{
    using System.Collections.ObjectModel;
    using Common.DataModel;
    using Contracts.Commands;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Tabs;

    public interface IRatingOverviewWindowViewModel : IViewModel<Ratings>
    {
        ObservableCollection<TabItemViewModel> SimulatorRatings { get; set; }

        IRelayCommandWithParameter<(ISimulatorRatingsViewModel SimulatorRating, IClassRatingViewModel ClassRating)> OpenClassHistoryCommand { get; set; }
    }
}