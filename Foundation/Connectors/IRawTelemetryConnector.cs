namespace SecondMonitor.Foundation.Connectors
{
    using System;
    using System.Threading.Tasks;

    public interface IRawTelemetryConnector
    {
        event EventHandler<DataEventArgs> DataLoaded;
        event EventHandler<EventArgs> ConnectedEvent;
        event EventHandler<EventArgs> Disconnected;
        event EventHandler<DataEventArgs> SessionStarted;
        event EventHandler<MessageArgs> DisplayMessage;

        string Name { get; }

        bool IsConnected { get; }

        bool IsEnabledByDefault { get;  }

        Action ConnectorOptionsDelegate { get; }

        bool TryConnect();

        Task FinnishConnectorAsync();

        void StartConnectorLoop();
    }
}
