namespace SecondMonitor.Telemetry.TelemetryApplication
{
    using AggregatedCharts;
    using AggregatedCharts.Filter;
    using AggregatedCharts.Histogram.Extractors;
    using AggregatedCharts.Histogram.Providers;
    using AggregatedCharts.LineSeries.Providers;
    using AggregatedCharts.ScatterPlot.Extractors;
    using AggregatedCharts.ScatterPlot.Providers;
    using Checks;
    using Checks.Evaluators;
    using Checks.Evaluators.BrakeTemperatures;
    using Checks.Evaluators.Engine;
    using Checks.Evaluators.LapSummary;
    using Checks.Evaluators.RideHeight;
    using Checks.Evaluators.TyrePressures;
    using Checks.Evaluators.TyreTemperatures;
    using Contracts.NInject;
    using Contracts.UserInput;
    using Controllers.AggregatedChart;
    using Controllers.MainWindow;
    using Controllers.MainWindow.GraphPanel;
    using Controllers.MainWindow.LapPicker;
    using Controllers.MainWindow.MapView;
    using Controllers.MainWindow.Replay;
    using Controllers.MainWindow.Snapshot;
    using Controllers.MainWindow.Workspace;
    using Controllers.OpenWindow;
    using Controllers.Settings;
    using Controllers.Settings.Car;
    using Controllers.Settings.DefaultCarProperties;
    using Controllers.SettingsWindow;
    using Controllers.Synchronization;
    using Controllers.Synchronization.Graphs;
    using Controllers.TelemetryLoad;
    using Controllers.TelemetryLoad.DataFillers;
    using DataAdapters;
    using LapAutoSelector;

    using Ninject.Extensions.NamedScope;
    using Ninject.Modules;
    using Repository;

    using SecondMonitor.Telemetry.TelemetryApplication.Controllers.LapsCharts;
    using SecondMonitor.ViewModels.Colors;
    using SecondMonitor.ViewModels.Layouts.Factory;

    using Settings;
    using TelemetryManagement.StoryBoard;
    using ViewModels;
    using ViewModels.AggregatedCharts;
    using ViewModels.AggregatedCharts.LineSeries;
    using ViewModels.GraphPanel;
    using ViewModels.GraphPanel.Chassis;
    using ViewModels.GraphPanel.DataExtractor;
    using ViewModels.GraphPanel.DualSeries;
    using ViewModels.GraphPanel.Inputs;
    using ViewModels.GraphPanel.Wheels;
    using ViewModels.LapPicker;
    using ViewModels.LoadedLapCache;
    using ViewModels.MapView;
    using ViewModels.OpenWindow;
    using ViewModels.Replay;
    using ViewModels.SettingsWindow;
    using ViewModels.SnapshotSection;
    using WindowsControls.WPF.UserInput;
    using Workspaces.Providers;
    using Workspaces.Providers.BuildIn;
    using Workspaces.Providers.BuildIn.ChartSet;
    using LapSummaryViewModel = ViewModels.LapPicker.LapSummaryViewModel;

    public class TelemetryApplicationModule : NinjectModule
    {
        private const string MainWidowScopeName = "MainWindow";
        private const string WorkspaceScopeName = "WorkspaceEditor";

