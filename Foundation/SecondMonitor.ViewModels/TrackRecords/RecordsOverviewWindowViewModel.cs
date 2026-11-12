namespace SecondMonitor.ViewModels.TrackRecords
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;
    using Contracts.TrackMap;
    using DataModel.TrackMap;
    using DataModel.TrackRecords;
    using Factory;
    using Track;

    public class RecordsOverviewWindowViewModel : AbstractViewModel<SimulatorsRecords>
    {
        private readonly IViewModelFactory _viewModelFactory;
        private readonly IMapsLoader _mapsLoader;
        private bool _isPracticeChecked;
        private bool _isQualificationChecked;
        private bool _isRaceChecked;
        private string _selectedSimulator;
        private TrackThumbnailViewModel _selectedTrack;
        private List<TrackThumbnailViewModel> _availableTracks;
        private ICommand _deleteAllRecordsForSimulator;

        public RecordsOverviewWindowViewModel(IViewModelFactory viewModelFactory,  IMapsLoaderFactory mapsLoaderFactory)
        {
            _viewModelFactory = viewModelFactory;
            _mapsLoader = mapsLoaderFactory.Create();
            IsPracticeChecked = true;
            IsQualificationChecked = true;
            IsRaceChecked = true;
            AvailableTracks = new List<TrackThumbnailViewModel>();
            TracksAllRecordsViewModel = _viewModelFactory.Create<TracksAllRecordsViewModel>();
        }

        public TracksAllRecordsViewModel TracksAllRecordsViewModel { get; }

        public string[] Simulators { get; private set; }

        public List<TrackThumbnailViewModel> AvailableTracks
        {
            get => _availableTracks;
            private set => SetProperty(ref _availableTracks, value);
        }

        public TrackThumbnailViewModel SelectedTrack
        {
            get => _selectedTrack;
            set => SetProperty(ref _selectedTrack, value, (_, __) => InitializeSelectedTrackRecords());
        }

        public string SelectedSimulator
        {
            get => _selectedSimulator;
            set => SetProperty(ref _selectedSimulator, value, (_, __) => InitializeTracks());
        }

        public bool IsPracticeChecked
        {
            get => _isPracticeChecked;
            set => SetProperty(ref _isPracticeChecked, value);
        }

        public bool IsQualificationChecked
        {
            get => _isQualificationChecked;
            set => SetProperty(ref _isQualificationChecked, value);
        }

        public bool IsRaceChecked
        {
            get => _isRaceChecked;
            set => SetProperty(ref _isRaceChecked, value);
        }

        public ICommand DeleteAllRecordsForSimulator
        {
            get => _deleteAllRecordsForSimulator;
            set => SetProperty(ref _deleteAllRecordsForSimulator, value);
        }

        protected override void ApplyModel(SimulatorsRecords model)
        {
            Simulators = model.SimulatorRecords.Select(x => x.SimulatorName).OrderBy(x => x).ToArray();
            _selectedSimulator = Simulators.FirstOrDefault();

            InitializeTracks();
        }

        public override SimulatorsRecords SaveToNewModel()
        {
            throw new NotSupportedException();
        }

        private void InitializeSelectedTrackRecords()
        {
            if (_selectedTrack == null)
            {
                return;
            }

            TracksAllRecordsViewModel.FromModel(OriginalModel.GetOrCreateSimulatorRecords(_selectedSimulator).GetOrCreateTrackRecord(_selectedTrack.TrackName));
        }

        private void InitializeTracks()
        {
            TracksAllRecordsViewModel.FromModel(null);
            if (string.IsNullOrEmpty(_selectedSimulator))
            {
                AvailableTracks = new List<TrackThumbnailViewModel>();
                return;
            }

            List<TrackThumbnailViewModel> newAvailableTracks = new List<TrackThumbnailViewModel>();

            foreach (TrackRecord trackRecord in OriginalModel.GetOrCreateSimulatorRecords(_selectedSimulator).TrackRecords.OrderBy(x => x.TrackName))
            {
                TrackThumbnailViewModel newViewModel = _viewModelFactory.Create<TrackThumbnailViewModel>();
                newViewModel.TrackName = trackRecord.TrackName;
                if (_mapsLoader.TryLoadMap(_selectedSimulator, trackRecord.TrackName, out TrackMapDto trackMapDto))
                {
                    newViewModel.TrackGeometryViewModel.FromModel(trackMapDto.TrackGeometry);
                }

                newAvailableTracks.Add(newViewModel);
            }

            AvailableTracks = newAvailableTracks;
        }
    }
}