namespace SecondMonitor.ViewModels.TrackRecords
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;
    using DataModel.TrackRecords;
    using Factory;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.ViewModels.Settings;

    public class TracksAllRecordsViewModel : AbstractViewModel<TrackRecord>
    {
        private readonly IViewModelFactory _viewModelFactory;
        private readonly VelocityUnits _velocityUnits;
        private List<ClassRecordsViewModel> _classRecords;
        private TimeSpan _overallBest;
        private ICommand _deleteAllRecordsForTrack;
        private string _trackName;
        private string _overallBestSpeed;

        public TracksAllRecordsViewModel(IViewModelFactory viewModelFactory, ISettingsProvider settingsProvider)
        {
            _viewModelFactory = viewModelFactory;
            _velocityUnits = settingsProvider.DisplaySettingsViewModel.VelocityUnits;
            _classRecords = new List<ClassRecordsViewModel>();
        }

        public ICommand DeleteAllRecordsForTrack
        {
            get => _deleteAllRecordsForTrack;
            set => SetProperty(ref _deleteAllRecordsForTrack, value);
        }

        public string TrackName
        {
            get => _trackName;
            private set => SetProperty(ref _trackName, value);
        }

        public TimeSpan OverallBest
        {
            get => _overallBest;
            private set => SetProperty(ref _overallBest, value);
        }

        public string OverallBestSpeed
        {
            get => _overallBestSpeed;
            private set => SetProperty(ref _overallBestSpeed, value);
        }

        public List<ClassRecordsViewModel> ClassRecords
        {
            get => _classRecords;
            set => SetProperty(ref _classRecords, value);
        }

        protected override void ApplyModel(TrackRecord model)
        {
            if (model == null)
            {
                ClassRecords = new List<ClassRecordsViewModel>();
                TrackName = string.Empty;
                OverallBest = TimeSpan.Zero;
                return;
            }

            TrackName = model.TrackName;
            RecordEntryDto overallBest = model.OverallRecord.GetOverAllBest();
            OverallBest = overallBest?.LapTime ?? TimeSpan.Zero;
            OverallBestSpeed = overallBest?.AverageSpeedInKph > 0
                ? $"{Velocity.FromKph(overallBest.AverageSpeedInKph).GetValueInUnits(_velocityUnits):F2}{Velocity.GetUnitSymbol(_velocityUnits)}" : "-";

            var newClassRecords = new List<ClassRecordsViewModel>();
            var recordsGroupedByCar = model.VehicleRecords.Where(x => x.GetOverAllBest() != null).GroupBy(x => x.GetOverAllBest().CarClass);
            foreach (IGrouping<string, NamedRecordSet> recordForClass in recordsGroupedByCar)
            {
                ClassRecordsViewModel classRecordsViewModel = _viewModelFactory.Create<ClassRecordsViewModel>();
                classRecordsViewModel.TrackName = TrackName;
                classRecordsViewModel.FromModel(recordForClass);
                newClassRecords.Add(classRecordsViewModel);
            }

            ClassRecords = newClassRecords.OrderBy(x => x.OverallBest).ToList();
        }

        public override TrackRecord SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}