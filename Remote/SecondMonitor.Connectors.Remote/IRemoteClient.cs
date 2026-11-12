namespace SecondMonitor.Connectors.Remote
{
    using System;
    using System.Net;
    using DataModel.Snapshot;

    public interface IRemoteClient
    {
        event EventHandler Disconnected;
        event EventHandler<SimulatorDataSet> SessionStarted;
        event EventHandler<SimulatorDataSet> DataLoaded;
        bool TryConnect();

        void StartClientLoop();
        void SwitchToPeer(IPEndPoint endPoint);
        void ConnectToPeer(IPEndPoint endPoint);
    }
}
