namespace SecondMonitor.Remote.ViewModels
{
    using LiteNetLib;
    using SecondMonitor.ViewModels;

    public interface INetworkStatsViewModel : IViewModel<NetStatistics>
    {
        string UploadSpeed { get; }
        string DownloadSpeed { get; }
        string UploadDownloadSpeed { get; }
        internal long UploadBytesPerSecond { get; }
        internal long DownloadBytesPerSecond { get; }
    }
}
