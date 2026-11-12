namespace SecondMonitor.ViewModels.TrackInfo
{
    using System.Collections.Generic;
    using System.Linq;

    using DataModel.BasicProperties;
    using DataModel.Snapshot;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.DataModel.Snapshot.Drivers;
    using SecondMonitor.ViewModels.Settings;
    using SecondMonitor.ViewModels.Settings.ViewModel;

    public class TrackInfoViewModel : AbstractViewModel, ISimulatorDataSetViewModel
    {
        public const string ViewModelLayoutName = "Track Name and Weather";

        private readonly IList<ITemperatureChangeTracker> _airTemperatureTrackers;
        private readonly IList<ITemperatureChangeTracker> _trackTemperatureTrackers;
        private readonly DisplaySettingsViewModel _displaySettingsViewModel;

        private string _trackName;
        private string _sessionType;
        private string _rainIntensity;
        private string _trackWetness;
        private string _airTemperatureInfo;
        private string _trackTemperatureInfo;
        private bool _weatherInfoAvailable;
        private Distance _layoutLength;

        private TemperatureUnits _temperatureUnits;
        private DistanceUnits _distanceUnits;

        private Temperature _lastAirTemperature;
        private Temperature _lastTrackTemperature;
        private OptimalQuantity<Temperature> _airSecondTemperatureChange;
        private OptimalQuantity<Temperature> _airFirstTemperatureChange;
        private OptimalQuantity<Temperature> _trackSecondTemperatureChange;
        private OptimalQuantity<Temperature> _trackFirstTemperatureChange;
        private string _runningCarsInfo;

        public TrackInfoViewModel(IList<ITemperatureChangeTracker> airTemperatureTrackers, IList<ITemperatureChangeTracker> trackTemperatureTrackers, ISettingsProvider settingsProvider)
        {
            _displaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
            TemperatureUnits = _displaySettingsViewModel.TemperatureUnits;
            DistanceUnits = _displaySettingsViewModel.DistanceUnits;
            _airTemperatureTrackers = airTemperatureTrackers;
            _trackTemperatureTrackers = trackTemperatureTrackers;
            _runningCarsInfo = string.Empty;

            AirSecondTemperatureChange = new OptimalQuantity<Temperature>()
            {
                ActualQuantity = Temperature.FromCelsius(0),
                IdealQuantity = Temperature.FromCelsius(0),
                IdealQuantityWindow = Temperature.FromCelsius(1)
            };

            AirFirstTemperatureChange = new OptimalQuantity<Temperature>()
            {
                ActualQuantity = Temperature.FromCelsius(0),
                IdealQuantity = Temperature.FromCelsius(0),
                IdealQuantityWindow = Temperature.FromCelsius(0.75)
            };

            TrackSecondTemperatureChange = new OptimalQuantity<Temperature>()
            {
                ActualQuantity = Temperature.FromCelsius(0),
                IdealQuantity = Temperature.FromCelsius(0),
                IdealQuantityWindow = Temperature.FromCelsius(1)
            };

            TrackFirstTemperatureChange = new OptimalQuantity<Temperature>()
            {
                ActualQuantity = Temperature.FromCelsius(0),
                IdealQuantity = Temperature.FromCelsius(0),
                IdealQuantityWindow = Temperature.FromCelsius(0.75)
            };

            Reset();
        }

        public string TrackName
        {
            get => _trackName;
            private set => SetProperty(ref _trackName, value);
        }

        public Distance LayoutLength
        {
            get => _layoutLength;
            private set => SetProperty(ref _layoutLength, value);
        }

        public DistanceUnits DistanceUnits
        {
            get => _distanceUnits;
            private set => SetProperty(ref _distanceUnits, value);
        }

        public string SessionType
        {
            get => _sessionType;
            private set => SetProperty(ref _sessionType, value);
        }

        public string RainIntensity
        {
            get => _rainIntensity;
            private set => SetProperty(ref _rainIntensity, value);
        }

        public string AirTemperatureInfo
        {
            get => _airTemperatureInfo;
            private set => SetProperty(ref _airTemperatureInfo, value);
        }

        public string TrackTemperatureInfo
        {
            get => _trackTemperatureInfo;
            private set => SetProperty(ref _trackTemperatureInfo, value);
        }

        public TemperatureUnits TemperatureUnits
        {
            get => _temperatureUnits;
            private set => SetProperty(ref _temperatureUnits, value);
        }

        public bool WeatherInfoAvailable
        {
            get => _weatherInfoAvailable;
            private set => SetProperty(ref _weatherInfoAvailable, value);
        }

        public OptimalQuantity<Temperature> AirSecondTemperatureChange
        {
            get => _airSecondTemperatureChange;
            set => SetProperty(ref _airSecondTemperatureChange, value);
        }

        public OptimalQuantity<Temperature> AirFirstTemperatureChange
        {
            get => _airFirstTemperatureChange;
            set => SetProperty(ref _airFirstTemperatureChange, value);
        }

        public OptimalQuantity<Temperature> TrackSecondTemperatureChange
        {
            get => _trackSecondTemperatureChange;
            set => SetProperty(ref _trackSecondTemperatureChange, value);
        }

        public OptimalQuantity<Temperature> TrackFirstTemperatureChange
        {
            get => _trackFirstTemperatureChange;
            set => SetProperty(ref _trackFirstTemperatureChange, value);
        }

        public string TrackWetness
        {
            get => _trackWetness;
            set => SetProperty(ref _trackWetness, value);
        }

        public string RunningCarsInfo
        {
            get => _runningCarsInfo;
            set => SetProperty(ref _runningCarsInfo, value);
        }

        public void ApplyDateSet(SimulatorDataSet dataSet)
        {
            if (dataSet?.SessionInfo?.TrackInfo == null || dataSet.SessionInfo?.WeatherInfo == null)
            {
                return;
            }

            int runningCars = dataSet.SessionInfo.SessionType == DataModel.BasicProperties.SessionType.Race ?
                dataSet.DriversInfo.Count(x => x.FinishStatus != DriverFinishStatus.Dnf && x.FinishStatus != DriverFinishStatus.Dq && x.FinishStatus != DriverFinishStatus.Dnq) :
                dataSet.DriversInfo.Count(x => !x.InPits);

            RunningCarsInfo = $"{runningCars} / {dataSet.DriversInfo.Length}";

            TemperatureUnits = _displaySettingsViewModel.TemperatureUnits;
            DistanceUnits = _displaySettingsViewModel.DistanceUnits;

            _lastTrackTemperature = dataSet.SessionInfo.WeatherInfo.TrackTemperature;
            _lastAirTemperature = dataSet.SessionInfo.WeatherInfo.AirTemperature;

            SessionType = dataSet.SessionInfo.SessionType.ToString();
            FormatTrackName(dataSet.SessionInfo.TrackInfo.TrackName, dataSet.SessionInfo.TrackInfo.TrackLayoutName);
            RainIntensity = dataSet.SessionInfo.WeatherInfo.RainIntensity + "%";
            TrackWetness = dataSet.SessionInfo.WeatherInfo.TrackWetness > 0 ? dataSet.SessionInfo.WeatherInfo.TrackWetness + "%" : string.Empty;
            LayoutLength = dataSet.SessionInfo.TrackInfo.LayoutLength;
            _airTemperatureTrackers.ForEach(x => x.Update(dataSet.SessionInfo.WeatherInfo.AirTemperature, dataSet));
            _trackTemperatureTrackers.ForEach(x => x.Update(dataSet.SessionInfo.WeatherInfo.TrackTemperature, dataSet));

            RefreshTemperatures(dataSet.SessionInfo.SessionType);
        }

        private void FormatTrackName(string trackName, string trackLayout)
        {
            if (string.IsNullOrEmpty(trackLayout))
            {
                TrackName = trackName;
                return;
            }

            TrackName = trackName + " (" + trackLayout + ")";
        }

        private void RefreshTemperatures(SessionType sessionType)
        {
            if (_lastAirTemperature == null || _lastTrackTemperature == null)
            {
                return;
            }

            WeatherInfoAvailable =
                _lastAirTemperature != Temperature.Zero || _lastTrackTemperature != Temperature.Zero;

            AirTemperatureInfo = _lastAirTemperature.GetFormattedWithUnits(1, TemperatureUnits);
            TrackTemperatureInfo = _lastTrackTemperature.GetFormattedWithUnits(1, TemperatureUnits);

            WeatherInfoSettingsViewModel weatherSettings = _displaySettingsViewModel.WeatherInfoSettings;
            if (sessionType == DataModel.BasicProperties.SessionType.Race)
            {
                AirFirstTemperatureChange = _airTemperatureTrackers.Single(x => x.ChangeKind == weatherSettings.RaceFirstAirTempInfo.Value).TemperatureChange;
                AirSecondTemperatureChange = _airTemperatureTrackers.Single(x => x.ChangeKind == weatherSettings.RaceSecondAirTempInfo.Value).TemperatureChange;

                TrackFirstTemperatureChange = _trackTemperatureTrackers.Single(x => x.ChangeKind == weatherSettings.RaceFirstTrackTempInfo.Value).TemperatureChange;
                TrackSecondTemperatureChange = _trackTemperatureTrackers.Single(x => x.ChangeKind == weatherSettings.RaceSecondTrackTempInfo.Value).TemperatureChange;
            }
            else
            {
                AirFirstTemperatureChange = _airTemperatureTrackers.Single(x => x.ChangeKind == weatherSettings.PracticeFirstAirTempInfo.Value).TemperatureChange;
                AirSecondTemperatureChange = _airTemperatureTrackers.Single(x => x.ChangeKind == weatherSettings.PracticeSecondAirTempInfo.Value).TemperatureChange;

                TrackFirstTemperatureChange = _trackTemperatureTrackers.Single(x => x.ChangeKind == weatherSettings.PracticeFirstTrackTempInfo.Value).TemperatureChange;
                TrackSecondTemperatureChange = _trackTemperatureTrackers.Single(x => x.ChangeKind == weatherSettings.PracticeSecondAirTempInfo.Value).TemperatureChange;
            }
        }

        public void Reset()
        {
            _trackName = "No Track";
            _sessionType = "No Session";
            _rainIntensity = "0%";
            _trackWetness = string.Empty;
            _airTemperatureInfo = string.Empty;
            _trackTemperatureInfo = string.Empty;
            _weatherInfoAvailable = false;
            _airTemperatureTrackers.ForEach(x => x.Reset());
            _trackTemperatureTrackers.ForEach(x => x.Reset());
            _runningCarsInfo = string.Empty;
            NotifyPropertyChanged(string.Empty);
        }
    }
}