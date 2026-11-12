namespace SecondMonitor.Timing.Api.Data
{
    using SecondMonitor.DataModel.Snapshot.Drivers;

    public class SafetySystemData
    {
        public SafetySystemData(SafetySystemInfo safetySystemInfo)
        {
            IsAvailable = safetySystemInfo.IsAvailable;
            IsActive = safetySystemInfo.IsActive;
            Remark = safetySystemInfo.Remark;
        }

        public bool IsAvailable { get; set; }

        public bool IsActive { get; set; }

        public string Remark { get; set; }
    }
}
