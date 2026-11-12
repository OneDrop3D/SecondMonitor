namespace SecondMonitor.SimdataManagement
{
    using Contracts.SimSettings;
    using Contracts.TrackMap;
    using DriverPresentation;
    using Ninject.Modules;
    using RaceSuggestion;
    using SimSettings;
    using SimSettings.IdealPressure;
    using WheelDiameterWizard;

    public class SimDataManagementModule : NinjectModule
    {
        public override void Load()
        {
            Bind<ICarSpecificationProvider>().To<CarSpecificationProvider>().InSingletonScope();
            Bind<IWheelDiameterWizardController>().To<WheelDiameterWizardController>();
            Bind<ITrackDtoManipulator>().To<TrackDtoManipulator>();
            Bind<TrackMapFromTelemetryFactory>().ToSelf();
            Bind<IDriverPresentationsManager, DriverPresentationsManager>().To<DriverPresentationsManager>().InSingletonScope();
            Bind<DriverPresentationsLoader>().ToSelf().InSingletonScope();
            Bind<IRaceSuggestionController>().To<RaceSuggestionController>();
            Bind<IIdealPressureByTemperatureCalculator>().To<LinearIdealPressureCalculator>();
            Bind<IMapsLoaderFactory>().To<MapsLoaderFactory>();
        }
    }
}