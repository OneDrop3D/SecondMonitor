namespace SecondMonitor.DataModel.BasicProperties
{
    public enum SafetyCarState
    {
        None,
        WaitingForLeader,
        PitsClosed,
        PitsOpenForLeadLapCars,
        PitOpen,
        LastScLap,
        LightsOff
    }
}