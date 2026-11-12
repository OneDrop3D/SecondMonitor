namespace SecondMonitor.Connectors.Remote
{
    using System.Collections.Generic;
    using System.Net;
    public interface IRemoteConnectorConfiguration
    {
        int? FindInLanPort { get; }
        IList<IPEndPoint> ServerPeers { get; }

        bool IsMultiClientEnabled { get; }
    }
}