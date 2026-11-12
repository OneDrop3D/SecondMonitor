namespace SecondMonitor.Connectors.Remote
{
    using System;
    using ViewModels;
    internal static class ConnectionStateExtensions
    {
        public static ConnectionState Convert(this LiteNetLib.ConnectionState connectionState)
        {
            var convertedConnectionState = connectionState switch
            {
                LiteNetLib.ConnectionState.Connected => ConnectionState.Connected,
                LiteNetLib.ConnectionState.Disconnected => ConnectionState.Disconnected,
                LiteNetLib.ConnectionState.Outgoing => ConnectionState.Connecting,
                LiteNetLib.ConnectionState.ShutdownRequested => ConnectionState.Disconnecting,
                _ => throw new ArgumentOutOfRangeException(nameof(connectionState))
            };

            return convertedConnectionState;
        }
    }
}