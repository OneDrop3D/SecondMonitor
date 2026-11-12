namespace SecondMonitor.ViewModels
{
    using CarStatus;
    using CarStatus.FuelStatus;
    using Colors;
    using Contracts.Session;
    using Controllers;
    using DataGrid;
    using Dialogs;
    using Factory;
    using FuelConsumption;
    using Layouts;
    using Layouts.Editor;
    using Layouts.Factory;
    using Ninject.Modules;
    using PluginsSettings;

    using SecondMonitor.DataModel.Summary.FuelConsumption;
    using SecondMonitor.ViewModels.Colors.CustomSimColors;
    using SecondMonitor.ViewModels.Formatters;
    using SecondMonitor.ViewModels.FuelConsumption.FuelStrategy;
    using SecondMonitor.ViewModels.FuelConsumption.FuelStrategy.PitWindow;
    using SecondMonitor.ViewModels.FuelConsumption.FuelStrategy.Stint;
    using SecondMonitor.ViewModels.FuelConsumption.FuelStrategy.TankToTank;
    using SecondMonitor.ViewModels.FuelConsumption.FuelStrategy.VirtualTankToTank;
    using SecondMonitor.ViewModels.FuelConsumption.Repository;
    using SecondMonitor.ViewModels.FuelConsumption.Repository.Migration;
    using SecondMonitor.ViewModels.Repository;
    using SecondMonitor.ViewModels.TrackInfo;

    using SessionEvents;
    using Settings;
    using Settings.ViewModel;
    using Settings.ViewModel.Migrations;
    using Settings.ViewModel.Overrides;
    using SimulatorContent;
    using SplashScreen;
    using Track;
    using Track.MapView;
    using Track.MapView.Controller;
    using TrackRecords;
    using Weather;
    using WheelDiameterWizard;

    public class ViewModelsModule : NinjectModule
    {
        public override void Load()
        {
            Bind<ICommandFactory>().To<CommandFactory>();
            Bind<IViewModelFactory>().To<ViewModelFactory>();
            Bind<IChildControllerFactory>().To<ChildControllerFactory>();
            Bind<IWindowService>().To<WindowService>();
            Bind<IDialogService>().To<DialogService>();
            Bind<IBroadcastLimitSettingsViewModel>().To<BroadcastLimitSettingsViewModel>();
            Bind<IPluginConfigurationViewModel>().To<PluginConfigurationViewModel>();
            Bind<IConnectorConfigurationViewModel>().To<ConnectorConfigurationViewModel>();
            Bind<IPluginsConfigurationViewModel>().To<PluginsConfigurationViewModel>();
            Bind<IRemoteConfigurationViewModel>().To<RemoteConfigurationViewModel>();
            Bind<F12019ConfigurationViewModel>().ToSelf();
            Bind<ForzaMotorsportConfigurationViewModel>().ToSelf();
            Bind<PCars2ConfigurationViewModel>().ToSelf();
            Bind<AccConfigurationViewModel>().ToSelf();
            Bind<ISettingsProvider>().To<AppDataSettingsProvider>().InSingletonScope();
            Bind<ISimulatorContentRepository>().To<StoredSimulatorContentRepository>().InSingletonScope();
            Bind<ISimulatorContentController, ISimulatorContentProvider>().To<SimulatorContentController>().InSingletonScope();

            Bind<ITrackRecordsViewModel>().To<TrackRecordsViewModel>();
            Bind<IRecordViewModel>().To<RecordViewModel>();

            Bind<DisplaySettingsViewModel>().ToSelf();

            Bind<TrackRecordViewModel>().ToSelf();
            Bind<CarRecordViewModel>().ToSelf();
            Bind<SimulatorRecordsViewModel>().ToSelf();
            Bind<CarRecordsCollectionViewModel>().ToSelf();
            Bind<RecordEntryViewModel>().ToSelf();
            Bind<WelcomeStageViewModel>().ToSelf();
            Bind<AccelerationStageViewModel>().ToSelf();
            Bind<PreparationStageViewModel>().ToSelf();
            Bind<MeasurementPhaseViewModel>().ToSelf();
            Bind<ResultsStageViewModel>().ToSelf();
            Bind<SplashScreenViewModel>().ToSelf();
            Bind<TrackGeometryViewModel>().ToSelf();
            Bind<TrackWithSectorsGeometryViewModel>().ToSelf();

            Bind<WheelStatusViewModel>().ToSelf();
            Bind<WheelStatusViewModelFactory>().ToSelf();

            Bind<ISessionEventProvider>().To<SessionEventProvider>().InSingletonScope();

            Bind<YesNoDialogViewModel>().ToSelf();
            Bind<SituationOverviewViewModelFactory>().ToSelf();
            Bind<MapViewController>().ToSelf().InSingletonScope();
            Bind<IMapSidePanelViewModel>().To<MapSidePanelViewModel>();
            Bind<IClassColorProvider>().To<OverridableClassColorProvider>().InSingletonScope();
            Bind<IColorPaletteProvider>().To<BasicColorPaletteProvider>().WhenInjectedExactlyInto<ClassColorProvider>();
            Bind<IColorPaletteProvider>().To<BasicColorPaletteProvider>().WhenInjectedExactlyInto<OverridableClassColorProvider>();
            Bind<FuelConsumptionRepository>().ToSelf().InSingletonScope();
            Bind<FuelConsumptionController, IFuelPredictionProvider>().To<FuelConsumptionController>().InSingletonScope();
            Bind<CarStatusViewModel>().ToSelf();
            Bind<WindInformationViewModel>().ToSelf();
            Bind<ILayoutFactory>().To<LayoutFactory>();

            Bind<RowsDefinitionSettingViewModel>().ToSelf();
            Bind<ILayoutEditorManipulator>().To<LayoutEditorManipulator>();
            Bind<ILayoutConfigurationViewModelFactory>().To<LayoutConfigurationViewModelFactory>();

            Bind<IDefaultColumnsFactory>().To<TimingGridColumnsFactory>().WhenInjectedExactlyInto<SessionsOptionsViewModel>();
            Bind<IColumnDescriptorTemplatesFactory>().To<TimingGridDescriptorsTemplateFactory>().WhenInjectedExactlyInto<SessionsOptionsViewModel>();

            Bind<ISettingMigration>().To<Version04ClassColumnNameBugMigration>();
            Bind<ISettingMigration>().To<Version02AutoHideColumnsMigration>();
            Bind<ISettingMigration>().To<Version03ChangePositionToTemplateColumn>();
            Bind<ISettingMigration>().To<Version05RemoveClassRibbonMigration>();
            Bind<ISettingMigration>().To<Version06DeltaTypo>();
            
            //Disabled as the API is not stable enough and is not updated correctly
            Bind<ISettingMigration>().To<Version07VeColumn>();

            Bind<ConnectorViewModel>().To<ConnectorViewModel>().InSingletonScope();
            Bind<IUomOverride>().To<AccPressureUomOverride>();
            Bind<SessionRemainingCalculator>().ToSelf().InSingletonScope();
            Bind<ISessionInformationProvider>().To<SessionInformationProviderNullObject>();

            Bind<ISessionFuelCalculatorConfiguration>().To<PracticeFuelCalculatorConfiguration>();
            Bind<ISessionFuelCalculatorConfiguration>().To<QualificationFuelCalculatorConfiguration>();
            Bind<ISessionFuelCalculatorConfiguration>().To<RaceFuelCalculatorConfiguration>();

            Bind<IFuelOverviewViewModel>().To<FuelOverviewViewModel>();
            Bind<IFuelStrategyController>().To<FuelStrategyController>();
            Bind<IFuelStrategy>().To<TankToTankStrategy>();
            Bind<IFuelStrategy>().To<PitWindowStrategy>();
            Bind<IFuelStrategy>().To<TimedStintStrategy>();
            Bind<IFuelStrategy>().To<VirtualTankToTankStrategy>();

            Bind<ITemperatureChangeTracker>().To<FromSessionStartTracker>();
            Bind<ITemperatureChangeTracker>().To<Last5MinutesTemperatureTracker>();
            Bind<ITemperatureChangeTracker>().To<FromStintStartTracker>();

            Bind<ISimClassColorProvider>().To<LmUClassColors>();
            Bind<ISimClassColorProvider>().To<AccClassColors>();

            Bind<IDtoMigration<OverallFuelConsumptionHistory>>().To<RemoveEmptySessionsVersion2>();

            Bind<IFuelMarkerViewModel>().To<FuelToAddMarkerViewModel>();
            Bind<IFuelMarkerViewModel>().To<FuelAtRaceEndMarkerViewModel>();

            Bind<IConsumptionMonitor>().To<FuelConsumptionMonitor>();
            Bind<IConsumptionMonitorFactory>().To<ConsumptionMonitorFactory>();

            Bind<IQuantityFormatter>().To<QuantityFormatter>();
        }
    }
}
