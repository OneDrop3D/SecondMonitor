namespace SecondMonitor.Timing.Api.Data
{
    using SecondMonitor.DataModel.Snapshot.Systems;

    public class DrsData
    {
        public DrsData(DrsSystem drsSystem)
        {
            DrsStatus = (int)drsSystem.DrsStatus;
            DrsStatusName = drsSystem.DrsStatus.ToString();
            DrsActivationsLeft = drsSystem.DrsActivationLeft;
        }

        public int DrsStatus { get; }

        public string DrsStatusName { get; }

        public int DrsActivationsLeft { get; }
    }
}
