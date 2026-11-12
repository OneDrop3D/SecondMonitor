namespace SecondMonitor.DataModel.Snapshot.Drivers
{
    public static class DriverFinishStatusExtension
    {
        public static bool IsFinishedRaceStatus(this DriverFinishStatus driverFinishStatus)
        {
            return driverFinishStatus == DriverFinishStatus.Dnf || driverFinishStatus == DriverFinishStatus.Dq || driverFinishStatus == DriverFinishStatus.Finished;
        }
    }
}
