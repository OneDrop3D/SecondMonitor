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

    public class ClassRecordsViewModel : AbstractViewModel<IEnumerable<NamedRecordSet>>
    {
        private readonly IViewModelFactory _viewModelFactory;
        private readonly VelocityUnits _velocityUnits;
        private ICommand _removeClassCommand;

        public ClassRecordsViewModel(IViewModelFactory viewModelFactory, ISettingsProvider settingsProvider)
        {
            _viewModelFactory = viewModelFactory;
            _velocityUnits = settingsProvider.DisplaySettingsViewModel.VelocityUnits;
            CarRecordViewModels = new List<CarRecordSummaryViewModel>();
        }

        public TimeSpan OverallBest { get; private set; }

        public string OverallBestSpeed { get; private set; }

        public string ClassName { get; private set; }

        public string TrackName { get; set; }

        public ICommand RemoveClassCommand
        {
            get => _removeClassCommand;
            set => SetProperty(ref _removeClassCommand, value);
        }

        public List<CarRecordSummaryViewModel> CarRecordViewModels { get; }

        protected override void ApplyModel(IEnumerable<NamedRecordSet> model)
        {
            bool overallInitialized = false;
            foreach (NamedRecordSet vehicleSet in model.OrderBy(x => x.GetOverAllBest().LapTimeSeconds))
            {
                if (!overallInitialized)
                {
                    RecordEntryDto overallBest = vehicleSet.GetOverAllBest();
                    OverallBestSpeed = overallBest.AverageSpeedInKph > 0 ? $"{Velocity.FromKph(overallBest.AverageSpeedInKph).GetValueInUnits(_velocityUnits):F2}{Velocity.GetUnitSymbol(_velocityUnits)}" : "-";
                    OverallBest = overallBest.LapTime;
                    ClassName = overallBest.CarClass;
                    overallInitialized = true;
                }

                CarRecordSummaryViewModel carRecordSummaryViewModel = _viewModelFactory.Create<CarRecordSummaryViewModel>();
                carRecordSummaryViewModel.FromModel(vehicleSet);
                CarRecordViewModels.Add(carRecordSummaryViewModel);
            }
        }

        public override IEnumerable<NamedRecordSet> SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}