namespace SecondMonitor.Timing.Application.SessionTiming.Drivers.Presentation.ViewModel
{
    using System;
    using Common.SessionTiming;
    using Common.SessionTiming.Drivers;
    using DataModel.BasicProperties;
    using ViewModels.Settings;
    using ViewModels.Settings.ViewModel;

    public class GapClosureInformationProvider : IGapClosureInformationProvider
    {
        private readonly DisplaySettingsViewModel _displaySettingsViewModel;
        private DateTime _lastEstimation;

        public GapClosureInformationProvider(ISettingsProvider settingsProvider)
        {
            _displaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
        }

        public int? LapsToClose { get; private set; }

        public void GenerateClosureInformation(DriverTiming driverTiming)
        {
            if (!IsSessionValid(driverTiming))
            {
                LapsToClose = null;
            }

            if ((DateTime.UtcNow - _lastEstimation).TotalSeconds < 10)
            {
                return;
            }

            _lastEstimation = DateTime.UtcNow;

            DriverTiming player = driverTiming.Session.Player;
            DriverTiming driverInFont;
            DriverTiming driverInBack;

            if (player == null)
            {
                return;
            }

            if (player.Position < driverTiming.Position)
            {
                driverInFont = player;
                driverInBack = driverTiming;
            }
            else
            {
                driverInFont = driverTiming;
                driverInBack = player;
            }

            if (!IsDriverClosing(driverInFont, driverInBack))
            {
                LapsToClose = null;
                return;
            }

            if (_displaySettingsViewModel.GapComputeOnlyOnLapStart && player.CurrentLap?.CurrentlyValidProgressTime.TotalSeconds >= 10)
            {
                return;
            }

            double gapToClose = Math.Abs(driverTiming.GapToPlayerAbsolute.TotalSeconds) - 1;
            if (gapToClose < 0)
            {
                LapsToClose = null;
                return;
            }

            int positionDifference = driverInBack.Position - driverInFont.Position;
            if (positionDifference > _displaySettingsViewModel.GapClosingMaximumPositionDifference)
            {
                LapsToClose = null;
                return;
            }

            gapToClose += (positionDifference - 1) * _displaySettingsViewModel.GapClosingOvertakeTimeLost;

            double paceDifference = driverInFont.Pace.TotalSeconds - driverInBack.Pace.TotalSeconds;

            LapsToClose = (int)Math.Ceiling(gapToClose / paceDifference);
        }

        private bool IsDriverClosing(DriverTiming driverInFront, DriverTiming driverInBack)
        {
            if (driverInFront.IsLappingPlayer || driverInFront.IsLappedByPlayer || driverInBack.IsLappingPlayer || driverInBack.IsLappedByPlayer)
            {
                return false;
            }

            return driverInFront.Pace > driverInBack.Pace;
        }

        private bool IsSessionValid(DriverTiming driverTiming)
        {
            if (!_displaySettingsViewModel.PaceContainsLapsToCatchEstimation)
            {
                return false;
            }

            ISessionInfo sessionInfo = driverTiming.Session;
            if (driverTiming.IsPlayer || sessionInfo.Player == null || sessionInfo.SessionType != SessionType.Race || sessionInfo.LastSet == null)
            {
                return false;
            }

            if (driverTiming.Pace == TimeSpan.Zero || sessionInfo.Player.Pace == TimeSpan.Zero)
            {
                return false;
            }

            if (sessionInfo.LastSet.SessionInfo.SessionTime.TotalMinutes < _displaySettingsViewModel.GapClosingInitialTime)
            {
                return false;
            }

            return true;
        }
    }
}