namespace SecondMonitor.Timing.Application.ReportCreation.ViewModel
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using DataModel.BasicProperties;
    using DataModel.Summary;
    using Newtonsoft.Json;
    using NLog;
    using ViewModels.Settings.ViewModel;
    using XslxExport;

    public class ReportsController
    {
        private const string ReportNamePrefix = "";

        private const string ReportDirectoryName = "Reports";
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly DisplaySettingsViewModel _settingsView;

        public ReportsController(DisplaySettingsViewModel settingsView)
        {
            _settingsView = settingsView;
        }

        public void CreateReport(SessionSummary sessionSummary, string fileSuffix)
        {
            if (!ShouldBeExported(sessionSummary))
            {
                return;
            }

            try
            {
                string reportName = GetReportName(sessionSummary, fileSuffix);
                SessionSummaryXslxExporter sessionSummaryXslxExporter = CreateSessionSummaryExporter();
                string fullReportPath = Path.Combine(
                    GetReportDirectory(),
                    reportName);
                /*#if DEBUG
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(SessionSummary));
                string xmlFilePath = fullReportPath + ".xml";
                using (FileStream file = File.Exists(xmlFilePath) ? File.Open(xmlFilePath, FileMode.Truncate) : File.Create(xmlFilePath))
                {
                    xmlSerializer.Serialize(file, sessionSummary);
                }
                #endif*/

                sessionSummaryXslxExporter.ExportSessionSummary(sessionSummary, fullReportPath);

                if (_settingsView.ReportingSettingsViewModel.ExportJson)
                {
                    ExportJson(fullReportPath, sessionSummary);
                }

                OpenReportIfEnabled(sessionSummary, fullReportPath);
                CheckAndDeleteIfMaximumReportsExceeded();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unable to export session info");
            }
        }

        private void ExportJson(string reportName, SessionSummary sessionSummary)
        {
            reportName += ".json";
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
            };

            settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            Logger.Info($"Exporting Json report file {reportName}");
            File.WriteAllText(reportName, JsonConvert.SerializeObject(sessionSummary, settings));
        }

        public void OpenLastReport()
        {
            DirectoryInfo di = new DirectoryInfo(GetReportDirectory());
            FileInfo fileToOpen = di.GetFiles("*.xlsx").OrderBy(x => x.CreationTime).LastOrDefault();
            if (fileToOpen == null)
            {
                return;
            }

            OpenReport(fileToOpen.FullName);
        }

        public void OpenReportsFolder()
        {
            string reportDirectory = GetReportDirectory();
            Task.Run(
                () =>
                    {
                        Process.Start(new ProcessStartInfo("explorer.exe", reportDirectory) { UseShellExecute = true });
                    });
        }

        private SessionSummaryXslxExporter CreateSessionSummaryExporter()
        {
            SessionSummaryXslxExporter sessionSummaryXslxExporter = new SessionSummaryXslxExporter { VelocityUnits = _settingsView.VelocityUnits, DistanceUnits = _settingsView.DistanceUnits, VolumeUnits = _settingsView.VolumeUnits };
            return sessionSummaryXslxExporter;
        }

        private bool ShouldBeExported(SessionSummary sessionSummary)
        {
            if (_settingsView == null)
            {
                return false;
            }

            if (sessionSummary.Drivers.Count == 0)
            {
                Logger.Info("Not Exporting Report - No Drivers");
            }

            if (sessionSummary.SessionRunDuration.TotalMinutes < _settingsView.ReportingSettingsViewModel.MinimumSessionLength &&
                sessionSummary.SessionType != SessionType.Race)
            {
                Logger.Info("Not Exporting Report - Session Not Long Enough");
                return false;
            }

            switch (sessionSummary.SessionType)
            {
                case SessionType.Practice:
                    return _settingsView.ReportingSettingsViewModel.PracticeReportSettingsView.Export;
                case SessionType.Qualification:
                    return _settingsView.ReportingSettingsViewModel.QualificationReportSettingView.Export;
                case SessionType.WarmUp:
                    return _settingsView.ReportingSettingsViewModel.WarmUpReportSettingsView.Export;
                case SessionType.Race:
                    return _settingsView.ReportingSettingsViewModel.RaceReportSettingsView.Export;
                default:
                    return false;
            }
        }

        private void OpenReportIfEnabled(SessionSummary sessionSummary, string reportPath)
        {
            bool openReport = false;
            switch (sessionSummary.SessionType)
            {
                case SessionType.Na:
                    break;
                case SessionType.Practice:
                    openReport = _settingsView.ReportingSettingsViewModel.PracticeReportSettingsView.AutoOpen;
                    break;
                case SessionType.Qualification:
                    openReport = _settingsView.ReportingSettingsViewModel.QualificationReportSettingView.AutoOpen;
                    break;
                case SessionType.WarmUp:
                    openReport = _settingsView.ReportingSettingsViewModel.WarmUpReportSettingsView.AutoOpen;
                    break;
                case SessionType.Race:
                    openReport = _settingsView.ReportingSettingsViewModel.RaceReportSettingsView.AutoOpen;
                    break;
            }

            if (!openReport)
            {
                return;
            }

            OpenReport(reportPath);
        }

        private void OpenReport(string reportPath)
        {
            Task.Run(() => Process.Start("explorer.exe", reportPath));
        }

        private string GetReportName(SessionSummary sessionSummary, string fileSuffix)
        {
            StringBuilder reportName = new StringBuilder(ReportNamePrefix);

            reportName.Append(DateTime.Now.ToString("s") + "_").Replace(":", "-");
            reportName.Append(sessionSummary.Simulator.Replace(" ", "_") + "_");
            var player = sessionSummary.Drivers.FirstOrDefault(x => x.IsPlayer);
            if (player != null)
            {
                reportName.Append(player.ClassName.Replace(" ", string.Empty) + "_");
            }

            if (sessionSummary.IsMultiClass)
            {
                reportName.Append("Multiclass_");
            }

            reportName.Append(sessionSummary.TrackInfo.TrackName + "_");
            reportName.Append(sessionSummary.SessionType);

            if (!string.IsNullOrEmpty(fileSuffix))
            {
                reportName.Append(fileSuffix);
            }

            reportName.Append(".xlsx");
            string reportNameString = reportName.ToString();
            reportNameString = string.Join("_", reportNameString.Split(Path.GetInvalidFileNameChars()));
            return reportNameString;
        }

        private void CheckAndDeleteIfMaximumReportsExceeded()
        {
            DirectoryInfo di = new DirectoryInfo(GetReportDirectory());
            FileInfo[] files = di.GetFiles(ReportNamePrefix + "*.xlsx").Concat(di.GetFiles(ReportNamePrefix + "*.json")).OrderBy(f => f.CreationTimeUtc).ToArray();
            if (files.Length <= _settingsView.ReportingSettingsViewModel.MaximumReports)
            {
                return;
            }

            int filesToDelete = files.Length - _settingsView.ReportingSettingsViewModel.MaximumReports;
            for (int i = 0; i < filesToDelete; i++)
            {
                files[i].Delete();
            }
        }

        private string GetReportDirectory()
        {
            string directoryPath = Path.Combine(_settingsView.ReportingSettingsViewModel.ExportDirectoryReplacedSpecialDirs, ReportDirectoryName);
            Directory.CreateDirectory(directoryPath);
            return directoryPath;
        }
    }
}