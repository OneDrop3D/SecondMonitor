namespace SecondMonitor.Remote.Debug
{
    using Plugins.Remote;
    internal class MockedBroadcastServerPluginConfiguration : IBroadcastServerConfiguration
    {
        public MockedBroadcastServerPluginConfiguration(int port)
        {
            Port = port;
        }

        public int Port
        {
            get;
        }
    }
}