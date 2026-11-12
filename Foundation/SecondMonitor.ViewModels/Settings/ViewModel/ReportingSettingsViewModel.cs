namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using System.IO;
    using System.Windows.Forms;
    using System.Windows.Input;
    using Contracts.Commands;
    using Model;

    using SecondMonitor.DataModel.Extensions;

    public class ReportingSettingsViewModel : AbstractViewModel
    {
        private string _exportDirectory;
        private int _maximumReports;
        private SessionReportSettingsViewModel _practiceReportSettingsView;
        private SessionReportSettingsViewModel _qualificationReportSettingView;

        private SessionReportSettingsViewModel _raceReportSettingsView;
        private SessionReportSettingsViewModel _warmUpReportSettingsView;
        private int _minimumSessionLength;
        private bool _exportJson;

        public ReportingSettingsViewModel()
        {
            SelectExportDirCommand = new RelayCommand(SelectExportDir);
        }

        public ICommand SelectExportDirCommand { get; }

        internal string ExportDirectory
        {
            get => _exportDirectory;
            set => SetProperty(ref _exportDirectory, value);
        }

        public string ExportDirectoryReplacedSpecialDirs => ReplaceSpecialDirs(ExportDirectory);

        public int MaximumReports
        {
            get => _maximumReports;
            set => SetProperty(ref _maximumReports, value);
        }

        public SessionReportSettingsViewModel PracticeReportSettingsView
        {
            get => _practiceReportSettingsView;
            set => SetProperty(ref _practiceReportSettingsView, value);
        }

        public SessionReportSettingsViewModel QualificationReportSettingView
        {
            get => _qualificationReportSettingView;
            set => SetProperty(ref _qualificationReportSettingView, value);
        }

        public SessionReportSettingsViewModel RaceReportSettingsView
        {
            get => _raceReportSettingsView;
            set => SetProperty(ref _raceReportSettingsView, value);
        }

        public SessionReportSettingsViewModel WarmUpReportSettingsView
        {
            get => _warmUpReportSettingsView;
            set => SetProperty(ref _warmUpReportSettingsView, value);
        }

        public int MinimumSessionLength
        {
            get => _minimumSessionLength;
            set => SetProperty(ref _minimumSessionLength, value);
        }

        public bool ExportJson
        {
            get => _exportJson;
            set => SetProperty(ref _exportJson, value);
        }

        private static string ReplaceSpecialDirs(string path)
        {
            path = DirectoryUtil.PopulateSpecialDirPlaceholders(path);
            return path;
        }

        public void FromModel(ReportingSettings model)
        {
            ExportDirectory = model.ExportDirectory;
            MaximumReports = model.MaximumReports;
            MinimumSessionLength = model.MinimumSessionLength;
            ExportJson = model.ExportJson;
            PracticeReportSettingsView = SessionReportSettingsViewModel.FromModel(model.PracticeReportSettings);
            QualificationReportSettingView = SessionReportSettingsViewModel.FromModel(model.QualificationReportSettings);
            WarmUpReportSettingsView = SessionReportSettingsViewModel.FromModel(model.WarmUpReportSettings);
            RaceReportSettingsView = SessionReportSettingsViewModel.FromModel(model.RaceReportSettings);
            CheckExportDirExistence();
        }

        private void CheckExportDirExistence()
        {
            if (string.IsNullOrWhiteSpace(ExportDirectoryReplacedSpecialDirs))
            {
                return;
            }

            if (Directory.Exists(ExportDirectoryReplacedSpecialDirs))
            {
                return;
            }

            Directory.CreateDirectory(ExportDirectoryReplacedSpecialDirs);
        }

        public ReportingSettings ToModel()
        {
            return new ReportingSettings()
                       {
                           ExportDirectory = ExportDirectory,
                           MaximumReports = MaximumReports,
                           PracticeReportSettings = PracticeReportSettingsView.ToModel(),
                           QualificationReportSettings = QualificationReportSettingView.ToModel(),
                           RaceReportSettings = RaceReportSettingsView.ToModel(),
                           WarmUpReportSettings = WarmUpReportSettingsView.ToModel(),
                           MinimumSessionLength = MinimumSessionLength,
                           ExportJson = ExportJson,
                       };
        }

        private void SelectExportDir()
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.ShowNewFolderButton = true;
                fbd.SelectedPath = ExportDirectoryReplacedSpecialDirs;
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    ExportDirectory = fbd.SelectedPath;
                    NotifyPropertyChanged(nameof(ExportDirectoryReplacedSpecialDirs));
                }
            }
        }
    }
}