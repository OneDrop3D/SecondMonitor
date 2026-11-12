namespace SecondMonitor.Rating.Application.Championship.Operations
{
    using System.Collections.Generic;
    using System.Linq;

    using SecondMonitor.Rating.Application.Championship.Operations.PointsStrategy;
    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.Rating.Common.DataModel.Championship.Events;

    public class TeamChampionshipManipulator : ISubChampionshipManipulator
    {
        private readonly IScoringStrategyFactory _scoringStrategyFactory;

        public TeamChampionshipManipulator(IScoringStrategyFactory scoringStrategyFactory)
        {
            _scoringStrategyFactory = scoringStrategyFactory;
        }

        public void SetPlayer(ChampionshipDto championship, DriverDto player)
        {
            if (!championship.IsTeamScoringEnabled)
            {
                return;
            }

            List<TeamDto> allTeams = championship.TeamsChampionshipDtos.SelectMany(x => x.TeamDtos).ToList();

            bool isPlayerTeamKnown = !string.IsNullOrWhiteSpace(player.TeamName);
            allTeams.ForEach(x => x.IsPlayer = false);
            if (isPlayerTeamKnown)
            {
               championship.GetOrCreateTeamChampionship(new ClassIdentifier(player.LastClassName, player.LastClassId)).GetOrCreateTeam(player.TeamName).IsPlayer = true;
               if (championship.IsTrackAllClassesEnabled)
               {
                   championship.GetOrCreateTeamChampionship(championship.GetOverviewClassIdentifiers()).GetOrCreateTeam(player.TeamName).IsPlayer = true;
               }
            }
        }

        public void AddResults(ChampionshipDto championshipDto, SessionResultDto resultDto, ScoringDto scoring, ClassIdentifier classIdentifier)
        {
            AddResults(championshipDto, resultDto, resultDto, scoring, classIdentifier);
        }

        public void AddResults(ChampionshipDto championshipDto, SessionResultDto resultDto, IHaveTeamResults teamResults, ScoringDto scoring, ClassIdentifier classIdentifier)
        {
            teamResults.TeamSessionResult.Clear();
            if (!championshipDto.IsTeamScoringEnabled)
            {
                return;
            }

            IScoringStrategy scoringStrategy = _scoringStrategyFactory.Create(championshipDto.ManufacturerScoringKind);

            List<RepresentantFinishPosition> representantFinishPositions =
                resultDto.GetDriverSessionResultForClass(classIdentifier).Select(x => new RepresentantFinishPosition(x.TeamName, string.IsNullOrEmpty(classIdentifier.ClassId) ? x.FinishPosition : x.ClassFinishPosition)).ToList();
            List<ParticipantPoints> sessionPoints = scoringStrategy.ApplyScoring(representantFinishPositions, scoring);

            if (sessionPoints.Count < 2)
            {
                return;
            }

            int currentPosition = 1;

            TeamsChampionshipDto teamsChampionshipDto = championshipDto.GetOrCreateTeamChampionship(classIdentifier);

            // AddResults for manufacturers in event
            foreach (ParticipantPoints participantPoints in sessionPoints.OrderByDescending(x => x.Points))
            {
                TeamDto teamDto = teamsChampionshipDto.GetOrCreateTeam(participantPoints.Name);
                TeamSessionResultDto teamSessionResult = new()
                {
                    IsPlayer = teamDto.IsPlayer,
                    FinishPosition = currentPosition,
                    TeamName = teamDto.TeamName,
                    PointsGain = participantPoints.Points,
                    TotalPoints = teamDto.TotalPoints + participantPoints.Points,
                    BeforeEventPosition = teamDto.Position,
                };

                teamResults.TeamSessionResult.Add(teamSessionResult);
                currentPosition++;
            }

            // AddResults for manufacturers not in Event
            foreach (TeamDto teamDto in teamsChampionshipDto.TeamDtos.Where(x =>
                         teamResults.TeamSessionResult.All(y => y.TeamName != x.TeamName)))
            {
                TeamSessionResultDto teamSessionResult = new TeamSessionResultDto()
                {
                    IsPlayer = teamDto.IsPlayer,
                    FinishPosition = currentPosition,
                    TeamName = teamDto.TeamName,
                    PointsGain = 0,
                    TotalPoints = teamDto.TotalPoints,
                    BeforeEventPosition = teamDto.Position,
                    SkippedEvent = true,
                };

                teamResults.TeamSessionResult.Add(teamSessionResult);
                currentPosition++;
            }

            TeamsResultComparer comparer = new(championshipDto, resultDto);
            List<TeamSessionResultDto> teamsOrdered = teamResults.TeamSessionResult.OrderBy(x => x, comparer).ToList();
            for (int i = 0; i < teamsOrdered.Count; i++)
            {
                teamsOrdered[i].AfterEventPosition = i + 1;
            }
        }

        public void CommitResults(ChampionshipDto championshipDto, SessionResultDto resultDto, ClassIdentifier classIdentifier)
        {
            CommitResults(championshipDto, (IHaveTeamResults)resultDto, classIdentifier);
        }

        public void CommitResults(ChampionshipDto championshipDto, IHaveTeamResults teamResults, ClassIdentifier classIdentifier)
        {
            TeamsChampionshipDto teamsChampionshipDto = championshipDto.GetOrCreateTeamChampionship(classIdentifier);
            foreach (TeamSessionResultDto teamSessionResultDto in teamResults.TeamSessionResult)
            {
                TeamDto teamDto = teamsChampionshipDto.GetOrCreateTeam(teamSessionResultDto.TeamName);
                teamDto.TotalPoints = teamSessionResultDto.TotalPoints;
                teamDto.Position = teamSessionResultDto.AfterEventPosition;
            }
        }

        public void ResetChampionship(ChampionshipDto championshipDto)
        {
            championshipDto.TeamsChampionshipDtos.Clear();
        }

        public void RevertLastResults(ChampionshipDto championshipDto, ClassIdentifier classIdentifier)
        {
            SessionResultDto resultDto = championshipDto.GetLastSessionWithResults().SessionDto.SessionResult;
            TeamsChampionshipDto teamsChampionshipDto = championshipDto.GetOrCreateTeamChampionship(classIdentifier);
            foreach (TeamSessionResultDto teamSessionResultDto in resultDto.GetTeamResults(classIdentifier))
            {
                TeamDto teamDto = teamsChampionshipDto.GetOrCreateTeam(teamSessionResultDto.TeamName);
                teamDto.TotalPoints -= teamSessionResultDto.PointsGain;
                teamDto.Position = teamSessionResultDto.BeforeEventPosition;
            }
        }
    }
}
