namespace SecondMonitor.Plugins.Remote.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using System.Windows;
    using DataModel.Snapshot;
    using Foundation.Connectors;
    using NLog;
    using PluginManager.Core;
    using View;
    using ViewModels;

    public class DataBroadcasterPluginController : ISecondMonitorPlugin
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IBroadcastServerConfiguration _broadcastServerConfiguration;
        private readonly IBroadCastServerController _broadCastServerController;
        private readonly IServerOverviewViewModel _serverOverviewViewModel;
        
        private ServerOverviewWindow _serverOverviewWindow;
        private bool _isSending;

        public DataBroadcasterPluginController(
            IServerOverviewViewModel serverOverviewViewModel,
            IBroadCastServerController broadCastServerController,
            IBroadcastServerConfiguration broadcastServerConfiguration)
        {
            _serverOverviewViewModel = serverOverviewViewModel;
            _broadCastServerController = broadCastServerController;
            _broadcastServerConfiguration = broadcastServerConfiguration;
        }

        public PluginsManager PluginManager { get; set; }

        public string PluginName => "Data Broadcast Server";

        public bool IsEnabledByDefault => false;

        public bool IsDaemon => false;

        public async Task RunPlugin()
        {
            InitializeUi();
            await _broadCastServerController.StartControllerAsync();
        }

        public Task OnDisplayMessage(object sender, MessageArgs e)
        {
            if (e.IsDecision)
            {
                if (MessageBox.Show(
                        e.Message,
                        "Message from connector.",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    e.Action();
                }
            }
            else
            {
                MessageBox.Show(e.Message, "Message from connector.", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            return Task.CompletedTask;
        }

        public Task OnSessionStarted(SimulatorDataSet simulatorDataSet)
        {
            _broadCastServerController.SendSessionStartedPackage(simulatorDataSet);
            return Task.CompletedTask;
        }

        public Task OnDataLoaded(SimulatorDataSet simulatorDataSet)
        {
            if (_isSending)
            {
                return Task.CompletedTask;
            }

            _isSending = true;
            _broadCastServerController.SendRegularDataPackage(simulatorDataSet);
            _isSending = false;

            return Task.CompletedTask;
        }

        private void InitializeUi()
        {
            _serverOverviewWindow = new ServerOverviewWindow { DataContext = _serverOverviewViewModel };
            _serverOverviewWindow.Closed += ServerOverviewWindowOnClosed;
            _serverOverviewWindow.Show();
            InitializeAvailableIpList();
        }

        private void InitializeAvailableIpList()
        {
            int portNumber = _broadcastServerConfiguration.Port;
            string strHostName = Dns.GetHostName();
            IPHostEntry ipHostEntry = Dns.GetHostEntry(strHostName);
            _serverOverviewViewModel.AvailableIps = ipHostEntry.AddressList.Where(x => x.AddressFamily == AddressFamily.InterNetwork)
                .Select(ipAddress => $"{ipAddress}:{portNumber}").ToList();
            _serverOverviewViewModel.AvailableIps.ForEach(x => Logger.Info($"Available IP for Server: :{x}"));
        }

        private async void ServerOverviewWindowOnClosed(object sender, EventArgs e)
        {
            try
            {
                await _broadCastServerController.StopControllerAsync();
            }
            finally
            {
                await PluginManager.DeletePlugin(this, new List<Exception>());
            }
        }
    }
}