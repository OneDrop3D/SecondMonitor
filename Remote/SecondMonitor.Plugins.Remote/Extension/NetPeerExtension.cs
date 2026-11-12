namespace SecondMonitor.Plugins.Remote.Extension
{
    using LiteNetLib;

    public static class NetPeerExtension
    {
        public static string GetIdentifier(this NetPeer netPeer)
        {
            return $"Host:{netPeer.Address}:{netPeer.Port}";
        }
    }
}