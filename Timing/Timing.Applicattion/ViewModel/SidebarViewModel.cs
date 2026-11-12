namespace SecondMonitor.Timing.Application.ViewModel
{
    using System.Windows.Input;
    using Rating.Application.Championship.ViewModels.IconState;
    using ViewModels;

    public class SidebarViewModel : AbstractViewModel
    {
        public const string ViewModelLayoutName = "Buttons side bar";

        private bool _isOpenCarSettingsCommandEnable;
        private ICommand _openCurrentTelemetrySession;
        private ICommand _openCarSettingsCommand;
        private ICommand _openChampionshipWindowCommand;
        private ChampionshipIconStateViewModel _championshipIconStateViewModel;
        private ICommand _openLastReportCommand;
        private ICommand _openReportFolderCommand;
        private ICommand _openSettingsCommand;
        private ICommand _openRandomSuggestionsCommand;
        private ICommand _openTelemetryCheckCommand;

        public ICommand OpenTelemetryCheckCommand
        {
            get => _openTelemetryCheckCommand;
            set => SetProperty(ref _openTelemetryCheckCommand, value);
        }

        public ICommand OpenCurrentTelemetrySession
        {
            get => _openCurrentTelemetrySession;
            set => SetProperty(ref _openCurrentTelemetrySession, value);
        }

        public ICommand OpenCarSettingsCommand
        {
            get => _openCarSettingsCommand;
            set => SetProperty(ref _openCarSettingsCommand, value);
        }

        public ICommand OpenChampionshipWindowCommand
        {
            get => _openChampionshipWindowCommand;
            set => SetProperty(ref _openChampionshipWindowCommand, value);
        }

        public ChampionshipIconStateViewModel ChampionshipIconStateViewModel
        {
            get => _championshipIconStateViewModel;
            set => SetProperty(ref _championshipIconStateViewModel, value);
        }

        public bool IsOpenCarSettingsCommandEnable
        {
            get => _isOpenCarSettingsCommandEnable;
            set => SetProperty(ref _isOpenCarSettingsCommandEnable, value);
        }

        public ICommand OpenLastReportCommand
        {
            get => _openLastReportCommand;
            set => SetProperty(ref _openLastReportCommand, value);
        }

        public ICommand OpenReportFolderCommand
        {
            get => _openReportFolderCommand;
            set => SetProperty(ref _openReportFolderCommand, value);
        }

        public ICommand OpenSettingsCommand
        {
            get => _openSettingsCommand;
            set => SetProperty(ref _openSettingsCommand, value);
        }

        public ICommand OpenRandomSuggestionsCommand
        {
            get => _openRandomSuggestionsCommand;
            set => SetProperty(ref _openRandomSuggestionsCommand, value);
        }
    }
}