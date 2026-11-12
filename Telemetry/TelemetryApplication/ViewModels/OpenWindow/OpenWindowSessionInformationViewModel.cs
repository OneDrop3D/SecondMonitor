namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.OpenWindow
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;
    using SecondMonitor.ViewModels;
    using TelemetryManagement.DTO;

    public class OpenWindowSessionInformationViewModel : AbstractViewModel<SessionInfoDto>, IOpenWindowSessionInformationViewModel
    {
        public DateTime SessionRunDateTime { get; set; }

        public string SessionType { get; set; }

        public string Simulator { get; set; }

        public string TrackName { get; set; }

        public string CarName { get; set; }

        public int NumberOfLaps { get; set; }

        public TimeSpan BestLapTime { get; set; }

        public string PlayerName { get; set; }
        public string SessionName { get; set; }
        public bool IsSessionNameVisible { get; set; }

        public bool IsArchiveIconVisible { get; set; }

        public ICommand ArchiveCommand { get; set; }

        public ICommand SelectThisSessionCommand { get; set; }

        public ICommand OpenSessionFolderCommand { get; set; }

        public ICommand DeleteSessionCommand { get; set; }

        protected override void ApplyModel(SessionInfoDto model)
        {
            SessionRunDateTime = model.SessionRunDateTime;
            SessionType = model.SessionType;
            Simulator = model.Simulator;
            TrackName = model.TrackName;
            CarName = model.CarName;
            IEnumerable<LapSummaryDto> playerLaps = model.LapsSummary.Where(x => x.IsPlayer);
            NumberOfLaps = playerLaps.Count();
            BestLapTime = playerLaps.Where(x => x.Sector1TimeSeconds > 0 && x.Sector2TimeSeconds > 0 && x.Sector3TimeSeconds > 0).Select(x => x.LapTime).DefaultIfEmpty().Min();
            PlayerName = model.PlayerName;
            SessionName = model.SessionCustomName;
            IsSessionNameVisible = !string.IsNullOrEmpty(SessionName);
        }

        public override SessionInfoDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}