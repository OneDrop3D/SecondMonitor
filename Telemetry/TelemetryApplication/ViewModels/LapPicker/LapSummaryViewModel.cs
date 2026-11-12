namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.LapPicker
{
    using System;
    using System.Linq;
    using System.Windows.Input;
    using System.Windows.Media;
    using Controllers.Synchronization;
    using DataModel.BasicProperties;
    using SecondMonitor.ViewModels;

    using TelemetryManagement.DTO;

    public class LapSummaryViewModel : AbstractViewModel<LapSummaryDto>, ILapSummaryViewModel
    {
        private readonly ILapStintSynchronization _lapStintSynchronization;

        private TimeSpan _lapTime;
        private string _customName;
        private bool _display;
        private Color _lapColor;
        private TimeSpan _sector1Time;
        private TimeSpan _sector2Time;
        private TimeSpan _sector3Time;
        private int _stint;
        private ICommand _removeLapCommand;
        private bool _isPlayerLap;
        private ICommand _showTelemetryChecklist;
        private bool _isVisible;
        private string _driverName;
        private bool _isDriverNameVisible;
        private bool _isValid;

        public LapSummaryViewModel(ILapColorSynchronization lapColorSynchronization, ILapStintSynchronization lapStintSynchronization)
        {
            _lapStintSynchronization = lapStintSynchronization;
            LapColorSynchronization = lapColorSynchronization;
            AvailableStintGroups = Enumerable.Range(0, 20).ToArray();
        }

        public ILapColorSynchronization LapColorSynchronization { get; set; }

        public TimeSpan LapTime
        {
            get => _lapTime;
            set
            {
                _lapTime = value;
                NotifyPropertyChanged();
            }
        }

        public TimeSpan Sector1Time
        {
            get => _sector1Time;
            set => SetProperty(ref _sector1Time, value);
        }

        public TimeSpan Sector2Time
        {
            get => _sector2Time;
            set => SetProperty(ref _sector2Time, value);
        }

        public TimeSpan Sector3Time
        {
            get => _sector3Time;
            set => SetProperty(ref _sector3Time, value);
        }

        public bool Selected
        {
            get => _display;
            set => SetProperty(ref _display, value);
        }

        public bool HasLapTime => LapTime > TimeSpan.Zero;

        public string DriverName
        {
            get => _driverName;
            set => SetProperty(ref _driverName, value);
        }

        public bool IsDriverNameVisible
        {
            get => _isDriverNameVisible;
            set => SetProperty(ref _isDriverNameVisible, value);
        }

        public Color LapColor
        {
            get => _lapColor;
            set
            {
                _lapColor = value;
                LapColorSynchronization?.SetColorForLap(OriginalModel.Id, ColorDto.FromColor(value));
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(LapColorBrush));
            }
        }

        public SolidColorBrush LapColorBrush => new SolidColorBrush(LapColor);

        public bool IsPlayerLap
        {
            get => _isPlayerLap;
            set => SetProperty(ref _isPlayerLap, value);
        }

        public int Stint
        {
            get => _stint;
            set
            {
                SetProperty(ref _stint, value);
                _lapStintSynchronization.SetStintNumberForLap(OriginalModel.Id, value);
            }
        }

        public ICommand RemoveLapCommand
        {
            get => _removeLapCommand;
            set => SetProperty(ref _removeLapCommand, value);
        }

        public ICommand ShowTelemetryChecklist
        {
            get => _showTelemetryChecklist;
            set => SetProperty(ref _showTelemetryChecklist, value);
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public int[] AvailableStintGroups { get; }

        public int LapNumber { get; set; }

        public string CustomName
        {
            get => _customName;
            set => SetProperty(ref _customName, value);
        }

        public bool IsValid
        {
            get => _isValid;
            set => SetProperty(ref _isValid, value);
        }

        public void SetLapSelectedSilent(bool isSelected)
        {
            Selected = isSelected;
        }

        protected override void ApplyModel(LapSummaryDto model)
        {
            LapTime = model.LapTime;
            CustomName = model.CustomDisplayName;
            Sector1Time = model.Sector1Time;
            Sector2Time = model.Sector2Time;
            Sector3Time = model.Sector3Time;
            Stint = model.Stint;
            IsPlayerLap = model.IsPlayer;
            DriverName = model.DriverName ?? string.Empty;
            LapNumber = model.LapNumber;
            IsValid = model.IsValid;
        }

        public override LapSummaryDto SaveToNewModel()
        {
           throw new NotSupportedException();
        }
    }
}