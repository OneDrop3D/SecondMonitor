namespace SecondMonitor.ViewModels.PitBoard
{
    using SecondMonitor.Contracts.PitStatistics;

    public class PitDurationBoardViewModel : AbstractViewModel
    {
        private string _pitStopDuration;
        private string _averagePitStopDuration;
        private string _pitStallDuration;
        private string _averagePitStallDuration;
        private PitInfoBriefDescriptionKind _pitStopDurationStatus;
        private PitInfoBriefDescriptionKind _pitStallDurationStatus;
        public string PitStopDuration
        {
            get => _pitStopDuration;
            set => SetProperty(ref _pitStopDuration, value);
        }

        public string AveragePitStopDuration
        {
            get => _averagePitStopDuration;
            set => SetProperty(ref _averagePitStopDuration, value);
        }

        public string PitStallDuration
        {
            get => _pitStallDuration;
            set => SetProperty(ref _pitStallDuration, value);
        }

        public string AveragePitStallDuration
        {
            get => _averagePitStallDuration;
            set => SetProperty(ref _averagePitStallDuration, value);
        }

        public PitInfoBriefDescriptionKind PitStopDurationStatus
        {
            get => _pitStopDurationStatus;
            set => SetProperty(ref _pitStopDurationStatus, value);
        }

        public PitInfoBriefDescriptionKind PitStallDurationStatus
        {
            get => _pitStallDurationStatus;
            set => SetProperty(ref _pitStallDurationStatus, value);
        }
    }
}
