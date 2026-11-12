namespace SecondMonitor.Timing.Application.SessionTiming.Drivers.Presentation.ViewModel
{
    using Common.SessionTiming.Drivers;
    using ViewModels;

    public class SectorRelativeViewModel : AbstractViewModel
    {
        private bool _isLastSector1BetterThanPlayer;
        private bool _isLastSector2BetterThanPlayer;
        private bool _isLastSector3BetterThanPlayer;
        private bool _isSector1InProgress;
        private bool _isSector2InProgress;
        private bool _isSector3InProgress;

        public bool IsLastSector1BetterThanPlayer
        {
            get => _isLastSector1BetterThanPlayer;
            set => SetProperty(ref _isLastSector1BetterThanPlayer, value);
        }

        public bool IsLastSector2BetterThanPlayer
        {
            get => _isLastSector2BetterThanPlayer;
            set => SetProperty(ref _isLastSector2BetterThanPlayer, value);
        }

        public bool IsLastSector3BetterThanPlayer
        {
            get => _isLastSector3BetterThanPlayer;
            set => SetProperty(ref _isLastSector3BetterThanPlayer, value);
        }

        public bool IsSector1InProgress
        {
            get => _isSector1InProgress;
            set => SetProperty(ref _isSector1InProgress, value);
        }

        public bool IsSector2InProgress
        {
            get => _isSector2InProgress;
            set => SetProperty(ref _isSector2InProgress, value);
        }

        public bool IsSector3InProgress
        {
            get => _isSector3InProgress;
            set => SetProperty(ref _isSector3InProgress, value);
        }

        public void Update(DriverTimingViewModel driverTimingViewModel)
        {
            if (driverTimingViewModel.IsPlayer || driverTimingViewModel.DriverTiming == null)
            {
                IsLastSector1BetterThanPlayer = false;
                IsLastSector2BetterThanPlayer = false;
                IsLastSector3BetterThanPlayer = false;
                return;
            }

            CheckSector1(driverTimingViewModel.DriverTiming);
            CheckSector2(driverTimingViewModel.DriverTiming);
            CheckSector3(driverTimingViewModel.DriverTiming);
        }

        private void CheckSector1(DriverTiming driverTiming)
        {
            if (driverTiming.Session.Player == null)
            {
                IsSector1InProgress = true;
                return;
            }

            var sector1 = driverTiming.GetSector1Timing();
            var sector1Player = driverTiming.Session.Player.GetSector1Timing(true);
            if (sector1?.IsFinished != true || sector1Player == null)
            {
                IsSector1InProgress = true;
                return;
            }

            IsSector1InProgress = false;
            IsLastSector1BetterThanPlayer = sector1.Duration < sector1Player.Duration;
        }

        private void CheckSector2(DriverTiming driverTiming)
        {
            if (driverTiming.Session.Player == null)
            {
                IsSector2InProgress = true;
                return;
            }

            var sector2 = driverTiming.GetSector2Timing();
            var sector2Player = driverTiming.Session.Player.GetSector2Timing(true);
            if (sector2?.IsFinished != true || sector2Player == null)
            {
                IsSector2InProgress = true;
                return;
            }

            IsSector2InProgress = false;
            IsLastSector2BetterThanPlayer = sector2.Duration < sector2Player.Duration;
        }

        private void CheckSector3(DriverTiming driverTiming)
        {
            if (driverTiming.Session.Player == null)
            {
                IsSector3InProgress = true;
                return;
            }

            var sector3 = driverTiming.GetSector3Timing();
            var sector3Player = driverTiming.Session.Player.GetSector3Timing(true);
            if (sector3?.IsFinished != true || sector3Player == null)
            {
                IsSector3InProgress = true;
                return;
            }

            IsSector3InProgress = false;
            IsLastSector3BetterThanPlayer = sector3.Duration < sector3Player.Duration;
        }
    }
}