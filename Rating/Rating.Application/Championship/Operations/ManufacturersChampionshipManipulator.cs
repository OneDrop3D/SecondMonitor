namespace SecondMonitor.Rating.Application.Championship.Operations
{
    using System.Collections.Generic;
    using System.Linq;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.Rating.Application.Championship.Manufacturers;
    using SecondMonitor.Rating.Application.Championship.Operations.PointsStrategy;
    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.Rating.Common.DataModel.Championship.Events;

    public class ManufacturersChampionshipManipulator : ISubChampionshipManipulator
    {
        private readonly IManufacturerProvider _manufacturerProvider;
        private readonly IScoringStrategyFactory _scoringStrategyFactory;

        public ManufacturersChampionshipManipulator(IManufacturerProvider manufacturerProvider, IScoringStrategyFactory scoringStrategyFactory)
        {
            _manufacturerProvider = manufacturerProvider;
            _scoringStrategyFactory = scoringStrategyFactory;
        }

        public void SetPlayer(ChampionshipDto championship, DriverDto player)
        {
            if (!championship.IsManufacturerScoringEnabled)
            {
                return;
            }

            championship.ManufacturersChampionshipDtos.SelectMany(x => x.Manufacturers).ForEach(x => x.IsPlayer = false);
            bool isPlayerManufacturerKnown = _manufacturerProvider.TryGetManufacturer(player.LastCarName, out Manufacturer playerManufacturer);
            if (isPlayerManufacturerKnown)
            {
                championship.GetOrCreateManufacturerChampionship(new ClassIdentifier(player.LastClassName, player.LastClassId)).GetOrCreateManufacturer(playerManufacturer.Name).IsPlayer = true;

                if (championship.IsTrackAllClassesEnabled)
                {
                    championship.GetOrCreateManufacturerChampionship(championship.GetOverviewClassIdentifiers()).GetOrCreateManufacturer(playerManufacturer.Name).IsPlayer = true;
                }
            }
        }

        public void AddResults(ChampionshipDto championshipDto, SessionResultDto resultDto, ScoringDto scoring, ClassIdentifier classIdentifier)
        {
            AddResults(championshipDto, resultDto, resultDto, scoring, classIdentifier);
        }

        public void AddResults(ChampionshipDto championshipDto, SessionResultDto resultDto, IHaveManufacturersResults manufacturersResults,  ScoringDto scoring, ClassIdentifier classIdentifier)
        {
            manufacturersResults.ManufacturersSessionResult.Clear();
            if (!championshipDto.IsManufacturerScoringEnabled)
            {
                return;
            }

            IScoringStrategy scoringStrategy = _scoringStrategyFactory.Create(championshipDto.ManufacturerScoringKind);

            List<RepresentantFinishPosition> representantFinishPositions = [];
            HashSet<string> unknownManufacturers = [];
            foreach (DriverSessionResultDto driverSessionResultDto in resultDto.GetDriverSessionResultForClass(classIdentifier))
            {
                if (!_manufacturerProvider.TryGetManufacturer(driverSessionResultDto.CarName, out Manufacturer manufacturer))
                {
                    unknownManufacturers.Add(driverSessionResultDto.CarName);
                }

                int finishPosition = string.IsNullOrEmpty(classIdentifier.ClassId) ? driverSessionResultDto.FinishPosition : driverSessionResultDto.ClassFinishPosition;
                representantFinishPositions.Add(new RepresentantFinishPosition(manufacturer?.Name ?? string.Empty, finishPosition));
            }

            if (unknownManufacturers.Count > 0)
            {
                string remark = "Unknown Car Manufacturers " + string.Join("\n", unknownManufacturers);
                resultDto.AddRemark(remark);
            }

            List<ParticipantPoints> sessionPoints = scoringStrategy.ApplyScoring(representantFinishPositions, scoring);

            if (sessionPoints.Count < 2)
            {
                return;
            }

            ManufacturersChampionshipDto manufacturersChampionshipDto = championshipDto.GetOrCreateManufacturerChampionship(classIdentifier);
            int currentPosition = 1;
            foreach (ParticipantPoints participantPoints in sessionPoints.OrderByDescending(x => x.Points))
            {
                ManufacturerDto manufacturerDto = manufacturersChampionshipDto.GetOrCreateManufacturer(participantPoints.Name);
                ManufacturerSessionResultDto manufacturerSessionResultDto = new()
                {
                    IsPlayer = manufacturerDto.IsPlayer,
                    FinishPosition = currentPosition,
                    ManufacturerName = manufacturerDto.ManufacturerName,
                    PointsGain = participantPoints.Points,
                    TotalPoints = manufacturerDto.TotalPoints + participantPoints.Points,
                    BeforeEventPosition = manufacturerDto.Position,
                };

                manufacturersResults.ManufacturersSessionResult.Add(manufacturerSessionResultDto);
                currentPosition++;
            }

            // AddResults for manufacturers not in Event
            foreach (ManufacturerDto manufacturerDto in manufacturersChampionshipDto.Manufacturers.Where(x =>
                         manufacturersResults.ManufacturersSessionResult.All(y => y.ManufacturerName != x.ManufacturerName)))
            {
                ManufacturerSessionResultDto manufacturerSessionResultDto = new ManufacturerSessionResultDto()
                {
                    IsPlayer = manufacturerDto.IsPlayer,
                    FinishPosition = currentPosition,
                    ManufacturerName = manufacturerDto.ManufacturerName,
                    PointsGain = 0,
                    TotalPoints = manufacturerDto.TotalPoints,
                    BeforeEventPosition = manufacturerDto.Position,
                    SkippedEvent = true,
                };

                manufacturersResults.ManufacturersSessionResult.Add(manufacturerSessionResultDto);
                currentPosition++;
            }

            ManufacturerResultComparer comparer = new(championshipDto, resultDto);
            List<ManufacturerSessionResultDto> manufacturerOrderer = manufacturersResults.ManufacturersSessionResult.OrderBy(x => x, comparer).ToList();
            for (int i = 0; i < manufacturerOrderer.Count; i++)
            {
                manufacturerOrderer[i].AfterEventPosition = i + 1;
            }
        }

        public void CommitResults(ChampionshipDto championshipDto, SessionResultDto resultDto, ClassIdentifier classIdentifier)
        {
                CommitResults(championshipDto, (IHaveManufacturersResults)resultDto, classIdentifier);
        }

        public void CommitResults(ChampionshipDto championshipDto, IHaveManufacturersResults manufacturersResults, ClassIdentifier classIdentifier)
        {
            ManufacturersChampionshipDto manufacturersChampionshipDto = championshipDto.GetOrCreateManufacturerChampionship(classIdentifier);
            foreach (ManufacturerSessionResultDto manufacturerSessionResult in manufacturersResults.ManufacturersSessionResult)
            {
                ManufacturerDto manufacturerDto = manufacturersChampionshipDto.GetOrCreateManufacturer(manufacturerSessionResult.ManufacturerName);
                manufacturerDto.TotalPoints = manufacturerSessionResult.TotalPoints;
                manufacturerDto.Position = manufacturerSessionResult.AfterEventPosition;
            }
        }

        public void ResetChampionship(ChampionshipDto championshipDto)
        {
            championshipDto.ManufacturersChampionshipDtos.Clear();
        }

        public void RevertLastResults(ChampionshipDto championshipDto, ClassIdentifier classIdentifier)
        {
            SessionResultDto resultDto = championshipDto.GetLastSessionWithResults().SessionDto.SessionResult;
            ManufacturersChampionshipDto manufacturersChampionshipDto = championshipDto.GetOrCreateManufacturerChampionship(classIdentifier);
            foreach (ManufacturerSessionResultDto manufacturerSessionResult in resultDto.GetManufacturerSessionResultDtos(classIdentifier.ClassId))
            {
                ManufacturerDto manufacturerDto = manufacturersChampionshipDto.GetOrCreateManufacturer(manufacturerSessionResult.ManufacturerName);
                manufacturerDto.TotalPoints -= manufacturerSessionResult.PointsGain;
                manufacturerDto.Position = manufacturerSessionResult.BeforeEventPosition;
            }
        }
    }
}
