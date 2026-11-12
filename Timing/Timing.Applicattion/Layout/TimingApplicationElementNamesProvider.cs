namespace SecondMonitor.Timing.Application.Layout
{
    using System.Collections.Generic;
    using System.Linq;
    using Rating.Application.Rating.ViewModels;

    using SecondMonitor.Rating.Application.Championship.ViewModels.Events;

    using SessionTiming.Drivers.ViewModel;
    using TimingGrid;
    using ViewModel;
    using ViewModels.CarStatus;
    using ViewModels.CarStatus.FuelStatus;
    using ViewModels.Layouts.Factory;
    using ViewModels.Track.MapView;
    using ViewModels.TrackInfo;
    using ViewModels.TrackRecords;

    public class TimingApplicationElementNamesProvider : ILayoutElementNamesProvider
    {
        private static readonly List<string> ContentNames = new List<string>
        {
            CarWheelsViewModel.ViewModelLayoutName,
            CarSystemsViewModel.ViewModelLayoutName,
            DashboardViewModel.ViewModelLayoutName,
            PedalsAndGearViewModel.ViewModelLayoutName,
            TrackInfoViewModel.ViewModelLayoutName,
            LapDeltaViewModel.ViewModelLayoutName,
            FuelOverviewViewModel.ViewModelLayoutName,
            TrackRecordsViewModel.ViewModelLayoutName,
            SessionInfoViewModel.ViewModelLayoutName,
            DefaultSituationOverviewViewModel.ViewModelLayoutName,
            SidebarViewModel.ViewModelLayoutName,
            RatingApplicationViewModel.ViewModelLayoutName,
            TimingDataGridViewModel.ViewModelLayoutName,
            NextEventToolbarViewModel.ViewModelLayoutName,
        }.OrderBy(x => x).ToList();

        public List<string> GetAllowableContentNames()
        {
            return ContentNames;
        }
    }
}