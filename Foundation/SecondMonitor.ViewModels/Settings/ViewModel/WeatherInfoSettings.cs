namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using SecondMonitor.Contracts;
    using SecondMonitor.ViewModels.Settings.Model;

    [Serializable]
    public class WeatherInfoSettingsViewModel : AbstractViewModel<WeatherInfoSettings>
    {
        private HumanReadableItem<TemperatureChangeKind> _practiceFirstAirTempInfo;
        private HumanReadableItem<TemperatureChangeKind> _practiceSecondAirTempInfo;
        private HumanReadableItem<TemperatureChangeKind> _practiceFirstTrackTempInfo;
        private HumanReadableItem<TemperatureChangeKind> _practiceSecondTrackTempInfo;
        private HumanReadableItem<TemperatureChangeKind> _raceFirstAirTempInfo;
        private HumanReadableItem<TemperatureChangeKind> _raceSecondAirTempInfo;
        private HumanReadableItem<TemperatureChangeKind> _raceFirstTrackTempInfo;
        private HumanReadableItem<TemperatureChangeKind> _raceSecondTrackTempInfo;
        private TemperatureChangeKindMap _temperatureChangeKindMap;

        public WeatherInfoSettingsViewModel()
        {
            _temperatureChangeKindMap = new TemperatureChangeKindMap();
            AvailableTemperatureChangeValues = _temperatureChangeKindMap.GetAllTranslations().Select(x => new HumanReadableItem<TemperatureChangeKind>(x.Key, x.Value)).ToList();
        }

        public List<HumanReadableItem<TemperatureChangeKind>> AvailableTemperatureChangeValues { get; }

        public HumanReadableItem<TemperatureChangeKind> PracticeFirstAirTempInfo
        {
            get => _practiceFirstAirTempInfo;
            set => SetProperty(ref _practiceFirstAirTempInfo, value);
        }

        public HumanReadableItem<TemperatureChangeKind> PracticeSecondAirTempInfo
        {
            get => _practiceSecondAirTempInfo;
            set => SetProperty(ref _practiceSecondAirTempInfo, value);
        }

        public HumanReadableItem<TemperatureChangeKind> PracticeFirstTrackTempInfo
        {
            get => _practiceFirstTrackTempInfo;
            set => SetProperty(ref _practiceFirstTrackTempInfo, value);
        }

        public HumanReadableItem<TemperatureChangeKind> PracticeSecondTrackTempInfo
        {
            get => _practiceSecondTrackTempInfo;
            set => SetProperty(ref _practiceSecondTrackTempInfo, value);
        }

        public HumanReadableItem<TemperatureChangeKind> RaceFirstAirTempInfo
        {
            get => _raceFirstAirTempInfo;
            set => SetProperty(ref _raceFirstAirTempInfo, value);
        }

        public HumanReadableItem<TemperatureChangeKind> RaceSecondAirTempInfo
        {
            get => _raceSecondAirTempInfo;
            set => SetProperty(ref _raceSecondAirTempInfo, value);
        }

        public HumanReadableItem<TemperatureChangeKind> RaceFirstTrackTempInfo
        {
            get => _raceFirstTrackTempInfo;
            set => SetProperty(ref _raceFirstTrackTempInfo, value);
        }

        public HumanReadableItem<TemperatureChangeKind> RaceSecondTrackTempInfo
        {
            get => _raceSecondTrackTempInfo;
            set => SetProperty(ref _raceSecondTrackTempInfo, value);
        }

        protected override void ApplyModel(WeatherInfoSettings model)
        {
            PracticeFirstAirTempInfo = AvailableTemperatureChangeValues.Single(x => x.Value == model.PracticeFirstTrackTempInfo);
            PracticeSecondAirTempInfo = AvailableTemperatureChangeValues.Single(x => x.Value == model.PracticeSecondAirTempInfo);
            PracticeFirstTrackTempInfo = AvailableTemperatureChangeValues.Single(x => x.Value == model.PracticeFirstTrackTempInfo);
            PracticeSecondTrackTempInfo = AvailableTemperatureChangeValues.Single(x => x.Value == model.PracticeSecondTrackTempInfo);

            RaceFirstAirTempInfo = AvailableTemperatureChangeValues.Single(x => x.Value == model.RaceFirstAirTempInfo);
            RaceSecondAirTempInfo = AvailableTemperatureChangeValues.Single(x => x.Value == model.RaceSecondAirTempInfo);
            RaceFirstTrackTempInfo = AvailableTemperatureChangeValues.Single(x => x.Value == model.RaceFirstTrackTempInfo);
            RaceSecondTrackTempInfo = AvailableTemperatureChangeValues.Single(x => x.Value == model.RaceSecondTrackTempInfo);
        }

        public override WeatherInfoSettings SaveToNewModel()
        {
            return new WeatherInfoSettings()
            {
                PracticeFirstAirTempInfo = PracticeFirstAirTempInfo.Value,
                PracticeSecondAirTempInfo = PracticeSecondAirTempInfo.Value,
                PracticeFirstTrackTempInfo = PracticeFirstTrackTempInfo.Value,
                PracticeSecondTrackTempInfo = PracticeSecondTrackTempInfo.Value,

                RaceFirstAirTempInfo = RaceFirstAirTempInfo.Value,
                RaceSecondAirTempInfo = RaceSecondAirTempInfo.Value,
                RaceFirstTrackTempInfo = RaceFirstTrackTempInfo.Value,
                RaceSecondTrackTempInfo = RaceSecondTrackTempInfo.Value,
            };
        }
    }
}
