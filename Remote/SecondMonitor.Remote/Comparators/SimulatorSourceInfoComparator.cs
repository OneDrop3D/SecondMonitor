namespace SecondMonitor.Remote.Comparators
{
    using DataModel.Snapshot;

    public class SimulatorSourceInfoComparator : ISimulatorSourceInfoComparator
    {
        public bool AreEqual(SimulatorSourceInfo x, SimulatorSourceInfo y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return x.Equals(y);
        }
    }
}