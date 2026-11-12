namespace SecondMonitor.Rating.Application.Championship.Operations
{
    using System.Collections.Generic;
    using System.Linq;

    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.Rating.Common.DataModel.Championship.Events;

    public class ClassChampionshipManipulator : ISubChampionshipManipulator
    {
        private readonly TeamChampionshipManipulator _teamChampionshipManipulator;
        private readonly ManufacturersChampionshipManipulator _manufacturersChampionshipManipulator;
        public ClassChampionshipManipulator(TeamChampionshipManipulator teamChampionshipManipulator, ManufacturersChampionshipManipulator manufacturersChampionshipManipulator)
        {
            _teamChampionshipManipulator = teamChampionshipManipulator;
            _manufacturersChampionshipManipulator = manufacturersChampionshipManipulator;
        }

        public void SetPlayer(ChampionshipDto championship, DriverDto player)
        {
            if (!championship.IsTrackAllClassesEnabled)
            {
                return;
            }

            foreach (ClassDriverDto classDriverDto in championship.ClassChampionshipDtos.SelectMany(x => x.Drivers))
            {
                classDriverDto.IsPlayer = classDriverDto.DriverGlobalKey == player.GlobalKey;
                if (!classDriverDto.IsPlayer)
                {
                    continue;
                }

                classDriverDto.CarName = player.LastCarName;
                classDriverDto.TeamName = player.TeamName;
            }
        }

        public void AddResults(ChampionshipDto championshipDto, SessionResultDto resultDto, ScoringDto scoring, ClassIdentifier classIdentifier)
        {
            if (!championshipDto.IsTrackAllClassesEnabled)
            {
                return;
            }

            ClassSessionResultDto classSessionResultDto = resultDto.GetOrCreateClassSessionResult(classIdentifier);
            classSessionResultDto.Clear();

            double pointsCoef = resultDto.CompletionPercentage switch
            {
                > 0.75 => 1,
                > 0.25 => .5,
                _ => 0
            };

            ClassChampionshipDto classChampionship = championshipDto.GetOrCreateClassChampionship(classIdentifier);

            // AddResults for manufacturers in event
            foreach (DriverSessionResultDto driverResult in resultDto.GetDriverSessionResultForClass(classIdentifier))
            {
                int pointsGains = (int)(scoring.GetPointsForPosition(driverResult.ClassFinishPosition) * pointsCoef);
                ClassDriverDto driverDto = classChampionship.GetOrCreateDriver(driverResult.DriverGuid);
                driverDto.DriverName = driverResult.DriverName;
                ClassDriverResultDto classDriverResultDto = new()
                {
                    IsPlayer = driverResult.IsPlayer,
                    FinishPosition = driverResult.ClassFinishPosition,
                    DriverGlobalKey = driverDto.DriverGlobalKey,
                    DriverName = driverResult.DriverName,
                    CarName = driverResult.CarName,
                    TeamName = driverResult.TeamName,
                    PointsGain = pointsGains,
                    TotalPoints = driverDto.TotalPoints + pointsGains,
                    BeforeEventPosition = driverDto.Position,
                };

                classSessionResultDto.ClassSessionResult.Add(classDriverResultDto);
            }

            foreach (ClassDriverDto classDriverDto in
                     classChampionship.Drivers.Where(x => classSessionResultDto.ClassSessionResult.All(y => x.DriverGlobalKey != y.DriverGlobalKey)))
            {
                ClassDriverDto driverDto = classChampionship.GetOrCreateDriver(classDriverDto.DriverGlobalKey);
                ClassDriverResultDto classDriverResultDto = new()
                {
                    IsPlayer = driverDto.IsPlayer,
                    FinishPosition = classChampionship.Drivers.Count,
                    DriverGlobalKey = driverDto.DriverGlobalKey,
                    DriverName = classDriverDto.DriverName,
                    CarName = classDriverDto.CarName,
                    TeamName = classDriverDto.TeamName,
                    PointsGain = 0,
                    TotalPoints = driverDto.TotalPoints,
                    BeforeEventPosition = driverDto.Position,
                };

                classSessionResultDto.ClassSessionResult.Add(classDriverResultDto);
            }

            ClassDriverResultComparer comparer = new(championshipDto, resultDto);
            List<ClassDriverResultDto> classResultOrdered = classSessionResultDto.ClassSessionResult.OrderBy(x => x, comparer).ToList();
            for (int i = 0; i < classResultOrdered.Count; i++)
            {
                classResultOrdered[i].AfterEventPosition = i + 1;
            }

            _teamChampionshipManipulator.AddResults(championshipDto, resultDto, classSessionResultDto, scoring, classIdentifier);
            _manufacturersChampionshipManipulator.AddResults(championshipDto, resultDto, classSessionResultDto, scoring, classIdentifier);
        }

        public void CommitResults(ChampionshipDto championshipDto, SessionResultDto resultDto, ClassIdentifier classIdentifier)
        {
            if (!championshipDto.IsTrackAllClassesEnabled)
            {
                return;
            }

            ClassChampionshipDto classChampionship = championshipDto.GetOrCreateClassChampionship(classIdentifier);
            ClassSessionResultDto classSessionResultDto = resultDto.GetOrCreateClassSessionResult(classIdentifier);
            foreach (ClassDriverResultDto classDriverResultDto in classSessionResultDto.ClassSessionResult)
            {
                ClassDriverDto driver = classChampionship.GetOrCreateDriver(classDriverResultDto.DriverGlobalKey);
                driver.IsPlayer = classDriverResultDto.IsPlayer;
                driver.CarName = classDriverResultDto.CarName;
                driver.DriverName = classDriverResultDto.DriverName;
                driver.TotalPoints = classDriverResultDto.TotalPoints;
                driver.Position = classDriverResultDto.AfterEventPosition;
                driver.TeamName = classDriverResultDto.TeamName;
            }

            _teamChampionshipManipulator.CommitResults(championshipDto, classSessionResultDto, classIdentifier);
            _manufacturersChampionshipManipulator.CommitResults(championshipDto, classSessionResultDto, classIdentifier);
        }

        public void ResetChampionship(ChampionshipDto championshipDto)
        {
            if (!championshipDto.IsTrackAllClassesEnabled)
            {
                return;
            }

            championshipDto.ClassChampionshipDtos.Clear();
        }

        public void RevertLastResults(ChampionshipDto championshipDto, ClassIdentifier classIdentifier)
        {
            if (!championshipDto.IsTrackAllClassesEnabled)
            {
                return;
            }

            SessionResultDto resultDto = championshipDto.GetLastSessionWithResults().SessionDto.SessionResult;
            ClassChampionshipDto classChampionshipDto = championshipDto.GetOrCreateClassChampionship(classIdentifier);
            foreach (ClassDriverResultDto classDriverResultDto in resultDto.GetOrCreateClassSessionResult(classIdentifier).ClassSessionResult)
            {
                ClassDriverDto driverDto = classChampionshipDto.GetOrCreateDriver(classDriverResultDto.DriverGlobalKey);
                driverDto.TotalPoints -= classDriverResultDto.PointsGain;
                driverDto.Position = classDriverResultDto.BeforeEventPosition;
            }

            _teamChampionshipManipulator.RevertLastResults(championshipDto, classIdentifier);
            _manufacturersChampionshipManipulator.RevertLastResults(championshipDto, classIdentifier);
        }
    }
}
