namespace SecondMonitor.ViewModels.Track
{
    using Factory;

    public class TrackThumbnailViewModel : AbstractViewModel
    {
        public TrackThumbnailViewModel(IViewModelFactory viewModelFactory)
        {
            TrackGeometryViewModel = viewModelFactory.Create<TrackGeometryViewModel>();
        }

        public string TrackName { get; set; }

        public TrackGeometryViewModel TrackGeometryViewModel { get; }
    }
}