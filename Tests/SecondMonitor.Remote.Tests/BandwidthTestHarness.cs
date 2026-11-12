namespace SecondMonitor.Remote.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Connectors.Debug;
    using Connectors.Remote;
    using Foundation.Connectors;
    using LiteNetLib;
    using Ninject;
    using NUnit.Framework;
    using Plugins.Remote;
    using Plugins.Remote.Controllers;
    using Plugins.Remote.ViewModels;
    using PluginsConfiguration.Common.Controller;
    using PluginsConfiguration.Common.DataModel;
    using Remote;
    using SecondMonitor.ViewModels;

    [TestFixture]
    public class BandwidthTestHarness
    {
        private IKernel _serverKernel;
        private IKernel _connectorKernel;
        private IBroadCastServerController _broadCastServerController;
        private IRemoteClient _remoteClient;

        [Test]
        [Ignore("Only run on demand")]
        public async Task BasicMockedDataBandwidthTest()
        {
            IRawTelemetryConnector mockedConnector = new DebugConnector(null);
            mockedConnector.DataLoaded += MockedConnectorDataLoaded;
            mockedConnector.SessionStarted += MockedConnectorSessionStarted;

            if (!mockedConnector.TryConnect())
            {
                Assert.Fail("Mocked Connector has been disabled");
            }

            mockedConnector.StartConnectorLoop();
            var networkStats = _serverKernel.Get<IServerOverviewViewModel>().NetworkStatsViewModel;

            foreach (var delay in Enumerable.Repeat(1, 30).Select(x => Task.Delay(TimeSpan.FromSeconds(x))))
            {
                await delay;
                Console.WriteLine($"Upload bytes per second: {networkStats.UploadBytesPerSecond}");
                Console.WriteLine($"Download bytes per second: {networkStats.DownloadBytesPerSecond}");
            }

            Assert.That(networkStats.UploadBytesPerSecond, Is.GreaterThan(0L));
            Assert.That(networkStats.UploadBytesPerSecond, Is.LessThan(1024L * 15L));

            Assert.That(networkStats.DownloadBytesPerSecond, Is.GreaterThan(0L));
            Assert.That(networkStats.DownloadBytesPerSecond, Is.LessThan(1024 * 15L));
        }

        [OneTimeSetUp]
        public async Task Bootstrap()
        {
            var viewModelsModule = new ViewModelsModule();
            var remoteCommonModule = new RemoteModule();
            _serverKernel = new StandardKernel(viewModelsModule, remoteCommonModule, new RemotePluginModule());
            _connectorKernel = new StandardKernel(viewModelsModule, remoteCommonModule, new RemoteConnectorModule());
            _connectorKernel.Bind<Lazy<IPluginSettingsProvider>>()
                .ToMethod(_ => new Lazy<IPluginSettingsProvider>(() => _connectorKernel.Get<IPluginSettingsProvider>()));
            var serverPluginSettings = new PluginSettingsProvider();
            var connectorPluginSettings = new PluginSettingsProvider();

            _serverKernel.Bind<IPluginSettingsProvider>().ToConstant(serverPluginSettings);
            _connectorKernel.Bind<IPluginSettingsProvider>().ToConstant(connectorPluginSettings);

            _broadCastServerController = _serverKernel.Get<IBroadCastServerController>();
            _remoteClient = _connectorKernel.Get<IRemoteClient>();
            await _broadCastServerController.StartControllerAsync();
            await Task.Delay(TimeSpan.FromSeconds(5));

            if (!_remoteClient.TryConnect())
            {
                throw new Exception("Failed to connect.");
            }

            _remoteClient.StartClientLoop();
        }

        private void MockedConnectorDataLoaded(object sender, DataEventArgs args)
        {
            _broadCastServerController.SendRegularDataPackage(args.Data);
        }

        private void MockedConnectorSessionStarted(object sender, DataEventArgs args)
        {
            _broadCastServerController.SendSessionStartedPackage(args.Data);
        }

        internal class PluginSettingsProvider : IPluginSettingsProvider
        {
            public PluginsConfiguration PluginConfiguration { get; }

            public RemoteConfiguration RemoteConfiguration { get; } = new RemoteConfiguration()
            {
                HostAddress = NetUtils.GetLocalIp(LocalAddrType.IPv4),
                IsFindInLanEnabled = false,
                Port = 52643,
                BroadcastLimitSettings = new BroadcastLimitSettings()
                {
                    IsEnabled = true,
                    MinimumPackageInterval = 30,
                    PlayerTimingPackageInterval = 200,
                    OtherDriversTimingPackageInterval = 1000
                }
            };
            public F12019Configuration F12019Configuration { get; }
            public ForzaMotorsportConfiguration ForzaMotorsportConfiguration { get; }
            public PCars2Configuration PCars2Configurations { get; }
            public Ams2Configuration Ams2Configuration { get; set; }
            public AccConfiguration AccConfiguration { get; }

            public bool TryIsConnectorEnabled(string connectorName, out bool isEnabled)
            {
                throw new NotSupportedException();
            }

            public void SetConnectorEnabled(string connectorName, bool isConnectorEnabled)
            {
                throw new NotSupportedException();
            }

            public bool TryIsPluginEnabled(string pluginName, out bool isEnabled)
            {
                throw new NotSupportedException();
            }

            public void SetPluginEnabled(string pluginName, bool isPluginEnabled)
            {
                throw new NotSupportedException();
            }

            public void SaveConfiguration(PluginsConfiguration pluginsConfiguration)
            {
                throw new NotSupportedException();
            }
        }
    }
}