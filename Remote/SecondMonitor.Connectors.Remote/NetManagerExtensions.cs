namespace SecondMonitor.Connectors.Remote
{
    using LiteNetLib;
    using LiteNetLib.Utils;
    using NLog;
    using SecondMonitor.Remote.Models;
    internal static class NetManagerExtensions
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static void TryConnectUsingDiscovery(this NetManager netManager, int port)
        {
            Logger.Info($"Sending Discovery Request: {DatagramPayload.Version4}");

            var netDataWriter = new NetDataWriter();
            netDataWriter.Put(DatagramPayload.Version4);
            netManager.SendBroadcast(netDataWriter, port);
            netDataWriter.Reset();
        }
    }
}