        public override void Load()
        {
            Bind<IMainWindowController>().To<MainWindowController>().DefinesNamedScope(MainWidowScopeName);
            Bind<TelemetryStoryBoardFactory>().ToSelf();
            Bind<ILoadedLapsCache>().To<LoadedLapsCache>().InSingletonScope();
            Bind<ITelemetryLoadController>().To<TelemetryLoadController>().InSingletonScope();
            Bind<ITelemetryViewsSynchronization>().To<TelemetryViewsSynchronization>().InSingletonScope();
            Bind<IGraphViewSynchronization>().To<GraphViewSynchronization>().InNamedScope(MainWidowScopeName);
            Bind<IDataPointSelectionSynchronization>().To<DataPointSelectionSynchronization>().InSingletonScope();
            Bind<ILapStintSynchronization>().To<LapStintSynchronization>().InSingletonScope();
            Bind<IMainWindowViewModel>().To<MainWindowViewModel>().InSingletonScope();
            Bind<IMapViewController>().To<MapViewController>().InNamedScope(MainWidowScopeName);
            Bind<ILapColorSynchronization>().To<LapColorSynchronization>().InSingletonScope();
            Bind<IColorPaletteProvider>().To<BasicColorPaletteProvider>().InNamedScope(MainWidowScopeName);
            Bind<ITelemetrySettingsRepository>().To<TelemetrySettingsRepository>();
            Bind<ISettingsController>().To<SettingsController>().InSingletonScope();
            Bind<ICarSettingsController>().To<CarSettingsController>();
            Bind<IReplayController>().To<ReplayController>();
            Bind<IOpenWindowController>().To<OpenWindowController>();
            Bind<ISnapshotSectionController>().To<SnapshotSectionController>();
            Bind<ISettingsWindowController>().To<SettingsWindowController>();
            Bind<IAggregatedChartsController>().To<AggregatedChartsController>();
            Bind<IWorkspaceController>().To<WorkspaceController>();

            Bind<ITelemetryRepositoryFactory>().To<TelemetryRepositoryFactory>();
            Bind<ILapPickerController>().To<LapPickerController>();
            Bind<ISnapshotSectionViewModel>().To<SnapshotSectionViewModel>();

            Bind<LapSelectionViewModel>().ToSelf();
            Bind<ILapSummaryViewModel>().To<LapSummaryViewModel>();
            Bind<IReplayViewModel>().To<ReplayViewModel>();
            Bind<IPedalSectionViewModel>().To<PedalSectionViewModel>();
            Bind<IMapViewViewModel>().To<MapViewViewModel>();
            Bind<IOpenWindowViewModel>().To<OpenWindowViewModel>();
            Bind<IOpenWindowSessionInformationViewModel>().To<OpenWindowSessionInformationViewModel>();
            Bind<TelemetrySettingsViewModel>().ToSelf();
            Bind<IGraphSettingsViewModel>().To<GraphSettingsViewModel>();
            Bind<IWorkspacesSeriesChartsController>().To<WorkspacesSeriesChartsController>().InNamedScope(MainWidowScopeName);
            Bind<IWorkspaceAggregatedChartsController>().To<WorkspaceAggregatedChartsController>().InNamedScope(MainWidowScopeName);

            Bind<ILapCustomPathsCollection>().To<LapCustomPathsCollection>();
            Bind<IUserInputProvider>().To<DialogUserInputProvider>();

            Bind<IGraphViewModel>().To<LapTimeGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.LapTimeChartName);
            Bind<IGraphViewModel>().To<SteeringAngleGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.SteeringAngleChartName);
            Bind<IGraphViewModel>().To<ThrottleGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.ThrottleChartName);
            Bind<IGraphViewModel>().To<BrakeGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.BrakeChartName);
            Bind<IGraphViewModel>().To<ClutchGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.ClutchChartName);
            Bind<IGraphViewModel>().To<SpeedGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.SpeedChartName);
            Bind<IGraphViewModel>().To<EngineRpmGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.EngineRpmChartName);
            Bind<IGraphViewModel>().To<GearGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.GearChartName);
            Bind<IGraphViewModel>().To<LateralGGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.LatGChartName);
            Bind<IGraphViewModel>().To<TotalGGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.TotalGChartName);
            Bind<IGraphViewModel>().To<HorizontalGGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.LongGChartName);
            Bind<IGraphViewModel>().To<BrakeTemperaturesGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.BrakeTemperatureChartName);
            Bind<IGraphViewModel>().To<TyrePressuresGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.TyrePressuresChartName);
            Bind<IGraphViewModel>().To<LeftFrontTyreTempsGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.LeftFrontTyreTempChartName);
            Bind<IGraphViewModel>().To<RightFrontTyreTempsGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.RightFrontTyreTempChartName);
            Bind<IGraphViewModel>().To<LeftRearTyreTempsGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.LeftRearTyreTempChartName);
            Bind<IGraphViewModel>().To<RightRearTyreTempsGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.RightRearTyreTempChartName);
            Bind<IGraphViewModel>().To<WheelRpsGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.WheelRpsChartName);
            Bind<IGraphViewModel>().To<SuspensionTravelGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.SuspensionTravelChartName);
            Bind<IGraphViewModel>().To<RideHeightGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.RideHeightChartName);
            Bind<IGraphViewModel>().To<ChassisRideHeightGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.ChassisRideChartName);
            Bind<IGraphViewModel>().To<SuspensionVelocityGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.SuspensionVelocityChartName);
            Bind<IGraphViewModel>().To<OverallDownForceGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.DownforceOverallChartName);
            Bind<IGraphViewModel>().To<TurboBoostGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.TurboBoostChartName);
            Bind<IGraphViewModel>().To<FrontRearDownForceGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.DownforceFrontRearChartName);
            Bind<IGraphViewModel>().To<CamberGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.CamberChartName);
            Bind<IGraphViewModel>().To<RakeGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.RakeChartName);
            Bind<IGraphViewModel>().To<YawGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.YawChartName);
            Bind<IGraphViewModel>().To<TyreLoadGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.TyreLoadChartName);
            Bind<IGraphViewModel>().To<WheelSlipGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.WheelSlipChartName);
            Bind<IGraphViewModel>().To<CombinedPedalsGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.CombinedPedalsChartName);
            Bind<IGraphViewModel>().To<FrontRearBrakesTemperatureChart>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.BrakesTemperatureFrontRearChartName);
            Bind<IGraphViewModel>().To<InsideOutsideTyreTempGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.OutsideInsideTyresTempChartName);
            Bind<IGraphViewModel>().To<FrontRearTyreLoadsGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.FrontRearTyreLoads);
            Bind<IGraphViewModel>().To<SteeringInputGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.SteeringInputChartName);
            Bind<IGraphViewModel>().To<CrossTyreLoadChartViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.CrossTyreLoad);
            Bind<IGraphViewModel>().To<FrontWheelsRps>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.FrontTyresRps);
            Bind<IGraphViewModel>().To<FrontWheelsSlip>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.FrontTyresSlip);
            Bind<IGraphViewModel>().To<RearWheelsRps>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.RearTyresRps);
            Bind<IGraphViewModel>().To<RearWheelsSlip>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.RearTyresSlip);
            Bind<IGraphViewModel>().To<FrontTyresSpeedDifference>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.FrontTyresRpsDiff);
            Bind<IGraphViewModel>().To<RearTyresSpeedDifference>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.RearTyresRpsDiff);
            Bind<IGraphViewModel>().To<OilTemperatureGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.OilTemperature);
            Bind<IGraphViewModel>().To<CoolantTemperatureGraphViewModel>().WithMetadata(BindingMetadataIds.SeriesChartsNameBinding, SeriesChartNames.CoolantTemperature);
            Bind<SeriesChartFactory>().ToSelf();
            /*Bind<IGraphViewModel>().To<WorldYawGraphViewModel>();
            Bind<IGraphViewModel>().To<XGraphViewModel>();
            Bind<IGraphViewModel>().To<YGraphViewModel>();*/

            Bind<ISingleSeriesDataExtractor>().To<SimpleSingleSeriesDataExtractor>();
            Bind<ISingleSeriesDataExtractor>().To<CompareToReferenceDataExtractor>();

            Bind<ILapAutoSelector>().To<EmptyLapAutoSelector>();

            Bind<IAggregatedChartProvider>().To<SuspensionVelocityHistogramProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.SuspensionVelocityHistogramChartName);
            Bind<IAggregatedChartProvider>().To<SpeedToRpmChartProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.SpeedToRpmChartName);
            Bind<IAggregatedChartProvider>().To<SpeedHorizontalAccelerationChartProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.SpeedToLongitudinalAccChartName);
            Bind<IAggregatedChartProvider>().To<RpmToHorizontalGChartProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.RpmToLongitudinalAccChartName);
            Bind<IAggregatedChartProvider>().To<RpmHistogramProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.RpmHistogramChartName);
            Bind<IAggregatedChartProvider>().To<RideHeightHistogramProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.RideHeightHistogramChartName);
            Bind<IAggregatedChartProvider>().To<RideHeightToHorizontalAccProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.RideHeightToLongitudinalAccChartName);
            Bind<IAggregatedChartProvider>().To<RideHeightToLateralAccProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.RideHeightToLateralAccChartName);
            Bind<IAggregatedChartProvider>().To<RideHeightToSpeedProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.RideHeightToSpeedChartName);
            Bind<IAggregatedChartProvider>().To<SpeedToDownforceProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.SpeedToDownforceChartName);
            Bind<IAggregatedChartProvider>().To<SpeedToRakeProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.SpeedToRakeChartName);
            Bind<IAggregatedChartProvider>().To<RearRollAngleToFrontRollAngleProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.RearToFrontRollAngleChartName);
            Bind<IAggregatedChartProvider>().To<CamberToLateralGChartProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.CamberToLateralAccelerationChartName);
            Bind<IAggregatedChartProvider>().To<CamberHistogramProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.CamberHistogramChartName);
            Bind<IAggregatedChartProvider>().To<TyreLoadToCamberProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.TyreLoadToCamberChartName);
            Bind<IAggregatedChartProvider>().To<TyreLoadToLatGProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.TyreLoadToLatAccelerationChartName);
            Bind<IAggregatedChartProvider>().To<TyreLoadToLongGProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.TyreLoadToLongAccelerationChartName);
            Bind<IAggregatedChartProvider>().To<TyreLoadHistogramProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.TyreLoadHistogramChartName);
            Bind<IAggregatedChartProvider>().To<PowerCurveProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.PowerCurveChartName);
            Bind<IAggregatedChartProvider>().To<TractionCircleProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.TractionCircleChartName);
            Bind<IAggregatedChartProvider, SpeedInTurnsHistogramProvider>().To<SpeedInTurnsHistogramProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.SpeedInTurnsHistogramChartName);
            Bind<IAggregatedChartProvider>().To<WheelSlipAccelerationProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.WheelSlipAccelerationChartName);
            Bind<IAggregatedChartProvider>().To<WheelSlipBrakeProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.WheelSlipBrakingChartName);
            Bind<IAggregatedChartProvider>().To<SpeedToSuspensionTravelProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.SuspensionTravelToSpeedChartName);
            Bind<IAggregatedChartProvider>().To<LatAccelerationToFrontRollAngleProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.LatAccelerationToFrontRollAngleChartName);
            Bind<IAggregatedChartProvider>().To<LatAccelerationToRearRollAngleProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.LatAccelerationToRearRollAngleChartName);
            Bind<IAggregatedChartProvider>().To<LatAccelerationToSteeringInputProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.LatAccToSteeringInput);
            Bind<IAggregatedChartProvider>().To<SuspensionVelocityLineSeriesProvider>().WithMetadata(BindingMetadataIds.AggregatedChartProviderName, AggregatedChartNames.SuspensionVelocityLineSeriesChartName);

            //Bind<IAggregatedChartProvider>().To<LatToLogGProvider>();

            Bind<LatToLogGProvider>().ToSelf();
            Bind<SpeedToLatGProvider>().ToSelf();
            Bind<SpeedToLongGProvider>().ToSelf();

            Bind<SuspensionVelocityHistogramDataExtractor>().ToSelf();
            Bind<RideHeightGraphViewModel>().ToSelf();

            Bind<TyreLoadToCamberExtractor>().ToSelf();

            Bind<SpeedToRpmScatterPlotExtractor>().ToSelf();
            Bind<ITelemetryFilter>().To<NoClutchFilter>().WhenInjectedExactlyInto<SpeedToRpmScatterPlotExtractor>();
            Bind<ITelemetryFilter>().To<NoBrakeFilter>().WhenInjectedExactlyInto<SpeedToRpmScatterPlotExtractor>();

            Bind<HorizontalAccelerationToRideHeightExtractor>().ToSelf();
            Bind<ITelemetryFilter>().To<NoLateralAccelerationFilter>().WhenInjectedExactlyInto<HorizontalAccelerationToRideHeightExtractor>();

            Bind<TyreLoadToLatGExtractor>().ToSelf();
            Bind<ITelemetryFilter>().To<NoHorizontalAccelerationFilter>().WhenInjectedExactlyInto<TyreLoadToLatGExtractor>();

            Bind<TyreLoadToLongGExtractor>().ToSelf();
            Bind<ITelemetryFilter>().To<NoLateralAccelerationFilter>().WhenInjectedExactlyInto<TyreLoadToLongGExtractor>();

            Bind<LateralAccelerationToRideHeightExtractor>().ToSelf();
            Bind<ITelemetryFilter>().To<NoHorizontalAccelerationFilter>().WhenInjectedExactlyInto<LateralAccelerationToRideHeightExtractor>();

            Bind<SpeedToRakeExtractor>().ToSelf();
            //Bind<ITelemetryFilter>().To<NoLateralAccelerationFilter>().WhenInjectedExactlyInto<SpeedToRakeExtractor>();
            Bind<ITelemetryFilter>().To<NoBrakeFilter>().WhenInjectedExactlyInto<SpeedToRakeExtractor>();

            Bind<SpeedToRideHeightExtractor>().ToSelf();
            Bind<SpeedToDownforceExtractor>().ToSelf();

            Bind<CamberHistogramExtractor>().ToSelf();

            Bind<SpeedHistogramExtractor>().ToSelf();

            Bind<WheelSlipExtractor>().ToSelf();

            Bind<SpeedToHorizontalGExtractor>().ToSelf();
            Bind<ITelemetryFilter>().To<FullThrottleFilter>().WhenInjectedExactlyInto<SpeedToHorizontalGExtractor>();
            Bind<ITelemetryFilter>().To<NoBrakeFilter>().WhenInjectedExactlyInto<SpeedToHorizontalGExtractor>();
            Bind<ITelemetryFilter>().To<NoClutchFilter>().WhenInjectedExactlyInto<SpeedToHorizontalGExtractor>();

            Bind<RpmToHorizontalGExtractor>().ToSelf();
            Bind<ITelemetryFilter>().To<FullThrottleFilter>().WhenInjectedExactlyInto<RpmToHorizontalGExtractor>();
            Bind<ITelemetryFilter>().To<NoBrakeFilter>().WhenInjectedExactlyInto<RpmToHorizontalGExtractor>();
            Bind<ITelemetryFilter>().To<NoClutchFilter>().WhenInjectedExactlyInto<RpmToHorizontalGExtractor>();

            Bind<RpmHistogramDataExtractor>().ToSelf();
            Bind<ITelemetryFilter>().To<NoBrakeFilter>().WhenInjectedExactlyInto<RpmHistogramDataExtractor>();
            Bind<ITelemetryFilter>().To<NoClutchFilter>().WhenInjectedExactlyInto<RpmHistogramDataExtractor>();

            Bind<TyreLoadHistogramExtractor>().ToSelf();

            Bind<CamberToLateralGExtractor>().ToSelf();

            Bind<RpmToPowerExtractor>().ToSelf();
            Bind<RpmToTorqueExtractor>().ToSelf();

            Bind<LateralToLongGExtractor>().ToSelf();
            Bind<RearRollAngleToFrontRollAngleExtractor>().ToSelf();
            Bind<LateralAccFilter>().ToSelf();
            Bind<ThrottlePositionFilter>().ToSelf();
            Bind<BrakePositionFilter>().ToSelf();
            Bind<LoadedWheelFilter>().ToSelf();
            Bind<CamberFilter>().ToSelf();
            Bind<InGearFilter>().ToSelf();
            Bind<PositiveTorqueFilter>().ToSelf();
            Bind<SpeedToLatGAllPointsExtractor>().ToSelf();
            Bind<SpeedToLongGAllPointsExtractor>().ToSelf();

            Bind<LineSeriesHistogramsChartViewModel>().ToSelf();
            Bind<IColorPaletteProvider>().To<BasicColorPaletteProvider>().WhenInjectedInto<LineSeriesHistogramsChartViewModel>();

            Bind<LatAccelerationToFrontRollAngleExtractor>().ToSelf();
            Bind<LatAccelerationToRearRollAngleExtractor>().ToSelf();
            Bind<LatAccelerationToSteeringInputExtractor>().ToSelf();
            /*Bind<ITelemetryFilter>().To<NoHorizontalAccelerationFilter>().WhenInjectedExactlyInto<LatAccelerationToSteeringAngleExtractor>();*/

            Bind<IAggregatedChartSelectorViewModel>().To<AggregatedChartSelectorViewModel>();
            Bind<IGearTelemetryFilter>().To<GearTelemetryFilter>();

            Bind<IAggregatedChartSettingsViewModel>().To<AggregatedChartSettingsViewModel>();

            Bind<IDefaultCarPropertiesProvider>().To<R3EDefaultCarPropertiesProvider>();
            Bind<DefaultR3ECarSettingsRepository>().ToSelf();

            Bind<IMissingTelemetryFiller>().To<SlipInformationFiller>();
            Bind<TrackMapWithCustomPathsViewModel>().ToSelf();
            Bind<AggregatedChartProviderFactory>().ToSelf();

            Bind<IWorkspaceProvider>().To<WorkspaceProvider>();
            Bind<IChartSetProvider>().To<ChartSetProvider>();

            Bind<WorkspaceEditorController>().ToSelf().Named(WorkspaceScopeName).DefinesNamedScope(WorkspaceScopeName);
            Bind<IDefaultLayoutFactory>().To<EmptyRowsLayoutFactory>().WhenAnyAncestorNamed(WorkspaceScopeName).InNamedScope(WorkspaceScopeName);
            Bind<ILayoutElementNamesProvider>().To<LayoutNamesProvider>().WhenAnyAncestorNamed(WorkspaceScopeName).InNamedScope(WorkspaceScopeName);

            Bind<IBuildInWorkspaceProvider>().To<DefaultWorkspaceProvider>();
            /*Bind<IBuildInWorkspaceProvider>().To<DefaultWorkspaceProviderTemp>();*/

            Bind<IBuildInChartSetProvider>().To<CompareChartSetProvider>();
            Bind<IBuildInChartSetProvider>().To<DriverChartSetProvider>();
            Bind<IBuildInChartSetProvider>().To<BrakesChartSetProvider>();
            Bind<IBuildInChartSetProvider>().To<SuspensionChartSetProvider>();
            Bind<IBuildInChartSetProvider>().To<EngineChartSetProvider>();
            Bind<IBuildInChartSetProvider>().To<TyresChartSetProvider>();
            Bind<IBuildInChartSetProvider>().To<AerodynamicsChartSetProvider>();
            Bind<IBuildInChartSetProvider>().To<RideHeightChartSetProvider>();
            Bind<IBuildInChartSetProvider>().To<SteeringChartSetProvider>();
            Bind<IBuildInChartSetProvider>().To<DifferentialChartSetProvider>();

            Bind<TyreLoadAdapter>().ToSelf();
            Bind<DownforceAdapter>().ToSelf();
            Bind<DownforceChassisAdapter>().ToSelf();

            Bind<ITelemetryCheckController>().To<TelemetryCheckController>();
            Bind<ITelemetryEvaluator>().To<LapSummaryEvaluator>();
            Bind<ITelemetryEvaluator>().To<EngineTemperature>();
            Bind<ITelemetryEvaluator>().To<TyrePressures>();
            Bind<ITelemetryEvaluator>().To<TyreTemperatures>();
            Bind<ITelemetryEvaluator>().To<BrakeTemperatures>();
            Bind<ITelemetryEvaluator>().To<RideHeight>();

            Bind<ILapsChartsController>().To<LapsChartsController>();
        }
    }
}