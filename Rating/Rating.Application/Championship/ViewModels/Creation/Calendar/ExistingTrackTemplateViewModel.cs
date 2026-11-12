namespace SecondMonitor.Rating.Application.Championship.ViewModels.Creation.Calendar
{
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.Track;

    public class ExistingTrackTemplateViewModel : AbstractTrackTemplateViewModel
    {
        private int _usageCount;

        public ExistingTrackTemplateViewModel(IViewModelFactory viewModelFactory)
        {
            TrackGeometryViewModel = viewModelFactory.Create<TrackGeometryViewModel>();
        }

        public double LayoutLengthMeters { get; set; }

        public TrackGeometryViewModel TrackGeometryViewModel { get; }
        public int UsageCount
        {
            get => _usageCount;
            set => SetProperty(ref _usageCount, value);
        }
    }
}