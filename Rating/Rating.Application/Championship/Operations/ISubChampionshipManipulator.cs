namespace SecondMonitor.Rating.Application.Championship.Operations
{
    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.Rating.Common.DataModel.Championship.Events;

    public interface ISubChampionshipManipulator
    {
        void SetPlayer(ChampionshipDto championship, DriverDto player);

        void AddResults(ChampionshipDto championshipDto, SessionResultDto resultDto, ScoringDto scoring, ClassIdentifier classIdentifier);

        void CommitResults(ChampionshipDto championshipDto, SessionResultDto resultDto, ClassIdentifier classIdentifier);

        void ResetChampionship(ChampionshipDto championshipDto);

        void RevertLastResults(ChampionshipDto championshipDto, ClassIdentifier classIdentifier);
    }
}
