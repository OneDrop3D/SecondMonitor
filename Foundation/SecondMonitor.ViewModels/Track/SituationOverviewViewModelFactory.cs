namespace SecondMonitor.ViewModels.Track
{
    using System.Windows;
    using Colors;
    using DataModel.Snapshot;
    using DataModel.TrackMap;
    using Factory;
    using MapView;
    using Settings;

    public class SituationOverviewViewModelFactory
    {
        private readonly IClassColorProvider _classColorProvider;
        private readonly ISettingsProvider _settingsProvider;
        private readonly IViewModelFactory _viewModelFactory;

        public SituationOverviewViewModelFactory(IClassColorProvider classColorProvider, ISettingsProvider settingsProvider, IViewModelFactory viewModelFactory)
        {
            _classColorProvider = classColorProvider;
            _settingsProvider = settingsProvider;
            _viewModelFactory = viewModelFactory;
        }

        public ISituationOverviewViewModel Create(SimulatorDataSet simulatorData, TrackMapDto trackMapDto)
        {
            if (!Application.Current.CheckAccess())
            {
                return Application.Current.Dispatcher.Invoke(() => Create(simulatorData, trackMapDto));
            }

            TrackMapSituationOverviewViewModel trackMapSituationOverviewViewModel =
                new TrackMapSituationOverviewViewModel(_viewModelFactory, _classColorProvider, simulatorData.SessionInfo.TrackInfo.LayoutLength.InMeters, _settingsProvider.DisplaySettingsViewModel.AnimateDriversPosition, _settingsProvider.DisplaySettingsViewModel.DriversUpdatedPerTick)
                {
                    AutoScaleDrivers = _settingsProvider.DisplaySettingsViewModel.MapDisplaySettingsViewModel.AutoScaleDrivers,
                    DriverScale = _settingsProvider.DisplaySettingsViewModel.MapDisplaySettingsViewModel.DriverScalePercentage / 100.0,
                };
            trackMapSituationOverviewViewModel.ApplyTrackGeometry(trackMapDto.TrackGeometry);
            return trackMapSituationOverviewViewModel;
        }

        public ISituationOverviewViewModel CreateDefault(SimulatorDataSet simulatorData)
        {
            return CreateDefaultInternal(simulatorData);
        }

        private DefaultSituationOverviewViewModel CreateDefaultInternal(SimulatorDataSet simulatorData)
        {
            if (!Application.Current.CheckAccess())
            {
                return Application.Current.Dispatcher.Invoke(() => CreateDefaultInternal(simulatorData));
            }

            return new DefaultSituationOverviewViewModel(_viewModelFactory, _classColorProvider, simulatorData.SessionInfo.TrackInfo.LayoutLength.InMeters, _settingsProvider.DisplaySettingsViewModel.AnimateDriversPosition, _settingsProvider.DisplaySettingsViewModel.DriversUpdatedPerTick);
        }
    }
}