namespace SecondMonitor.Timing.Application.SessionTiming
{
    using Common.SessionTiming;

    using DataModel.Snapshot;
    using Ninject;
    using Ninject.Parameters;
    using Ninject.Syntax;
    using NLog;
    using Telemetry;
    using ViewModel;

    public class SessionTimingFactory
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly ISessionTelemetryControllerFactory _sessionTelemetryControllerFactory;
        private readonly SessionInfoProvider _sessionInfoProvider;
        private readonly IResolutionRoot _resolutionRoot;

        public SessionTimingFactory(IResolutionRoot resolutionRoot, ISessionTelemetryControllerFactory sessionTelemetryControllerFactory, SessionInfoProvider sessionInfoProvider)
        {
            _resolutionRoot = resolutionRoot;
            _sessionTelemetryControllerFactory = sessionTelemetryControllerFactory;
            _sessionInfoProvider = sessionInfoProvider;
        }

        public SessionTimingController Create(SimulatorDataSet dataSet, bool invalidateFirstLap)
        {
            Logger.Info($"New Session Started :{dataSet.SessionInfo.SessionType.ToString()}");

            ISessionTelemetryController sessionTelemetryController = _sessionTelemetryControllerFactory.Create(dataSet);
            TypeMatchingConstructorArgument sessionTelemetryControllerArgument = new TypeMatchingConstructorArgument(typeof(ISessionTelemetryController), (_, __) => sessionTelemetryController);
            TypeMatchingConstructorArgument dataSedArgument = new TypeMatchingConstructorArgument(typeof(SimulatorDataSet), (_, __) => dataSet);

            SessionTimingController timing = _resolutionRoot.Get<SessionTimingController>(dataSedArgument, sessionTelemetryControllerArgument);
            timing.InitializeDriversFrom(dataSet, invalidateFirstLap);

            _sessionInfoProvider.SetSessionInfo(timing);

            return timing;
        }
    }
}