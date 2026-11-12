namespace SecondMonitor.Contracts.PitStatistics
{
    public enum PitInfoBriefDescriptionKind
    {
        None,
        PitStopFasterThanPlayer,
        PitStopSlowerThanPlayer,
        TyresOlderThanPlayer,
        TyresYoungerThanPlayers,
        PitEntry,
        InPits,
        PitExit
    }
}