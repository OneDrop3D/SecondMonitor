namespace SecondMonitor.Timing.Application.ViewModel
{
    using System;

    using Contracts.Session;
    using DataModel.Snapshot.Drivers;
    using TimingGrid;

    public class SessionInfoViaTimingViewModelProvider : ISessionInformationProvider
    {
        private TimingDataGridViewModel _timingDataGridViewModel;

        public void RegisterRelay(TimingDataGridViewModel timingDataGridViewModel)
        {
            _timingDataGridViewModel = timingDataGridViewModel;
        }

        public bool IsDriverOnValidLap(IDriverInfo driver)
        {
            return _timingDataGridViewModel?.IsDriverOnValidLap(driver) == true;
        }

        public bool IsDriverLastSectorGreen(IDriverInfo driver, int sectorNumber)
        {
            return _timingDataGridViewModel?.IsDriverLastSectorGreen(driver, sectorNumber) == true;
        }

        public bool IsDriverLastSectorPurple(IDriverInfo driver, int sectorNumber)
        {
            return _timingDataGridViewModel?.IsDriverLastSectorPurple(driver, sectorNumber) == true;
        }

        public bool IsDriverLastClassSectorPurple(IDriverInfo driver, int sectorNumber)
        {
            return _timingDataGridViewModel?.IsDriverLastClassSectorPurple(driver, sectorNumber) == true;
        }

        public bool HasDriverCompletedPitWindowStop(IDriverInfo driverInfo)
        {
            return _timingDataGridViewModel?.HasDriverCompletedPitWindowStop(driverInfo) == true;
        }

        public int? GetPlayerPitStopCount()
        {
            return _timingDataGridViewModel?.GetPlayerPitStopCount();
        }

        public int GetDriverPitStopCount(IDriverInfo driverInfo)
        {
            return _timingDataGridViewModel?.GetDriverPitStopCount(driverInfo) ?? 0;
        }

        public TimeSpan GetDriverCurrentLapTime(IDriverInfo driverInfo)
        {
            return _timingDataGridViewModel.GetDriverCurrentLapTime(driverInfo);
        }
    }
}