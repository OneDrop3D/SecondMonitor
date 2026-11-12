namespace SecondMonitor.PluginManager.Core
{
    using System.Threading.Tasks;
    using DataModel.Snapshot;
    using Foundation.Connectors;

    public interface ISecondMonitorPlugin
    {
        PluginsManager PluginManager
        {
            get;
            set;
        }

        bool IsDaemon
        {
            get;
        }

        string PluginName { get; }
        bool IsEnabledByDefault { get; }

        Task RunPlugin();

        Task OnSessionStarted(SimulatorDataSet simulatorDataSet);

        Task OnDataLoaded(SimulatorDataSet simulatorDataSet);
        Task OnDisplayMessage(object sender, MessageArgs e);
    }
}
