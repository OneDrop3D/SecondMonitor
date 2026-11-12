namespace SecondMonitor.Rating.Application
{
    using Calculator.Elo;

    using Championship;
    using Championship.Controller;
    using Championship.Filters;
    using Championship.Manufacturers;
    using Championship.Operations;
    using Championship.Pool;
    using Championship.Repository;
    using Championship.ViewModels;
    using Championship.ViewModels.Creation;
    using Championship.ViewModels.Creation.Calendar;
    using Championship.ViewModels.Creation.Calendar.Predefined;
    using Championship.ViewModels.Creation.Session;
    using Championship.ViewModels.Creation.Session.SessionLength;
    using Championship.ViewModels.Events;
    using Championship.ViewModels.IconState;
    using Championship.ViewModels.Overview;
    using Championship.ViewModels.Selection;
    using Common.Calculator;
    using Contracts.Rating;
    using Ninject.Modules;
    using Rating.Controller;
    using Rating.Controller.RaceObserver;
    using Rating.Controller.SimulatorRating;
    using Rating.Controller.SimulatorRating.RatingUpdater;
    using Rating.RatingProvider;
    using Rating.RatingProvider.FieldRatingProvider;
    using Rating.RatingProvider.FieldRatingProvider.ReferenceRatingProviders;
    using Rating.ViewModels;
    using Rating.ViewModels.Rating;
    using Rating.ViewModels.RatingHistory;
    using Rating.ViewModels.RatingOverview;

    using SecondMonitor.Contracts.NInject;
    using SecondMonitor.DataModel.Championship;
    using SecondMonitor.Rating.Application.Championship.Operations.PointsStrategy;
    using SecondMonitor.Rating.Application.Championship.Repository.Migration;
    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.SimdataManagement.DriverPresentation;
    using SecondMonitor.ViewModels.Repository;

    public class RatingApplicationModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IRatingApplicationController>().To<RatingApplicationController>();
            Bind<IRaceObserverController, ISessionRatingProvider>().To<RaceObserverController>().InSingletonScope();
            Bind<IRatingStorageController>().To<RatingStorageController>();
            Bind<ISimulatorRatingController>().To<SimulatorRatingController>().InSingletonScope();
            Bind<IRaceStateFactory>().To<RaceStateFactory>();

            Bind<IHistoryWindowViewModel>().To<HistoryWindowViewModel>();
            Bind<IRaceResultViewModel>().To<RaceResultViewModel>();
            Bind<IRaceHistoriesViewModel>().To<RaceHistoriesViewModel>();

            Bind<IRatingOverviewWindowViewModel>().To<RatingOverviewWindowViewModel>();
            Bind<ISimulatorRatingsViewModel>().To<SimulatorRatingsViewModel>();
            Bind<IClassRatingViewModel>().To<ClassRatingViewModel>();

            Bind<IRatingApplicationViewModel>().To<RatingApplicationViewModel>();
            Bind<IRatingViewModel>().To<RatingViewModel>();
            Bind<IQualificationResultRatingProvider>().To<QualificationResultRatingProvider>();
            Bind<IRatingUpdater>().To<RatingUpdater>();

            Bind<IReferenceRatingProviderFactory>().To<ReferenceRatingProviderFactory>();
            Bind<IReferenceRatingProvider>().To<LeadPackReferenceRatingProvider>().Named("Leading Group");
            Bind<IReferenceRatingProvider>().To<LeaderReferenceRatingProvider>().Named("Leader");
            Bind<IReferenceRatingProvider>().To<FirstThirdsReferenceRatingProvider>().Named("First 1/3 of Field");
            Bind<IReferenceRatingProvider>().To<FirstHalfReferenceRatingProvider>().Named("First 1/2 of Field");
            Bind<IReferenceRatingProvider>().To<MidfieldReferenceRatingProvider>().Named("Midfield");
            Bind<IReferenceRatingProvider>().To<AverageTimeReferenceRatingProvider>().Named("Average Time");
            Bind<IReferenceRatingProvider>().To<LeaderPlus1ReferenceRatingProvider>().Named("Leader - 1%");
            Bind<IReferenceRatingProvider>().To<LeaderPlus3ReferenceRatingProvider>().Named("Leader - 3%");

            Bind<IRatingCalculator>().To<EloCalculator>();

            Bind<ISimulatorsTrackMappingRepository>().To<SimulatorsTrackMappingRepository>().InSingletonScope();
            Bind<ITrackTemplateToSimTrackMapper>().To<TrackTemplateToSimTrackMapper>().InSingletonScope();

            Bind<IChampionshipController, IChampionshipCurrentEventPointsProvider>().To<ChampionshipController>().InSingletonScope();
            Bind<IChampionshipsRepository>().To<ChampionshipFileRepository>().InSingletonScope();
            Bind<IChampionshipOverviewController>().To<ChampionshipOverviewController>();
            Bind<IChampionshipsPool>().To<ChampionshipsPool>().InSingletonScope();
            Bind<IChampionshipCreationController>().To<ChampionshipCreationController>();
            Bind<ISimulatorRatingProvider>().To<SimulatorRatingProvider>();
            Bind<ChampionshipIconStateViewModel>().ToSelf();

            Bind<ChampionshipsOverviewViewModel>().ToSelf();
            Bind<ChampionshipOverviewViewModel>().ToSelf();
            Bind<ChampionshipCreationViewModel>().ToSelf();
            Bind<CalendarDefinitionViewModel>().ToSelf();
            Bind<CreatedCalendarViewModel>().ToSelf();
            Bind<AvailableTracksViewModel>().ToSelf();
            Bind<EditableCalendarEntryViewModel>().ToSelf();
            Bind<ExistingTrackCalendarEntryViewModel>().ToSelf();
            Bind<GenericTrackTemplateViewModel>().ToSelf();
            Bind<CalendarPlaceholderEntryViewModel>().ToSelf();
            Bind<CalendarPreviewViewModel>().ToSelf();

            Bind<OpenLastResultPitBoardViewModel>().ToSelf();

            Bind<NextRaceOverviewViewModel>().ToSelf();

            Bind<CalendarTemplateGroupViewModel>().ToSelf();
            Bind<CalendarTemplateViewModel>().ToSelf();
            Bind<PredefinedCalendarSelectionViewModel>().ToSelf();

            Bind<SessionsDefinitionViewModel>().ToSelf();
            Bind<ChampionshipsSelectionViewModel>().ToSelf();

            Bind<EventStartingViewModel>().ToSelf();
            Bind<TrackOverviewViewModel>().ToSelf();

            Bind<DriverStandingViewModel>().ToSelf();
            Bind<DriversStandingOverviewViewModel>().ToSelf();
            Bind<ChampionshipDetailViewModel>().ToSelf();

            Bind<SessionCompletedViewModel>().ToSelf();
            Bind<PodiumViewModel>().ToSelf();
            Bind<DriverFinishViewModel>().ToSelf();
            Bind<DriversSessionResultViewModel>().ToSelf();
            Bind<ManufacturerNewStandingViewModel>().ToSelf();
            Bind<DriversNewStandingsViewModel>().ToSelf();
            Bind<EventTitleViewModel>().ToSelf();

            Bind<CalendarResultsOverviewViewModel>().ToSelf();
            Bind<EventResultOverviewViewModel>().ToSelf();
            Bind<SessionResultWithTitleViewModel>().ToSelf();
            Bind<ChampionshipSessionsResultsViewModel>().ToSelf();
            Bind<TrophyViewModel>().ToSelf();

            Bind<ISessionLengthDefinitionViewModel>().To<TimeLengthDefinitionViewModel>();
            Bind<ISessionLengthDefinitionViewModel>().To<LapsLengthDefinitionViewModel>();
            Bind<ISessionLengthDefinitionViewModel>().To<DistanceLengthDefinitionViewModel>();

            Bind<ICalendarEntryViewModelFactory>().To<CalendarEntryViewModelFactory>();
            Bind<ISessionDefinitionViewModelFactory>().To<SessionDefinitionViewModelFactory>();

            Bind<IChampionshipFactory>().To<ChampionshipFactory>();

            Bind<IChampionshipCondition>().To<SimulatorRequirement>();
            Bind<IChampionshipCondition>().To<CarClassRequirement>();
            Bind<IChampionshipCondition>().To<PlayerPresentRequirement>();
            Bind<IChampionshipCondition>().To<TrackRequirement>();
            Bind<IChampionshipCondition>().To<DistanceRequirement>();
            Bind<IChampionshipCondition>().To<OpponentsRequirements>();
            Bind<IChampionshipCondition>().To<SinglePlayerRequirement>();
            Bind<IChampionshipEligibilityEvaluator>().To<ChampionshipEligibilityEvaluator>();

            Bind<IChampionshipEventController>().To<ChampionshipEventController>();
            Bind<IChampionshipSelectionController>().To<ChampionshipSelectionController>();

            Bind<IChampionshipManipulator>().To<ChampionshipManipulator>();
            Bind<ISubChampionshipManipulator>().To<TeamChampionshipManipulator>();
            Bind<ISubChampionshipManipulator>().To<ManufacturersChampionshipManipulator>();
            Bind<IChampionshipDialogProvider>().To<ChampionshipDialogProvider>();
            Bind<IStoredCalendarsAccessor>().To<StoredCalendarAccessor>();
            Bind<IScoringStrategyFactory>().To<ScoringStrategyFactory>();
            Bind<IScoringStrategy>().To<F1FairScoringStrategy>().WithMetadata(BindingMetadataIds.ChampionshipPointStrategy, nameof(ScoringKind.F1Fair));
            Bind<IScoringStrategy>().To<F1ScoringStrategy>().WithMetadata(BindingMetadataIds.ChampionshipPointStrategy, nameof(ScoringKind.F1));
            Bind<IScoringStrategy>().To<ImsaScoringStrategy>().WithMetadata(BindingMetadataIds.ChampionshipPointStrategy, nameof(ScoringKind.Imsa));

            Bind<RestartChampionshipDialogViewModel>().ToSelf();
            Bind<IManufacturerProvider>().To<ManufacturerProvider>();

            Bind<IDtoMigration<AllChampionshipsDto>>().To<V01ManufacturerStandingsForEachClass>();
            Bind<IDtoMigration<AllChampionshipsDto>>().To<V02DriverFirstUsedName>();
            Bind<IDtoMigration<AllChampionshipsDto>>().To<V03RemoveClassSuffix>();
            Bind<IAutomaticPresentationProvider, IChampionshipDriverPresentationProvider>().To<ChampionshipDriverPresentationProvider>().InSingletonScope();
        }
    }
}
