namespace SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Drivers;
    using NLog;

    using SecondMonitor.DataModel.BasicProperties;

    using SessionTiming;
    using ViewModels;

    //TODO: This should be split into controller and viewmodel
    public class BestTimesSetViewModel : AbstractViewModel
    {
        public const string AllClassesName = "All Classes";
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly ILapEventProvider _lapEventProvider;
        private SectorTiming _bestSector1;
        private SectorTiming _bestSector2;
        private SectorTiming _bestSector3;
        private ILapInfo _bestLap;
        private ColorDto _classColor;
        private bool _isSetNameShown;

        public BestTimesSetViewModel(ILapEventProvider lapEventProvider,  string setName, string setLabel)
        {
            _lapEventProvider = lapEventProvider;
            SetName = setName;
            SetLabel = setLabel;
            IsAllClassesSet = setName == AllClassesName;
        }

        public bool IsAllClassesSet { get;  }

        public bool IsSetNameShown
        {
            get => _isSetNameShown;
            set => SetProperty(ref _isSetNameShown, value);
        }

        public string SetName { get; }
        public string SetLabel { get; }

        public ColorDto ClassColor
        {
            get => _classColor;
            set => SetProperty(ref _classColor, value);
        }

        public SectorTiming BestSector1
        {
            get => _bestSector1;
            set
            {
                SetProperty(ref _bestSector1, value, NotifyBestSectorChanged);
                NotifyPropertyChanged(nameof(AnySectorFilled));
            }
        }

        public SectorTiming BestSector2
        {
            get => _bestSector2;
            set
            {
                SetProperty(ref _bestSector2, value, NotifyBestSectorChanged);
                NotifyPropertyChanged(nameof(AnySectorFilled));
            }
        }

        public SectorTiming BestSector3
        {
            get => _bestSector3;
            set
            {
                SetProperty(ref _bestSector3, value, NotifyBestSectorChanged);
                NotifyPropertyChanged(nameof(AnySectorFilled));
            }
        }

        public bool AnySectorFilled => true; //BestSector1 != null || BestSector2 != null || BestSector3 != null;

        public ILapInfo BestLap
        {
            get => _bestLap;
            set => SetProperty(ref _bestLap, value,  NotifyBestLapChanged);
        }

        public void CheckAndUpdateBestLap(ILapInfo lap)
        {
            if (lap.Valid && (BestLap == null || BestLap.LapTime == TimeSpan.Zero || (BestLap.LapTime > lap.LapTime && lap.LapTime != TimeSpan.Zero)))
            {
                BestLap = lap;
            }
        }

        public void UpdateSectorBest(SectorTiming sectorTiming)
        {
            switch (sectorTiming.SectorNumber)
            {
                case 1:
                    if ((BestSector1 == null || BestSector1 > sectorTiming) && sectorTiming.Duration > TimeSpan.Zero)
                    {
                        Logger.Info($"New Best Sector 1, By Driver {sectorTiming.Lap.Driver.DriverId}. Sector Time: {sectorTiming.Duration.TotalSeconds} ");
                        Logger.Info(BestSector1 == null ? "Previous Sector Time was NULL" : $"Previous sector time {BestSector1.Duration.TotalSeconds}");
                        BestSector1 = sectorTiming;
                    }

                    break;
                case 2:
                    if ((BestSector2 == null || BestSector2 > sectorTiming) && sectorTiming.Duration > TimeSpan.Zero)
                    {
                        Logger.Info($"New Best Sector 2, By Driver {sectorTiming.Lap.Driver.DriverId}. Sector Time: {sectorTiming.Duration.TotalSeconds} ");
                        Logger.Info(BestSector2 == null ? "Previous Sector Time was NULL" : $"Previous sector time {BestSector2.Duration.TotalSeconds}");
                        BestSector2 = sectorTiming;
                    }

                    break;
                case 3:
                    if ((BestSector3 == null || BestSector3 > sectorTiming) && sectorTiming.Duration > TimeSpan.Zero)
                    {
                        Logger.Info($"New Best Sector 3, By Driver {sectorTiming.Lap.Driver.DriverId}. Sector Time: {sectorTiming.Duration.TotalSeconds} ");
                        Logger.Info(BestSector3 == null ? "Previous Sector Time was NULL" : $"Previous sector time {BestSector3.Duration.TotalSeconds}");
                        BestSector3 = sectorTiming;
                    }

                    break;
            }
        }

        public void FindBestLap(IReadOnlyCollection<DriverTiming> eligibleDrivers)
        {
            BestLap = eligibleDrivers.Select(x => x.BestLap).Where(x => x?.Valid == true && x.Completed && x.LapTime > TimeSpan.Zero).OrderBy(x => x.LapTime).FirstOrDefault();
        }

        public void InvalidateBestSectorForLap(ILapInfo lap, IReadOnlyCollection<DriverTiming> driverEligibleForNewBestSector)
        {
            if (BestSector1 == lap.Sector1)
            {
                BestSector1 = FindBestSector(d => d.BestSector1, driverEligibleForNewBestSector);
            }

            if (BestSector2 == lap.Sector2)
            {
                BestSector2 = FindBestSector(d => d.BestSector2, driverEligibleForNewBestSector);
            }

            if (BestSector3 == lap.Sector3)
            {
                BestSector3 = FindBestSector(d => d.BestSector3, driverEligibleForNewBestSector);
            }
        }

        private SectorTiming FindBestSector(Func<DriverTiming, SectorTiming> sectorTimeFunc, IReadOnlyCollection<DriverTiming> driverEligibleForNewBestSector)
        {
            return driverEligibleForNewBestSector.Where(d => d.IsActive).Select(sectorTimeFunc).Where(s => s != null && s.Duration > TimeSpan.Zero)
                .OrderBy(s => s.Duration).FirstOrDefault();
        }

        private void NotifyBestLapChanged(ILapInfo oldLap, ILapInfo newLap)
        {
            if (IsAllClassesSet)
            {
                _lapEventProvider.NotifyBestLapChanged(this, oldLap, newLap);
            }
            else
            {
                _lapEventProvider.NotifyBestClassLapChanged(this, oldLap, newLap);
            }
        }

        private void NotifyBestSectorChanged(SectorTiming oldSector, SectorTiming newSector)
        {
            if (string.IsNullOrEmpty(SetName))
            {
                _lapEventProvider.NotifyBestSectorChanged(this, oldSector, newSector);
            }
            else
            {
                _lapEventProvider.NotifyBestSectorClassChanged(this, oldSector, newSector);
            }
        }
    }
}