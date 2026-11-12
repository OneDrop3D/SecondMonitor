namespace SecondMonitor.Rating.Application.Championship.ViewModels.Creation.Calendar
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Data;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels;

    public class AvailableTracksViewModel : AbstractViewModel<ICollection<AbstractTrackTemplateViewModel>>
    {
        private readonly ObservableCollection<AbstractTrackTemplateViewModel> _trackTemplateViewModels;
        private AbstractTrackTemplateViewModel _selectedTrackTemplateViewModel;
        private string _filter;

        public AvailableTracksViewModel()
        {
            _trackTemplateViewModels = new ObservableCollection<AbstractTrackTemplateViewModel>();
            TrackTemplateViewModelsFiltered = CollectionViewSource.GetDefaultView(_trackTemplateViewModels);
            TrackTemplateViewModelsFiltered.Filter = p => FilterTrackTemplateViewModel((AbstractTrackTemplateViewModel)p);
            Filter = string.Empty;
        }

        public string Filter
        {
            get => _filter;
            set => SetProperty(ref _filter, value, (_, __) => TrackTemplateViewModelsFiltered.Refresh());
        }

        public ICollectionView TrackTemplateViewModelsFiltered
        {
            get;
        }

        public AbstractTrackTemplateViewModel SelectedTrackTemplateViewModel
        {
            get => _selectedTrackTemplateViewModel;
            set => SetProperty(ref _selectedTrackTemplateViewModel, value);
        }

        public ExistingTrackTemplateViewModel GetTrackByName(string trackName)
        {
            return _trackTemplateViewModels.OfType<ExistingTrackTemplateViewModel>().SingleOrDefault(x => x.TrackName == trackName);
        }

        protected override void ApplyModel(ICollection<AbstractTrackTemplateViewModel> model)
        {
            _trackTemplateViewModels.Clear();
            model.ForEach(_trackTemplateViewModels.Add);
        }

        public override ICollection<AbstractTrackTemplateViewModel> SaveToNewModel()
        {
            return OriginalModel;
        }

        private bool FilterTrackTemplateViewModel(AbstractTrackTemplateViewModel abstractTrackTemplateViewModel)
        {
            return string.IsNullOrWhiteSpace(Filter)
                   || abstractTrackTemplateViewModel is not ExistingTrackTemplateViewModel
                   || abstractTrackTemplateViewModel.TrackName.Contains(Filter, StringComparison.CurrentCultureIgnoreCase);
        }

        public List<ExistingTrackTemplateViewModel> GetAllExistingTracks()
        {
            return _trackTemplateViewModels.OfType<ExistingTrackTemplateViewModel>().ToList();
        }
    }
}