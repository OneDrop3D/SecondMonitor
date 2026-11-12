namespace SecondMonitor.Plugins.Remote.ViewModels
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using LiteNetLib;
    using SecondMonitor.Remote.ViewModels;
    using SecondMonitor.ViewModels;

    public interface IServerOverviewViewModel : IViewModel
    {
        List<string> AvailableIps { get; set; }

        bool IsRunning { get; set; }
        double ThrottleInput { get; set; }
        double ClutchInput { get; set; }
        double BrakeInput { get; set; }
        string ConnectedSimulator { get; set; }

        ObservableCollection<IClientViewModel> ConnectedClients { get; }

        INetworkStatsViewModel NetworkStatsViewModel { get; }

        void AddClient(NetPeer netPeer);
        void RemoveClient(NetPeer netPeer);

        void SampleStats(NetStatistics netStatistics);
    }
}