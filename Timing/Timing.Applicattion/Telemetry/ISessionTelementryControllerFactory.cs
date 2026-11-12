namespace SecondMonitor.Timing.Application.Telemetry
{
    using Common.SessionTiming;
    using DataModel.Snapshot;

    using SecondMonitor.Telemetry.TelemetryApplication.Repository;
    using ViewModels.Settings;

    public class SessionTelemetryControllerFactory : ISessionTelemetryControllerFactory
    {
        private readonly ITelemetryRepositoryFactory _telemetryRepositoryFactory;
        private readonly ISettingsProvider _settingsProvider;
        private readonly ILapEventProvider _lapEventProvider;

        public SessionTelemetryControllerFactory(ITelemetryRepositoryFactory telemetryRepositoryFactory, ISettingsProvider settingsProvider, ILapEventProvider lapEventProvider)
        {
            _telemetryRepositoryFactory = telemetryRepositoryFactory;
            _settingsProvider = settingsProvider;
            _lapEventProvider = lapEventProvider;
        }

        public ISessionTelemetryController Create(SimulatorDataSet simulatorDataSet)
        {
            return new SessionTelemetryController(simulatorDataSet.SessionInfo.TrackInfo.TrackName,
                simulatorDataSet.SessionInfo.SessionType,
                _telemetryRepositoryFactory.Create(_settingsProvider),
                _lapEventProvider,
                _settingsProvider);
        }
    }
}