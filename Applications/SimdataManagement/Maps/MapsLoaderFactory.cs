namespace SecondMonitor.SimdataManagement
{
    using Contracts.TrackMap;

    using Ninject;

    using ViewModels.Settings;

    public class MapsLoaderFactory : IMapsLoaderFactory
    {
        private readonly ISettingsProvider _settingsProvider;
        private readonly IKernel _kernel;

        public MapsLoaderFactory(ISettingsProvider settingsProvider, IKernel kernel)
        {
            _settingsProvider = settingsProvider;
            _kernel = kernel;
        }

        public IMapsLoader Create()
        {
            return new MapsLoader(_settingsProvider.MapRepositoryPath, _kernel.Get<BuildInTrackMapsProvider>());
        }
    }
}