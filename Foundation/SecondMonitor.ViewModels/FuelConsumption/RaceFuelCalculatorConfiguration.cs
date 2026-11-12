namespace SecondMonitor.ViewModels.FuelConsumption
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Summary.FuelConsumption;
    using SessionEvents;
    using Settings;
    using Settings.ViewModel;

    public class RaceFuelCalculatorConfiguration : ISessionFuelCalculatorConfiguration
    {
        private readonly ISessionEventProvider _sessionEventProvider;
        private readonly DisplaySettingsViewModel _displaySettingsViewModel;
        private bool _isAutoOpened;
        private bool _wasClosed;

        public RaceFuelCalculatorConfiguration(ISettingsProvider settingsProvider, ISessionEventProvider sessionEventProvider)
        {
            _sessionEventProvider = sessionEventProvider;
            _displaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
        }

        public SessionType SessionType => SessionType.Race;

        public (int Laps, int Minutes, int ExtraFuelLiters) GetRequiredRunTime(SimulatorDataSet dataSet, OverallFuelConsumptionHistory fuelConsumptionHistory)
        {
            if (!_sessionEventProvider.CurrentSimulatorSettings.IsSessionLengthAvailableBeforeStart)
            {
                return (_displaySettingsViewModel.DefaultRaceLaps, _displaySettingsViewModel.DefaultRaceMinutes, CalculateExtraFuelValue(fuelConsumptionHistory));
            }

            int extraFuel = CalculateExtraFuelValue(fuelConsumptionHistory);
            int totalMinutes = 0;
            int totalLaps = 0;
            switch (dataSet.SessionInfo.SessionLengthType)
            {
                case SessionLengthType.Na:
                    break;
                case SessionLengthType.Laps:
                    totalLaps += dataSet.SessionInfo.TotalNumberOfLaps + _displaySettingsViewModel.LapRaceSessionFuelSetting.ExtraLaps;
                    totalMinutes = _displaySettingsViewModel.LapRaceSessionFuelSetting.ExtraMinutes;
                    break;
                case SessionLengthType.Time:
                    totalMinutes = (int)Math.Ceiling(dataSet.SessionInfo.SessionTimeRemaining / 60) + _displaySettingsViewModel.TimedRaceSessionFuelSettings.ExtraMinutes;
                    totalLaps = 1 + _displaySettingsViewModel.TimedRaceSessionFuelSettings.ExtraLaps;
                    break;
                case SessionLengthType.TimeWithExtraLap:
                    totalMinutes = (int)Math.Ceiling(dataSet.SessionInfo.SessionTimeRemaining / 60) + _displaySettingsViewModel.TimedRaceSessionFuelSettings.ExtraMinutes;
                    totalLaps = 2 + _displaySettingsViewModel.TimedRaceSessionFuelSettings.ExtraLaps;
                    break;
                default:
                    return (1, 0, 0);
            }

            return (totalLaps, totalMinutes, extraFuel);
        }

        private int CalculateExtraFuelValue(OverallFuelConsumptionHistory fuelConsumptionHistory)
        {
            IReadOnlyCollection<SessionFuelConsumptionDto> trackHistory = fuelConsumptionHistory.GetTrackConsumptionHistoryEntries(_sessionEventProvider.LastDataSet.Source, _sessionEventProvider.LastTrackFullName, _sessionEventProvider.CurrentCarName);
            int raceCount = trackHistory.Count(x => !x.IsWetSession && x.SessionKind == SessionType.Race);
            int maxExtraFuel = _displaySettingsViewModel.MaximumExtraFuel;
            int minExtraFuel = _displaySettingsViewModel.MinimumExtraFuel;
            int racesToMinimum = _displaySettingsViewModel.RacesToTrainFuel;

            if (maxExtraFuel <= minExtraFuel)
            {
                return maxExtraFuel;
            }

            int range = maxExtraFuel - minExtraFuel;
            double racesFraction = raceCount / ((double)racesToMinimum);

            int extraFuel = Math.Max((int)(maxExtraFuel - (range * racesFraction)), minExtraFuel);
            return extraFuel;
        }

        public bool ShouldAutoOpen(SimulatorDataSet dataSet)
        {
            if (_isAutoOpened)
            {
                return false;
            }

            _isAutoOpened = dataSet.SessionInfo.SessionType == SessionType.Race && dataSet.SessionInfo.SessionPhase == SessionPhase.Countdown && dataSet.PlayerInfo?.Speed.InKph < 5 && !_isAutoOpened;
            return _isAutoOpened;
        }

        public bool ShouldAutoClose(SimulatorDataSet dataSet)
        {
            return !_wasClosed && _isAutoOpened && (dataSet.SessionInfo.SessionType != SessionType.Race || dataSet.SessionInfo.SessionPhase != SessionPhase.Countdown || dataSet.PlayerInfo?.Speed.InKph > 60);
        }

        public void OnUserClosed()
        {
            _isAutoOpened = true;
            _wasClosed = true;
        }

        public void OnAutoClose()
        {
            _wasClosed = true;
        }

        public void Reset()
        {
            _isAutoOpened = false;
            _wasClosed = false;
        }
    }
}