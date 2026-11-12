namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.SnapshotSection
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using DataModel.BasicProperties;
    using Replay;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.CarStatus;
    using SecondMonitor.ViewModels.Factory;
    using TelemetryManagement.DTO;

    public class SnapshotSectionViewModel : AbstractViewModel, ISnapshotSectionViewModel
    {
        private readonly List<LapSummaryDto> _availableLaps;

        private LapSummaryDto _selectedLap;
        private TemperatureUnits _temperatureUnits;
        private PressureUnits _pressureUnits;

        public SnapshotSectionViewModel(IViewModelFactory viewModelFactory, WheelStatusViewModelFactory wheelStatusViewModelFactory)
        {
            _availableLaps = new List<LapSummaryDto>();
            ReplayViewModel = viewModelFactory.Create<IReplayViewModel>();
            PedalSectionViewModel = viewModelFactory.Create<IPedalSectionViewModel>();
            CarWheelsViewModel = new CarWheelsViewModel(wheelStatusViewModelFactory);
        }

        public ReadOnlyCollection<LapSummaryDto> AvailableLaps => _availableLaps.AsReadOnly();

        public LapSummaryDto SelectedLap
        {
            get => _selectedLap;
            set
            {
                _selectedLap = value;
                NotifyPropertyChanged();
            }
        }

        public CarWheelsViewModel CarWheelsViewModel { get; }

        public TemperatureUnits TemperatureUnits
        {
            get => _temperatureUnits;
            set
            {
                _temperatureUnits = value;
                NotifyPropertyChanged();
            }
        }

        public PressureUnits PressureUnits
        {
            get => _pressureUnits;
            set
            {
                _pressureUnits = value;
                NotifyPropertyChanged();
            }
        }

        public IReplayViewModel ReplayViewModel { get; }

        public IPedalSectionViewModel PedalSectionViewModel { get; }

        public void AddAvailableLaps(IEnumerable<LapSummaryDto> lapSummaryDtos)
        {
            _availableLaps.AddRange(lapSummaryDtos);
            if (_availableLaps.Count > 0 && _selectedLap == null)
            {
                SelectedLap = _availableLaps.First();
            }

            _availableLaps.Sort((x, y) => x.LapNumber.CompareTo(y.LapNumber));
            NotifyPropertyChanged(nameof(AvailableLaps));
        }

        public void RemoveAvailableLaps(IEnumerable<LapSummaryDto> lapSummaryDtos)
        {
            _availableLaps.RemoveAll(lapSummaryDtos.Contains);
            if (!_availableLaps.Contains(_selectedLap))
            {
                SelectedLap = _availableLaps.Any() ? _availableLaps.First() : null;
            }
        }

        public void ClearAvailableLaps()
        {
            _availableLaps.Clear();
            SelectedLap = null;
            NotifyPropertyChanged(nameof(AvailableLaps));
        }
    }
}