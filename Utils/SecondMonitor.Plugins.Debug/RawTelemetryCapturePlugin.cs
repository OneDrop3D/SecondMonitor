namespace SecondMonitor.Plugins.Debug
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using DataModel.Snapshot;
    using Foundation.Connectors;
    using PluginManager.Core;
    using ProtoBuf;
    using SecondMonitor.Debug;
    using ViewModels.Settings;

    public class RawTelemetryCapturePlugin : ISecondMonitorPlugin
    {
        private readonly ISettingsProvider _settingsProvider;

        private FileInfo _currentFile = null;

        public RawTelemetryCapturePlugin(ISettingsProvider settingsProvider)
        {
            _settingsProvider = settingsProvider;
        }

        public PluginsManager PluginManager
        {
            get;
            set;
        }

        public bool IsDaemon => true;
        public string PluginName => "Raw Telemetry Capture";
        public bool IsEnabledByDefault => false;
        public Task RunPlugin()
        {
            return Task.CompletedTask;
        }

        public Task OnDisplayMessage(object sender, MessageArgs messageArgs)
        {
            return Task.CompletedTask;
        }

        public Task OnDataLoaded(SimulatorDataSet simulatorDataSet)
        {
            WriteToFile(simulatorDataSet);
            return Task.CompletedTask;
        }

        public Task OnSessionStarted(SimulatorDataSet simulatorDataSet)
        {
            var secondMonitorTelemetryFileName = Path.Combine(Path.GetTempPath(), $"SecondMonitor-Telemetry-{DateTime.Now.ToString("yyyyMMdd-HHmmsszz")}.smtmp");
            var tempFileName = Path.GetTempFileName();
            var tempFileInfo = new FileInfo(tempFileName);
            tempFileInfo.MoveTo(secondMonitorTelemetryFileName);
            WriteToFile(simulatorDataSet, tempFileInfo);
            return Task.CompletedTask;
        }

        private void WriteToFile(SimulatorDataSet data, FileInfo newFile = null)
        {
            if (_currentFile == null && newFile == null)
            {
                return;
            }

            var packet = new DataPacket()
            {
                Source = data.Source,
                InputInfo = data.InputInfo,
                SessionInfo = data.SessionInfo,
                DriversInfo = data.DriversInfo,
                PlayerInfo = data.PlayerInfo,
                LeaderInfo = data.LeaderInfo,
                SimulatorSourceInfo = data.SimulatorSourceInfo
            };

            var fileInfoToUse = newFile ?? _currentFile;

            using var file = fileInfoToUse.Open(FileMode.Append, FileAccess.Write);
            if (newFile != null)
            {
                // Write Logging interval
                var header = new DataHeader()
                {
                    UpdateInterval = _settingsProvider.DisplaySettingsViewModel.TelemetrySettingsViewModel.LoggingInterval
                };
                Serializer.SerializeWithLengthPrefix(file, header, PrefixStyle.Base128);

                _currentFile = newFile;
            }

            lock (_currentFile)
            {
                Serializer.SerializeWithLengthPrefix(file, packet, PrefixStyle.Fixed32);
            }
        }
    }
}