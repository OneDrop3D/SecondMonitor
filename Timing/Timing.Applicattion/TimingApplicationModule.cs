namespace SecondMonitor.Timing.Application
{
    using Common.SessionTiming;
    using Common.SessionTiming.Drivers;

    using Contracts.Session;
    using Contracts.TrackMap;
    using Contracts.TrackRecords;
    using Controllers;
    using DataModel.Snapshot;
    using LapTimings;
    using Layout;
    using Ninject;
    using Ninject.Extensions.Factory;
    using Ninject.Extensions.NamedScope;
    using Ninject.Modules;
    using PitBoard.Controller;
    using PitBoard.DataProviders;
    using PitStatistics;

    using SecondMonitor.Contracts.PitStatistics;
    using SecondMonitor.Timing.Application.AutoDelete.Deleters;
    using SecondMonitor.Timing.Application.TrackRecords.Controller.Migrations;
    using SecondMonitor.ViewModels.FuelConsumption.FuelStrategy.PitWindow;
    using SecondMonitor.ViewModels.FuelConsumption.FuelStrategy.Stint;

    using SessionTiming;
    using SessionTiming.Drivers;
    using SessionTiming.Drivers.Presentation.ViewModel;

    using Telemetry;
    using TimingGrid.Columns;
    using TrackRecords.Controller;
    using ViewModel;
    using ViewModels.CarStatus;
    using ViewModels.DataGrid;
    using ViewModels.Layouts.Factory;
    using ViewModels.PitBoard;
    using ViewModels.PitBoard.Controller;
    using ViewModels.Track;
    using ViewModels.Track.MapView.Controller;

    public class TimingApplicationModule : NinjectModule
    {
        public override void Load()
        {
            Bind<ITrackRecordsRepositoryFactory>().To<TrackRecordsRepositoryFactory>();
            Bind<ITrackRecordsController, ITrackRecordsProvider>().To<TrackRecordsController>().InSingletonScope();
            Bind<ISessionEventsController>().To<SessionEventsController>();
            Bind<CachedViewModelsFactory>().ToSelf().InSingletonScope();

            Bind<PitBoardViewModel>().ToSelf();
            Bind<IPitBoardController, PitBoardController>().To<PitBoardController>().InSingletonScope();
            Bind<IAutonomousPitBoardDataProvider>().To<GapPitBoardProvider>();
            Bind<IAutonomousPitBoardDataProvider>().To<YellowAheadPitBoardProvider>();
            Bind<IAutonomousPitBoardDataProvider>().To<ClearToRejoinBoardProvider>();
            Bind<IAutonomousPitBoardDataProvider>().To<PitEstimationPitBoardProvider>();
            Bind<IAutonomousPitBoardDataProvider>().To<SafetyCarPitBoardProvider>();
            Bind<IAutonomousPitBoardDataProvider>().To<PitEntrySpeedProvider>();
            Bind<IAutonomousPitBoardDataProvider>().To<PitStopDurationBoardProvider>();
            Bind<SessionTimingFactory>().ToSelf();
            Bind<TimingApplicationViewModel>().To<TimingApplicationViewModel>().InSingletonScope();
            Bind<IPaceProvider, PaceProviderFromTimingApplicationViewModel>().To<PaceProviderFromTimingApplicationViewModel>().InSingletonScope();
            Bind<DriverDetailsController>().ToSelf().InSingletonScope();
            Bind<ISessionTelemetryControllerFactory>().To<SessionTelemetryControllerFactory>();
            Bind<IMapManagementController, MapManagementController>().To<MapManagementController>().InSingletonScope();
            Bind<SessionInfoViaTimingViewModelProvider>().To<SessionInfoViaTimingViewModelProvider>().InSingletonScope();
            Bind<ISessionInformationProvider>().ToMethod(c => c.Kernel.Get<SessionInfoViaTimingViewModelProvider>()).WhenInjectedExactlyInto<MapViewController>();
            Bind<ISessionInformationProvider>().ToMethod(c => c.Kernel.Get<SessionInfoViaTimingViewModelProvider>()).WhenInjectedExactlyInto<DriverPositionViewModel>();
            Bind<ISessionInformationProvider>().ToMethod(c => c.Kernel.Get<SessionInfoViaTimingViewModelProvider>()).WhenInjectedExactlyInto<PitWindowStrategy>();
            Bind<ISessionInformationProvider>().ToMethod(c => c.Kernel.Get<SessionInfoViaTimingViewModelProvider>()).WhenInjectedExactlyInto<TimedStintStrategy>();

            Bind<SettingsWindowController>().ToSelf().Named("SettingsWindow").DefinesNamedScope("SettingsWindow");
            Bind<IDefaultLayoutFactory>().To<TimingApplicationDefaultLayoutFactory>().WhenAnyAncestorNamed("SettingsWindow").InNamedScope("SettingsWindow");
            Bind<ILayoutElementNamesProvider>().To<TimingApplicationElementNamesProvider>().WhenAnyAncestorNamed("SettingsWindow").InNamedScope("SettingsWindow");
            Bind<ISimulatorDataSetVisitor>().To<ForceSingleClassVisitor>();
            Bind<ILapEventProvider, ILapCompletedEventProvider>().To<LapTimings.LapEventProvider>().InSingletonScope();
            Bind<ISimSettingControllerFactory>().ToFactory();
            Bind<IOpenTelemetryChecklistController>().To<OpenTelemetryChecklistController>();

            Bind<IColumnVisibilityAdapter>().To<ClassVisibilityAdapter>().WithMetadata(BindingMetadataIds.AdapterColumnName, "Class Ribbon");
            Bind<IColumnVisibilityAdapter>().To<ClassVisibilityAdapter>().WithMetadata(BindingMetadataIds.AdapterColumnName, "Class");
            Bind<IColumnVisibilityAdapter>().To<RatingVisibilityAdapter>().WithMetadata(BindingMetadataIds.AdapterColumnName, "Rating");
            Bind<IColumnVisibilityAdapter>().To<PointsVisibilityAdapter>().WithMetadata(BindingMetadataIds.AdapterColumnName, "Points");
            Bind<IColumnVisibilityAdapter>().To<TyreVisibilityAdapter>().WithMetadata(BindingMetadataIds.AdapterColumnName, "Tyre Kind Ribbon");
            Bind<IColumnVisibilityAdapter>().To<VirtualEnergyVisibilityAdapter>().WithMetadata(BindingMetadataIds.AdapterColumnName, "Virtual Energy");
            Bind<IColumnVisibilityAdapter>().To<PitsVisibilityAdapter>().WithMetadata(BindingMetadataIds.AdapterColumnName, "Pit Information");
            Bind<IColumnVisibilityAdapter>().To<ReputationVisibilityAdapter>().WithMetadata(BindingMetadataIds.AdapterColumnName, "Reputation");
            Bind<IColumnVisibilityAdapter>().To<PushToPassVisibilityAdapter>().WithMetadata(BindingMetadataIds.AdapterColumnName, "Push to Pass Uses");
            Bind<IColumnVisibilityAdapter>().To<CarNumberVisibilityAdapter>().WithMetadata(BindingMetadataIds.AdapterColumnName, ColumnNames.CarRaceNumber);
            Bind<IColumnVisibilityAdapter>().To<TeamNameVisibilityAdapter>().WithMetadata(BindingMetadataIds.AdapterColumnName, ColumnNames.TeamName);

            /*Bind<SessionTimingController, ISessionInfo>().To<SessionTimingController>().DefinesNamedScope(NamedScopes.SessionScope);*/
            Bind<PitStopStatisticsController, IPitStopStatisticProvider>().To<PitStopStatisticsController>().InSingletonScope();
            Bind<PitPredictionController, IPlayerPitEstimationProvider>().To<PitPredictionController>().InSingletonScope();
            Bind<DriverTimingBySessionProvider, IDriverTimingProvider>().To<DriverTimingBySessionProvider>().InSingletonScope();
            Bind<IGapClosureInformationProvider>().To<GapClosureInformationProvider>();

            Bind<IAutoDeleter>().To<LmuReplayDeleter>();
            Bind<IAutoDeleter>().To<AccSaveGameDeleter>();
            Bind<IAutoDeleter>().To<R3EReplayDeleter>();

            Bind<IPitOutlierFilter>().To<PitOutlierFilter>();
            Bind<ISimulatorsRecordsMigration>().To<V001CarDisplayNameMigration>();
            Bind<ISimulatorsRecordsMigration>().To<V002LapRecordAverageSpeedMigration>();
        }
    }
}