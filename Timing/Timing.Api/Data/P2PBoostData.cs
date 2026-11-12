namespace SecondMonitor.Timing.Api.Data
{
    using SecondMonitor.DataModel.Snapshot.Systems;

    public class P2PBoostData
    {
        public P2PBoostData(BoostSystem boostSystem)
        {
            BoostStatus = (int)boostSystem.BoostStatus;
            BoostStatusName = boostSystem.BoostStatus.ToString();
            CooldownTimerSeconds = boostSystem.CooldownTimer.TotalSeconds;
            TimeRemainingSeconds = boostSystem.TimeRemaining.TotalSeconds;
            ActivationsRemaining = boostSystem.ActivationsRemaining;
        }

        public int BoostStatus { get; }

        public string BoostStatusName { get; }

        public double CooldownTimerSeconds { get; }

        public double TimeRemainingSeconds { get; }

        public int ActivationsRemaining { get; }
    }
}
