namespace SecondMonitor.ViewModels.PitBoard
{
    using System;

    using SecondMonitor.Contracts.Session;
    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels.StatusIcon;

    public class PitReturnBoardViewModel : AbstractViewModel<ReturnPosition>
    {
        private string _returnPosition;
        private string _pitTime;
        private string _gapAhead;
        private string _gapBehind;
        private string _driverAhead;
        private string _driverBehind;
        private StatusIconState _gapAheadStatus;
        private StatusIconState _gapBehindStatus;

        public PitReturnBoardViewModel()
        {
            ReturnPosition = "5";
            PitTime = "30.0";
            GapAhead = "1.2";
            GapBehind = "5.2";
            DriverAhead = "Fero Fero";
            DriverBehind = "Jozo Jozo";
            _gapAheadStatus = StatusIconState.Ok;
            _gapBehindStatus = StatusIconState.Ok;
        }

        public string ReturnPosition
        {
            get => _returnPosition;
            set => SetProperty(ref _returnPosition, value);
        }

        public string PitTime
        {
            get => _pitTime;
            set => SetProperty(ref _pitTime, value);
        }

        public string GapAhead
        {
            get => _gapAhead;
            set => SetProperty(ref _gapAhead, value);
        }

        public string GapBehind
        {
            get => _gapBehind;
            set => SetProperty(ref _gapBehind, value);
        }

        public string DriverAhead
        {
            get => _driverAhead;
            set => SetProperty(ref _driverAhead, value);
        }

        public string DriverBehind
        {
            get => _driverBehind;
            set => SetProperty(ref _driverBehind, value);
        }

        public StatusIconState GapAheadStatus
        {
            get => _gapAheadStatus;
            set => SetProperty(ref _gapAheadStatus, value);
        }

        public StatusIconState GapBehindStatus
        {
            get => _gapBehindStatus;
            set => SetProperty(ref _gapBehindStatus, value);
        }

        protected override void ApplyModel(ReturnPosition model)
        {
            GapAheadStatus = model.GapAhead == null ? StatusIconState.Ok : Math.Abs(model.GapAhead?.TotalSeconds ?? 0) switch
            {
                < 1.5 => StatusIconState.Error,
                < 3 => StatusIconState.Warning,
                _ => StatusIconState.Ok,
            };

            GapBehindStatus = model.GapBehind == null ? StatusIconState.Ok : Math.Abs(model.GapBehind?.TotalSeconds ?? 0) switch
            {
                < 1.5 => StatusIconState.Error,
                < 3 => StatusIconState.Warning,
                _ => StatusIconState.Ok,
            };

            GapAhead = model.GapAhead?.FormatTimeSpanOnlySecondNoMiliseconds(false) ?? string.Empty;
            GapBehind = model.GapBehind?.FormatTimeSpanOnlySecondNoMiliseconds(false) ?? string.Empty;
            DriverAhead = model.DriverInFront?.DriverShortName ?? string.Empty;
            DriverBehind = model.DriverBehind?.DriverShortName ?? string.Empty;
        }

        public override ReturnPosition SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}