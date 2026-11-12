namespace SecondMonitor.ViewModels.CarStatus
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;

    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using Settings;
    using Settings.ViewModel;

    public class PedalsAndGearViewModel : AbstractViewModel, ISimulatorDataSetViewModel
    {
        private readonly Stopwatch _refreshStopWatch;
        public const string ViewModelLayoutName = "Pedals And Gear";
        private readonly DisplaySettingsViewModel _displaySettingsViewModel;
        private double _throttlePercentage;
        private string _speed;
        private int _rpm;
        private double _wheelRotation;
        private double _clutchPercentage;
        private double _brakePercentage;
        private double _throttleFilteredPercentage;
        private double _brakeFilteredPercentage;
        private string _gear;
        private TimeSpan _lowestSpeedRecordTime;
        private TimeSpan _highestSpeedRecordTime;
        private Velocity _lowestSpeed;
        private Velocity _highestSpeed;
        private bool _isLowestSpeedEnabled;
        private bool _isHighestSpeedEnabled;

        public PedalsAndGearViewModel(ISettingsProvider settingsProvider)
        {
            _refreshStopWatch = Stopwatch.StartNew();
            _displaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
            _lowestSpeed = Velocity.Zero;
            _highestSpeed = Velocity.Zero;
            IsLowestSpeedEnabled = _displaySettingsViewModel.IsRecentLowestSpeedEnabled;
            IsHighestSpeedEnabled = _displaySettingsViewModel.IsRecentHighestSpeedEnabled;
            _displaySettingsViewModel.PropertyChanged += DisplaySettingsViewModelOnPropertyChanged;
        }

        public double ThrottlePercentage
        {
            get => _throttlePercentage;
            set => SetProperty(ref _throttlePercentage, value);
        }

        public string Speed
        {
            get => _speed;
            set => SetProperty(ref _speed, value);
        }

        public string LowestSpeedFormatted => _lowestSpeed.GetValueInUnits(_displaySettingsViewModel.VelocityUnits).ToString("F0");

        public string HighestSpeedFormatted => _highestSpeed.GetValueInUnits(_displaySettingsViewModel.VelocityUnits).ToString("F0");

        public int Rpm
        {
            get => _rpm;
            set => SetProperty(ref _rpm, value);
        }

        public double WheelRotation
        {
            get => _wheelRotation;
            set => SetProperty(ref _wheelRotation, value);
        }

        public double ClutchPercentage
        {
            get => _clutchPercentage;
            set => SetProperty(ref _clutchPercentage, value);
        }

        public double BrakePercentage
        {
            get => _brakePercentage;
            set => SetProperty(ref _brakePercentage, value);
        }

        public double ThrottleFilteredPercentage
        {
            get => _throttleFilteredPercentage;
            set => SetProperty(ref _throttleFilteredPercentage, value);
        }

        public double BrakeFilteredPercentage
        {
            get => _brakeFilteredPercentage;
            set => SetProperty(ref _brakeFilteredPercentage, value);
        }

        public string Gear
        {
            get => _gear;
            set => SetProperty(ref _gear, value);
        }

        public bool IsLowestSpeedEnabled
        {
            get => _isLowestSpeedEnabled;
            set => SetProperty(ref _isLowestSpeedEnabled, value);
        }

        public bool IsHighestSpeedEnabled
        {
            get => _isHighestSpeedEnabled;
            set => SetProperty(ref _isHighestSpeedEnabled, value);
        }

        public void ApplyDateSet(SimulatorDataSet dataSet)
        {
            if (_refreshStopWatch.ElapsedMilliseconds < 33)
            {
                return;
            }

            if (dataSet?.PlayerInfo?.CarInfo == null || dataSet.InputInfo == null || !_displaySettingsViewModel.EnablePedalInformation)
            {
                return;
            }

            if (dataSet.InputInfo.ThrottlePedalPosition >= 0)
            {
                ThrottlePercentage = Math.Round(dataSet.InputInfo.ThrottlePedalPosition * 100);
                //ThrottleFilteredPercentage = dataSet.InputInfo.ThrottlePedalFilteredPosition * 100;
            }

            if (dataSet.InputInfo.BrakePedalPosition >= 0)
            {
                BrakePercentage = Math.Round(dataSet.InputInfo.BrakePedalPosition * 100);
                //BrakeFilteredPercentage = dataSet.InputInfo.BrakePedalFilteredPosition * 100;
            }

            if (dataSet.InputInfo.ClutchPedalPosition >= 0)
            {
                ClutchPercentage = Math.Round(dataSet.InputInfo.ClutchPedalPosition * 100);
            }

            if (dataSet.PlayerInfo.Speed < _lowestSpeed || (dataSet.SessionInfo.SessionTime - _lowestSpeedRecordTime).TotalSeconds > _displaySettingsViewModel.LowestHighestSpeedTimeout)
            {
                _lowestSpeedRecordTime = dataSet.SessionInfo.SessionTime;
                _lowestSpeed = dataSet.PlayerInfo.Speed;
                NotifyPropertyChanged(nameof(LowestSpeedFormatted));
            }

            if (dataSet.PlayerInfo.Speed > _highestSpeed || (dataSet.SessionInfo.SessionTime - _highestSpeedRecordTime).TotalSeconds > _displaySettingsViewModel.LowestHighestSpeedTimeout)
            {
                _highestSpeedRecordTime = dataSet.SessionInfo.SessionTime;
                _highestSpeed = dataSet.PlayerInfo.Speed;
                NotifyPropertyChanged(nameof(HighestSpeedFormatted));
            }

            Speed = dataSet.PlayerInfo.Speed.GetValueInUnits(_displaySettingsViewModel.VelocityUnits).ToString("F0");
            Rpm = dataSet.PlayerInfo.CarInfo.EngineRpm;

            WheelRotation = Math.Round(dataSet.InputInfo.WheelAngle);
            Gear = dataSet.PlayerInfo.CarInfo.CurrentGear;

            _refreshStopWatch.Restart();
        }

        public void Reset()
        {
            _lowestSpeedRecordTime = TimeSpan.Zero;
            _highestSpeedRecordTime = TimeSpan.Zero;
            _lowestSpeed = Velocity.Zero;
            _highestSpeed = Velocity.Zero;

            NotifyPropertyChanged(nameof(LowestSpeedFormatted));
            NotifyPropertyChanged(nameof(HighestSpeedFormatted));
        }

        private void DisplaySettingsViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(_displaySettingsViewModel.IsRecentLowestSpeedEnabled) && e.PropertyName != nameof(_displaySettingsViewModel.IsRecentHighestSpeedEnabled))
            {
                return;
            }

            IsLowestSpeedEnabled = _displaySettingsViewModel.IsRecentLowestSpeedEnabled;
            IsHighestSpeedEnabled = _displaySettingsViewModel.IsRecentHighestSpeedEnabled;
        }
    }
}