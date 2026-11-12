namespace SecondMonitor.Remote.ViewModels
{
    using System;
    using System.Diagnostics;
    using LiteNetLib;
    using Models;
    using SecondMonitor.ViewModels;

    internal class NetworkStatsViewModel : AbstractViewModel<NetStatistics>, INetworkStatsViewModel
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly NetStatSampler _uploadSampler = new NetStatSampler();
        private readonly NetStatSampler _downloadSampler = new NetStatSampler();

        private string _uploadSpeed;
        private string _downloadSpeed;
        private string _uploadDownloadSpeed;
        private long _uploadBytesPerSecond;
        private long _downloadBytesPerSecond;

        long INetworkStatsViewModel.UploadBytesPerSecond => _uploadBytesPerSecond;
        long INetworkStatsViewModel.DownloadBytesPerSecond => _downloadBytesPerSecond;

        public string UploadSpeed
        {
            get => _uploadSpeed;
            set => this.SetProperty(ref _uploadSpeed, value);
        }

        public string DownloadSpeed
        {
            get => _downloadSpeed;
            set => this.SetProperty(ref _downloadSpeed, value);
        }

        public string UploadDownloadSpeed
        {
            get => _uploadDownloadSpeed;
            private set => this.SetProperty(ref _uploadDownloadSpeed, value);
        }

        protected override void ApplyModel(NetStatistics model)
        {
            var elapsedSample = _stopwatch.ElapsedMilliseconds;
            _stopwatch.Restart();
            var uploadSample = model.BytesSent;
            var downloadSample = model.BytesReceived;
            model.Reset();

            _uploadSampler.AddSample(uploadSample, elapsedSample);
            _downloadSampler.AddSample(downloadSample, elapsedSample);

            _uploadBytesPerSecond = _uploadSampler.BytesPerSecond;
            _downloadBytesPerSecond = _downloadSampler.BytesPerSecond;
            DownloadSpeed = _downloadSampler.SpeedPerSecond;
            UploadSpeed = _uploadSampler.SpeedPerSecond;
            UploadDownloadSpeed = $"↑ {UploadSpeed} | ↓ {DownloadSpeed}";
        }

        public override NetStatistics SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}
