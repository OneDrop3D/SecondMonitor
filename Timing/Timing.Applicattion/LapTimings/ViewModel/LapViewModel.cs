namespace SecondMonitor.Timing.Application.LapTimings.ViewModel
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Common.SessionTiming.Drivers.Lap;
    using Common.SessionTiming.Drivers.Lap.PitStop;

    using DataModel.BasicProperties;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels.Settings;
    using SecondMonitor.ViewModels.Settings.ViewModel;

    using ViewModels;

    public class LapViewModel : AbstractViewModel
    {
        private readonly DisplaySettingsViewModel _displaySettingsViewModel;
        private bool _refresh = true;
        private int _startOfLapPosition;
        private string _startOfLapTyres;
        private string _startOfLapTyreWear;
        private string _startOfLapFuelLoad;
        private string _pitInfo;
        private string _bestPersonalDelta;
        private string _bestLapDelta;
        private PitStopInfo _lapPitStop;
        private int _lapNumber;
        private string _sector1;
        private string _sector2;
        private string _sector3;
        private string _lapTime;
        private bool _isSector1PersonalBest;
        private bool _isSector1ClassBest;
        private bool _isSector2PersonalBest;
        private bool _isSector2ClassBest;
        private bool _isSector3PersonalBest;
        private bool _isSector3ClassBest;
        private bool _isSector1SessionBest;
        private bool _isSector2SessionBest;
        private bool _isSector3SessionBest;
        private bool _isLapBestSessionLap;
        private bool _isLapBestPersonalLap;
        private bool _isLapBestClassLap;
        private bool _isLapValid;

        public LapViewModel(ILapInfo lapInfo, ISettingsProvider settingsProvider)
        {
            _displaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
            LapInfo = lapInfo;
            RefreshInfo();
            RefreshStartOfLapInfo();
            TimerMethod(RefreshInfo, () => LapInfo.Driver.Session.DisplaySettingsViewModel.RefreshRate);
        }

        public ILapInfo LapInfo
        {
            get;
            set;
        }

        public int LapNumber
        {
            get => _lapNumber;
            set => SetProperty(ref _lapNumber, value);
        }

        public string Sector1
        {
            get => _sector1;
            set => SetProperty(ref _sector1, value);
        }

        public string Sector2
        {
            get => _sector2;
            set => SetProperty(ref _sector2, value);
        }

        public string Sector3
        {
            get => _sector3;
            set => SetProperty(ref _sector3, value);
        }

        public string LapTime
        {
            get => _lapTime;
            set => SetProperty(ref _lapTime, value);
        }

        public bool IsSector1PersonalBest
        {
            get => _isSector1PersonalBest;
            set => SetProperty(ref _isSector1PersonalBest, value);
        }

        public bool IsSector1ClassBest
        {
            get => _isSector1ClassBest;
            set => SetProperty(ref _isSector1ClassBest, value);
        }

        public bool IsSector2PersonalBest
        {
            get => _isSector2PersonalBest;
            set => SetProperty(ref _isSector2PersonalBest, value);
        }

        public bool IsSector2ClassBest
        {
            get => _isSector2ClassBest;
            set => SetProperty(ref _isSector2ClassBest, value);
        }

        public bool IsSector3PersonalBest
        {
            get => _isSector3PersonalBest;
            set => SetProperty(ref _isSector3PersonalBest, value);
        }

        public bool IsSector3ClassBest
        {
            get => _isSector3ClassBest;
            set => SetProperty(ref _isSector3ClassBest, value);
        }

        public bool IsSector1SessionBest
        {
            get => _isSector1SessionBest;
            set => SetProperty(ref _isSector1SessionBest, value);
        }

        public bool IsSector2SessionBest
        {
            get => _isSector2SessionBest;
            set => SetProperty(ref _isSector2SessionBest, value);
        }

        public bool IsSector3SessionBest
        {
            get => _isSector3SessionBest;
            set => SetProperty(ref _isSector3SessionBest, value);
        }

        public bool IsLapBestSessionLap
        {
            get => _isLapBestSessionLap;
            set => SetProperty(ref _isLapBestSessionLap, value);
        }

        public bool IsLapBestPersonalLap
        {
            get => _isLapBestPersonalLap;
            set => SetProperty(ref _isLapBestPersonalLap, value);
        }

        public bool IsLapBestClassLap
        {
            get => _isLapBestClassLap;
            set => SetProperty(ref _isLapBestClassLap, value);
        }

        public bool IsLapValid
        {
            get => _isLapValid;
            set => SetProperty(ref _isLapValid, value);
        }

        public int StartOfLapPosition
        {
            get => _startOfLapPosition;
            set => SetProperty(ref _startOfLapPosition, value);
        }

        public string StartOfLapTyres
        {
            get => _startOfLapTyres;
            set => SetProperty(ref _startOfLapTyres, value);
        }

        public string StartOfLapTyreWear
        {
            get => _startOfLapTyreWear;
            set => SetProperty(ref _startOfLapTyreWear, value);
        }

        public string StartOfLapFuelLoad
        {
            get => _startOfLapFuelLoad;
            set => SetProperty(ref _startOfLapFuelLoad, value);
        }

        public string PitInfo
        {
            get => _pitInfo;
            set => SetProperty(ref _pitInfo, value);
        }

        public string BestPersonalDelta
        {
            get => _bestPersonalDelta;
            set => SetProperty(ref _bestPersonalDelta, value);
        }

        public string BestLapDelta
        {
            get => _bestLapDelta;
            set => SetProperty(ref _bestLapDelta, value);
        }

        private async void TimerMethod(Action timedAction, Func<int> delayAction)
        {
            while (_refresh)
            {
                bool isCompleted = LapInfo.Completed &&
                                   (LapInfo.Driver.Session.SessionType != SessionType.Race || _lapPitStop?.Completed != false);
                await Task.Delay(delayAction() * 2);
                timedAction();
                if (isCompleted)
                {
                    return;
                }
            }
        }

        public void StopRefresh()
        {
            _refresh = false;
        }

        public void RefreshInfo()
        {
            LapNumber = LapInfo.LapNumber;
            Sector1 = GetSector1();
            Sector2 = GetSector2();
            Sector3 = GetSector3();
            LapTime = GetLapTime();
            IsSector1SessionBest = GetIsSector1SessionBest();
            IsSector2SessionBest = GetIsSector2SessionBest();
            IsSector3SessionBest = GetIsSector3SessionBest();
            IsSector1PersonalBest = GetIsSector1PersonalBest();
            IsSector2PersonalBest = GetIsSector2PersonalBest();
            IsSector3PersonalBest = GetIsSector3PersonalBest();

            IsLapBestSessionLap = GetIsLapBestSessionLap();
            IsLapBestPersonalLap = GetIsLapBestPersonalLap();
            IsLapValid = LapInfo.Valid;

            BestTimesSetViewModel bestTimesForClass = LapInfo.Driver.Session.GetBestTimesForClass(LapInfo.Driver.CarClassId);
            IsSector1ClassBest = GetIsSector1ClassBest(bestTimesForClass);
            IsSector2ClassBest = GetIsSector2ClassBest(bestTimesForClass);
            IsSector3ClassBest = GetIsSector3ClassBest(bestTimesForClass);
            IsLapBestClassLap = GetIsLapClassBest(bestTimesForClass);

            RefreshDeltasToBest(bestTimesForClass);
            RefreshPitInfo();
        }

        private void RefreshPitInfo()
        {
            if (LapInfo.Driver.Session.SessionType != SessionType.Race)
            {
                return;
            }

            if (_lapPitStop == null)
            {
                _lapPitStop = LapInfo.Driver.PitStops.FirstOrDefault(x => x.EntryLap == LapInfo);
            }

            if (_lapPitStop == null)
            {
                return;
            }

            _pitInfo = _lapPitStop.PitInfoFormatted;
        }

        private void RefreshDeltasToBest(BestTimesSetViewModel bestTimesForClass)
        {
            if (!LapInfo.Completed)
            {
                return;
            }

            if (LapInfo.LapTime == TimeSpan.Zero)
            {
                BestLapDelta = "-";
                BestPersonalDelta = "-";
                return;
            }

            if (LapInfo.Driver.BestLap != null)
            {
                BestPersonalDelta = TimeSpanFormatHelper.FormatTimeSpanOnlySeconds(LapInfo.LapTime.Subtract(LapInfo.Driver.BestLap.LapTime), true);
            }

            if (bestTimesForClass?.BestLap != null)
            {
                BestLapDelta = TimeSpanFormatHelper.FormatTimeSpanOnlySeconds(LapInfo.LapTime.Subtract(bestTimesForClass.BestLap.LapTime), true);
            }
        }

        private void RefreshStartOfLapInfo()
        {
            var startOfLapTele = LapInfo?.LapTelemetryInfo?.LapStarSnapshot;
            if (startOfLapTele == null)
            {
                return;
            }

            StartOfLapPosition = startOfLapTele.PlayerData.Position;
            StartOfLapTyres = startOfLapTele.PlayerData.CarInfo.WheelsInfo.FrontLeft.TyreVisualType;
            StartOfLapTyreWear = Math.Round(startOfLapTele.PlayerData.CarInfo.WheelsInfo.AllWheels.Average(x => x.TyreWear.ActualWear) * 100.0, 1).ToString("F1") + "%";
            StartOfLapFuelLoad = startOfLapTele.PlayerData.CarInfo.FuelSystemInfo.FuelRemaining.GetValueInUnits(_displaySettingsViewModel.VolumeUnits).ToString("F1") + Volume.GetUnitSymbol(_displaySettingsViewModel.VolumeUnits);
        }

        private bool GetIsLapBestSessionLap()
        {
            return LapInfo == LapInfo.Driver.Session.SessionBestTimesViewModel.BestLap;
        }

        private bool GetIsLapBestPersonalLap()
        {
            return LapInfo == LapInfo.Driver.BestLap;
        }

        private bool GetIsSector1SessionBest()
        {
            var sector = LapInfo.Sector1;
            return sector != null && sector == LapInfo.Driver.Session.SessionBestTimesViewModel.BestSector1;
        }

        private bool GetIsSector1ClassBest(BestTimesSetViewModel bestTimesSetViewModel)
        {
            var sector = LapInfo.Sector1;
            return sector != null && sector == bestTimesSetViewModel.BestSector1;
        }

        private bool GetIsSector2ClassBest(BestTimesSetViewModel bestTimesSetViewModel)
        {
            var sector = LapInfo.Sector2;
            return sector != null && sector == bestTimesSetViewModel.BestSector2;
        }

        private bool GetIsSector3ClassBest(BestTimesSetViewModel bestTimesSetViewModel)
        {
            var sector = LapInfo.Sector3;
            return sector != null && sector == bestTimesSetViewModel.BestSector3;
        }

        private bool GetIsLapClassBest(BestTimesSetViewModel bestTimesSetViewModel)
        {
            return LapInfo == bestTimesSetViewModel.BestLap;
        }

        private bool GetIsSector2SessionBest()
        {
            var sector = LapInfo.Sector2;
            return sector != null && sector == LapInfo.Driver.Session.SessionBestTimesViewModel.BestSector2;
        }

        private bool GetIsSector3SessionBest()
        {
            var sector = LapInfo.Sector3;
            return sector != null && sector == LapInfo.Driver.Session.SessionBestTimesViewModel.BestSector3;
        }

        private bool GetIsSector1PersonalBest()
        {
            var sector = LapInfo.Sector1;
            return sector != null && sector == LapInfo.Driver.BestSector1;
        }

        private bool GetIsSector2PersonalBest()
        {
            var sector = LapInfo.Sector2;
            return sector != null && sector == LapInfo.Driver.BestSector2;
        }

        private bool GetIsSector3PersonalBest()
        {
            var sector = LapInfo.Sector3;
            return sector != null && sector == LapInfo.Driver.BestSector3;
        }

        private string GetSectorTiming(SectorTiming sectorTiming)
        {
            if (sectorTiming == null)
            {
                return "N/A";
            }

            if (sectorTiming.Lap.Valid)
            {
                return TimeSpanFormatHelper.FormatTimeSpanOnlySeconds(sectorTiming.Duration, false);
            }

            if (!sectorTiming.Lap.Driver.Session.RetrieveAlsoInvalidLaps)
            {
                return "N/A";
            }

            if (sectorTiming.Lap.Driver.Session.SessionType == SessionType.Race || !sectorTiming.Lap.IsPitLap)
            {
                return TimeSpanFormatHelper.FormatTimeSpanOnlySeconds(sectorTiming.Duration, false);
            }

            return "N/A";
        }

        private string GetSector1()
        {
            return GetSectorTiming(LapInfo.Sector1);
        }

        private string GetSector2()
        {
            return GetSectorTiming(LapInfo.Sector2);
        }

        private string GetSector3()
        {
            return GetSectorTiming(LapInfo.Sector3);
        }

        private string GetLapTime()
        {
            if (!LapInfo.Valid && LapInfo.Driver.Session.SessionType != SessionType.Race && (LapInfo.Driver.InPits || LapInfo.IsPitLap))
            {
                return LapInfo.IsPitLap ? "Out Lap" : "Pit Lap";
            }

            return LapInfo.Completed ? TimeSpanFormatHelper.FormatTimeSpan(LapInfo.LapTime) : TimeSpanFormatHelper.FormatTimeSpan(LapInfo.CurrentlyValidProgressTime);
        }
    }
}