namespace SecondMonitor.Rating.Application.Championship.Operations
{
    using Common.DataModel.Championship;
    using DataModel.Snapshot;

    using SecondMonitor.Rating.Application.Championship.ViewModels.Overview.ModifyResults;

    using Timing.Common.SessionTiming.Drivers.Lap;

    public interface IChampionshipManipulator
    {
        void StartChampionship(ChampionshipDto championship, SimulatorDataSet dataSet);
        void StartNextEvent(ChampionshipDto championship, SimulatorDataSet dataSet);
        void AddResultsForCurrentSession(ChampionshipDto championship, SimulatorDataSet dataSet, ILapInfo bestLap, double completionPercentage);
        void UpdateDriversProperties(ChampionshipDto championship, SimulatorDataSet dataSet);
        void UpdateAiDriversNames(ChampionshipDto championship, SimulatorDataSet dataSet);

        void DnfPlayerAndCommitLastSessionResults(ChampionshipDto championship);
        void CommitLastSessionResultsAndAdvance(ChampionshipDto championship);

        void CommitLastSessionResult(ChampionshipDto championship);

        void ReRollMysteryProperties(ChampionshipDto championship);
        void ResetChampionship(ChampionshipDto championship, bool shuffleCalendar);

        void RevertLastSessionResult(ChampionshipDto championship);

        void AmendLastResult(ChampionshipDto championship, AmendedSessionResult amendedResult);
    }
}