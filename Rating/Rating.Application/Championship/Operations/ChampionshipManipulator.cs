namespace SecondMonitor.Rating.Application.Championship.Operations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Common.DataModel.Championship;
    using Common.DataModel.Championship.Events;

    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;

    using NLog;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.DataModel.SimulatorContent;
    using SecondMonitor.Rating.Application.Championship.ViewModels.Overview.ModifyResults;
    using SecondMonitor.ViewModels.Settings;
    using SecondMonitor.ViewModels.Settings.ViewModel;
    using SecondMonitor.ViewModels.SimulatorContent;

    using Timing.Common.SessionTiming.Drivers.Lap;

    public class ChampionshipManipulator : IChampionshipManipulator
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly ISimulatorContentProvider _simulatorContentProvider;
        private readonly IList<ISubChampionshipManipulator> _subChampionshipManipulators;
        private readonly ClassChampionshipManipulator _classChampionshipManipulator;
        private readonly Random _random;
        private readonly DisplaySettingsViewModel _displaySettingsViewModel;

        public ChampionshipManipulator(ISimulatorContentProvider simulatorContentProvider, ISettingsProvider settingsProvider, 
            IList<ISubChampionshipManipulator> subChampionshipManipulators, ClassChampionshipManipulator classChampionshipManipulator)
        {
            _random = new Random();
            _simulatorContentProvider = simulatorContentProvider;
            _subChampionshipManipulators = subChampionshipManipulators;
            _classChampionshipManipulator = classChampionshipManipulator;
            _displaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
        }

        public void StartChampionship(ChampionshipDto championship, SimulatorDataSet dataSet)
        {
            InitializeDrivers(championship, dataSet);
            championship.GetCurrentOrLastEvent().EventStatus = EventStatus.InProgress;
            championship.ChampionshipState = ChampionshipState.Started;
        }

        public void StartNextEvent(ChampionshipDto championship, SimulatorDataSet dataSet)
        {
            EventDto currentEvent = championship.GetCurrentOrLastEvent();
            currentEvent.TrackName = dataSet.SessionInfo.TrackInfo.TrackFullName;
            championship.ClassName = dataSet.PlayerInfo.CarClassName;
        }

        public void AddResultsForCurrentSession(ChampionshipDto championship, SimulatorDataSet dataSet, ILapInfo bestLap, double completionPercentage)
        {
            UpdateDriversProperties(championship, dataSet);
            EventDto currentEvent = championship.GetCurrentOrLastEvent();
            SessionDto currentSession = currentEvent.Sessions[championship.CurrentSessionIndex];
            //Clear session result, so it is not used for drivers comparison
            currentSession.SessionResult = null;
            try
            {
                currentSession.SessionResult = CreateResultDto(championship, dataSet, bestLap, completionPercentage);
            }
            catch (InvalidOperationException ex)
            {
                logger.Error(ex);
            }
        }

        public void UpdateDriversProperties(ChampionshipDto championship, SimulatorDataSet dataSet)
        {
            UpdatePlayerCar(championship, dataSet);
            UpdateAiDriversNames(championship, dataSet);
        }

        public void UpdatePlayerCar(ChampionshipDto championship, SimulatorDataSet dataSet)
        {
            DriverDto player = championship.Drivers.FirstOrDefault(x => x.IsPlayer);
            if (player == null || dataSet.PlayerInfo == null)
            {
                return;
            }

            player.LastCarName = dataSet.PlayerInfo.CarDisplayName;
            player.LastClassId = ClassIdentifier.RemoveClassSuffixes(dataSet.PlayerInfo.CarClassId);
            player.LastClassName = ClassIdentifier.RemoveClassSuffixes(dataSet.PlayerInfo.CarClassName);
            player.TeamName = dataSet.PlayerInfo.TeamName;

            _subChampionshipManipulators.ForEach(x => x.SetPlayer(championship, player));
        }

        public void UpdateAiDriversNames(ChampionshipDto championship, SimulatorDataSet dataSet)
        {
            logger.Info("Championships - Updating Driver Names");
            DriverDto player = championship.Drivers.FirstOrDefault(x => x.IsPlayer);
            if (player != null && dataSet.PlayerInfo != null)
            {
                player.LastUsedName = dataSet.PlayerInfo.DriverLongName;
            }

            if (championship.IsTrackAllClassesEnabled)
            {
                foreach (IGrouping<string, DriverInfo> driverGroupedByClass in dataSet.DriversInfo.GroupBy(x => ClassIdentifier.RemoveClassSuffixes(x.CarClassId)))
                {
                    List<DriverInfo> driversToAssign = driverGroupedByClass.Where(x => !x.IsPlayer).ToList();
                    UpdateAiDriversNameClass(championship, driversToAssign);
                }
            }
            else
            {
                List<DriverInfo> driversToAssign = dataSet.GetDriversInfoByMultiClass().Where(x => !x.IsPlayer).ToList();
                UpdateAiDriversNameClass(championship, driversToAssign);
            }

            championship.TotalDrivers = championship.Drivers.Count(x => !x.IsInactive);

            UpdateResultsName(championship);
        }

        private static void UpdateAiDriversNameClass(ChampionshipDto championship, List<DriverInfo> driversToAssign)
        {
            string classId = ClassIdentifier.RemoveClassSuffixes(driversToAssign[0].CarClassId);
            List<DriverDto> driverPool = championship.IsTrackAllClassesEnabled
                ? championship.Drivers.Where(x => x.LastClassId == classId && !x.IsPlayer).ToList()
                : championship.Drivers.Where(x => !x.IsPlayer).ToList();
            List<DriverDto> driversByPosition = driverPool.OrderByDescending(x => x.Position).ToList();

            // If there are more drivers in the championships, then in session, then disable those drivers in championships that have the lowest score, until number of participants is equal
            // This is to avoid loosing high scoring drivers from championship, who are not present in the session. Their points are rather assigned to another driver
            for (int i = 0; i < driversByPosition.Count && driversToAssign.Count < driverPool.Count; i++)
            {
                driversByPosition[i].IsInactive = true;
                driverPool.Remove(driversByPosition[i]);
            }

            //First iteration = use first names
            foreach (DriverInfo driver in driversToAssign.ToList())
            {
                DriverDto driverMatch = driverPool.FirstOrDefault(x => x.FirstUsedName == driver.DriverLongName);
                if (driverMatch == null)
                {
                    continue;
                }

                logger.Info($"Championships - Driver {driverMatch.FirstUsedName} matched by First Used Name");
                driverMatch.IsInactive = false;
                driverMatch.SetAnotherName(driver.DriverLongName);
                driverMatch.UpdateCarAndClass(driver);
                driverPool.Remove(driverMatch);
                driversToAssign.Remove(driver);
            }

            //2nd iteration = use previous names
            foreach (DriverInfo driver in driversToAssign.ToList())
            {
                DriverDto driverMatch = driverPool.FirstOrDefault(x => x.LastUsedName == driver.DriverLongName);
                if (driverMatch == null)
                {
                    continue;
                }

                logger.Info($"Championships - Driver {driverMatch.LastUsedName} matched by Last Used Name");
                driverMatch.IsInactive = false;
                driverMatch.UpdateCarAndClass(driver);
                driverPool.Remove(driverMatch);
                driversToAssign.Remove(driver);
            }

            //3rd iteration, try to find drivers that previously used that name
            foreach (DriverInfo driver in driversToAssign.ToList())
            {
                DriverDto driverMatch = driverPool.FirstOrDefault(x => x.OtherNames.Contains(driver.DriverLongName));
                if (driverMatch == null)
                {
                    continue;
                }

                logger.Info($"Championships - Driver {driver.DriverLongName} replaced {driverMatch.LastUsedName}");
                driverMatch.IsInactive = false;
                driverMatch.SetAnotherName(driver.DriverLongName);
                driverMatch.UpdateCarAndClass(driver);
                driverPool.Remove(driverMatch);
                driversToAssign.Remove(driver);
            }

            //4th iteration, try to find drivers that used the same car
            foreach (DriverInfo driver in driversToAssign.ToList())
            {
                DriverDto driverMatch = driverPool.FirstOrDefault(x => x.LastCarName == driver.CarDisplayName);
                if (driverMatch == null)
                {
                    continue;
                }

                logger.Info($"Championships - Driver {driver.DriverLongName} replaced {driverMatch.LastUsedName}");
                driverMatch.IsInactive = false;
                driverMatch.SetAnotherName(driver.DriverLongName);
                driverMatch.UpdateCarAndClass(driver);
                driverPool.Remove(driverMatch);
                driversToAssign.Remove(driver);
            }

            while (driverPool.Count > 0)
            {
                if (driversToAssign.Count > 0)
                {
                    logger.Info($"Championships - Driver {driversToAssign[0].DriverLongName} randomly replaced {driverPool[0].LastUsedName}");
                    driverPool[0].UpdateCarAndClass(driversToAssign[0]);
                    driverPool[0].SetAnotherName(driversToAssign[0].DriverLongName);
                    driverPool.RemoveAt(0);
                    driversToAssign.RemoveAt(0);
                    continue;
                }

                logger.Info($"Championships - Driver {driverPool[0].LastUsedName} set as inactive");
                driverPool[0].IsInactive = true;
                driverPool.RemoveAt(0);
            }

            int position = championship.TotalDrivers;

            List<DriverDto> newDrives = driversToAssign.Select(x => new DriverDto()
            {
                LastUsedName = x.DriverLongName,
                FirstUsedName = x.DriverLongName,
                IsPlayer = false,
                Position = ++position,
                LastCarName = x.CarDisplayName,
                LastClassId = ClassIdentifier.RemoveClassSuffixes(x.CarClassId),
                LastClassName = ClassIdentifier.RemoveClassSuffixes(x.CarClassName),
                TeamName = x.TeamName,
            }).ToList();

            championship.Drivers.AddRange(newDrives);
        }

        public void DnfPlayerAndCommitLastSessionResults(ChampionshipDto championship)
        {
            EventDto currentEvent = championship.GetCurrentOrLastEvent();
            SessionDto currentSession = currentEvent.Sessions[championship.CurrentSessionIndex];
            Dictionary<Guid, DriverDto> guidDriverDictionary = championship.GetGuidToDriverDictionary();

            if (currentSession.SessionResult == null)
            {
                logger.Warn($"No Results for Championship: {championship.ChampionshipName}, advancing");
                AdvanceChampionship(championship);
                return;
            }

            SessionResultDto sessionResults = currentSession.SessionResult;

            ScoringDto scoring = championship.Scoring[championship.CurrentSessionIndex];

            DriverSessionResultDto playerResult = sessionResults.DriverSessionResult.Single(x => x.IsPlayer);
            int playerFinishingPosition = playerResult.FinishPosition;

            //Apply new scoring
            foreach (DriverSessionResultDto driverSessionResult in sessionResults.DriverSessionResult)
            {
                driverSessionResult.TotalPoints -= driverSessionResult.PointsGain;
                if (driverSessionResult.FinishPosition >= playerFinishingPosition)
                {
                    driverSessionResult.FinishPosition = driverSessionResult.IsPlayer ? sessionResults.DriverSessionResult.Count : driverSessionResult.FinishPosition - 1;
                }

                if (driverSessionResult.ClassFinishPosition >= playerResult.ClassFinishPosition && driverSessionResult.ClassId == playerResult.ClassId)
                {
                    driverSessionResult.ClassFinishPosition = driverSessionResult.IsPlayer ? sessionResults.DriverSessionResult.Count(x => x.ClassId == playerResult.ClassId) : driverSessionResult.ClassFinishPosition - 1;
                }

                driverSessionResult.PointsGain = driverSessionResult.FinishPosition <= scoring.Scoring.Count ? scoring.Scoring[driverSessionResult.FinishPosition - 1] : 0;

                if (driverSessionResult.WasFastestLap && driverSessionResult.FinishPosition <= scoring.MaxPositionForFastestLap)
                {
                    driverSessionResult.PointsGain += scoring.PointsForFastestLap;
                }

                driverSessionResult.TotalPoints += driverSessionResult.PointsGain;
            }

            DriverSessionResultComparer comparer = new(championship, sessionResults);
            List<DriverSessionResultDto> resultsOrdered = sessionResults.DriverSessionResult.OrderBy(x => x, comparer).ToList();
            for (int i = 0; i < resultsOrdered.Count; i++)
            {
                resultsOrdered[i].AfterEventPosition = i + 1;
            }

            foreach (DriverSessionResultDto driverSessionResultDto in currentSession.SessionResult.DriverSessionResult)
            {
                DriverDto driverDto = guidDriverDictionary[driverSessionResultDto.DriverGuid];
                driverDto.TotalPoints = driverSessionResultDto.TotalPoints;
                driverDto.Position = driverSessionResultDto.AfterEventPosition;
                driverDto.LastCarName = driverSessionResultDto.CarName;
                driverDto.LastClassName = ClassIdentifier.RemoveClassSuffixes(driverSessionResultDto.ClassName);
                driverDto.LastClassId = ClassIdentifier.RemoveClassSuffixes(driverSessionResultDto.ClassId);
                driverDto.TeamName = driverSessionResultDto.TeamName;
            }

            sessionResults.Remarks = string.Empty;

            ClassIdentifier overviewClassIdentifier = championship.GetOverviewClassIdentifiers();

            _subChampionshipManipulators.ForEach(x => x.AddResults(championship, sessionResults, scoring, overviewClassIdentifier));
            _subChampionshipManipulators.ForEach(x => x.CommitResults(championship, sessionResults, overviewClassIdentifier));

            if (championship.IsTrackAllClassesEnabled)
            {
                List<ClassIdentifier> classesOrdered = championship.GetClassesOrdered();
                classesOrdered.ForEach(x => _classChampionshipManipulator.AddResults(championship, sessionResults, scoring, x));
                classesOrdered.ForEach(x => _classChampionshipManipulator.CommitResults(championship, sessionResults, x));
            }

            AdvanceChampionship(championship);
        }

        public void CommitLastSessionResultsAndAdvance(ChampionshipDto championship)
        {
            EventDto currentEvent = championship.GetCurrentOrLastEvent();
            SessionDto currentSession = currentEvent.Sessions[championship.CurrentSessionIndex];
            if (currentSession.SessionResult == null)
            {
                logger.Warn($"No Results for Championship: {championship.ChampionshipName}, advancing");
                AdvanceChampionship(championship);
                return;
            }

            CommitLastSessionResult(championship);
            AdvanceChampionship(championship);
        }

        public void CommitLastSessionResult(ChampionshipDto championship)
        {
            SessionDto currentSession = championship.GetLastSessionWithResults().SessionDto;
            Dictionary<Guid, DriverDto> guidDriverDictionary = championship.GetGuidToDriverDictionary();

            foreach (DriverSessionResultDto driverSessionResultDto in currentSession.SessionResult.DriverSessionResult)
            {
                DriverDto driverDto = guidDriverDictionary[driverSessionResultDto.DriverGuid];
                driverDto.TotalPoints = driverSessionResultDto.TotalPoints;
                driverDto.Position = driverSessionResultDto.AfterEventPosition;
                driverDto.LastCarName = driverSessionResultDto.CarName;
                driverDto.LastClassName = ClassIdentifier.RemoveClassSuffixes(driverSessionResultDto.ClassName);
                driverDto.LastClassId = ClassIdentifier.RemoveClassSuffixes(driverSessionResultDto.ClassId);
                driverDto.TeamName = driverSessionResultDto.TeamName;
            }

            ClassIdentifier overviewClassIdentifier = championship.GetOverviewClassIdentifiers();
            _subChampionshipManipulators.ForEach(x => x.CommitResults(championship, currentSession.SessionResult, overviewClassIdentifier));
            if (championship.IsTrackAllClassesEnabled)
            {
                List<ClassIdentifier> classesOrdered = championship.GetClassesOrdered();
                classesOrdered.ForEach(x => _classChampionshipManipulator.CommitResults(championship, currentSession.SessionResult, x));
            }
        }

        public void RevertLastSessionResult(ChampionshipDto championship)
        {
            SessionResultDto sessionResult = championship.GetLastSessionWithResults().SessionDto.SessionResult;
            Dictionary<Guid, DriverDto> guidDriverDictionary = championship.GetGuidToDriverDictionary();

            foreach (DriverSessionResultDto driverSessionResultDto in sessionResult.DriverSessionResult)
            {
                DriverDto driverDto = guidDriverDictionary[driverSessionResultDto.DriverGuid];
                driverDto.TotalPoints -= driverSessionResultDto.PointsGain;
                driverDto.Position = driverSessionResultDto.BeforeEventPosition;
            }

            ClassIdentifier overviewClassIdentifier = championship.GetOverviewClassIdentifiers();
            _subChampionshipManipulators.ForEach(x => x.RevertLastResults(championship, overviewClassIdentifier));
            if (championship.IsTrackAllClassesEnabled)
            {
                List<ClassIdentifier> classesOrdered = championship.GetClassesOrdered();
                classesOrdered.ForEach(x => _classChampionshipManipulator.RevertLastResults(championship, x));
            }
        }

        public void AmendLastResult(ChampionshipDto championship, AmendedSessionResult amendedResult)
        {
            RevertLastSessionResult(championship);
            (EventDto EventDto, SessionDto SessionDto) lastSessionWithResults = championship.GetLastSessionWithResults();
            ScoringDto scoring = championship.Scoring[lastSessionWithResults.EventDto.Sessions.IndexOf(lastSessionWithResults.SessionDto)];
            lastSessionWithResults.SessionDto.SessionResult = null;
            SessionResultDto amendedSessionResult = CreatedAmendedLastSessionResultDto(championship, amendedResult, scoring);
            lastSessionWithResults.SessionDto.SessionResult = amendedSessionResult;
            CommitLastSessionResult(championship);
        }

        private void UpdateResultsName(ChampionshipDto championship)
        {
            Dictionary<Guid, DriverDto> driverDictionary = championship.GetGuidToDriverDictionary();
            List<SessionResultDto> allResults = championship.GetAllResults().ToList();
            foreach (DriverSessionResultDto driverResult in championship.GetAllResults().SelectMany(x => x.DriverSessionResult))
            {
                driverResult.DriverName = driverDictionary[driverResult.DriverGuid].LastUsedName;
            }

            foreach (ClassDriverDto classDriverDto in championship.ClassChampionshipDtos.SelectMany(x => x.Drivers))
            {
                classDriverDto.DriverName = driverDictionary[classDriverDto.DriverGlobalKey].LastUsedName;
                classDriverDto.CarName = driverDictionary[classDriverDto.DriverGlobalKey].LastCarName;
            }

            foreach (ClassDriverResultDto classDriverResultDto in allResults.SelectMany(x => x.ClassSessionResults).SelectMany(x => x.ClassSessionResult))
            {
                classDriverResultDto.DriverName = driverDictionary[classDriverResultDto.DriverGlobalKey].LastUsedName;
            }
        }

        private void AdvanceChampionship(ChampionshipDto championship)
        {
            EventDto currentEvent = championship.GetCurrentOrLastEvent();
            championship.CurrentSessionIndex = (championship.CurrentSessionIndex + 1) % currentEvent.Sessions.Count;
            if (championship.CurrentSessionIndex == 0)
            {
                currentEvent.EventStatus = EventStatus.Finished;
                championship.CurrentEventIndex++;
                currentEvent = championship.GetCurrentOrLastEvent();

                if (currentEvent.EventStatus == EventStatus.NotStarted)
                {
                    currentEvent.EventStatus = EventStatus.InProgress;
                }

                championship.NextTrack = currentEvent.TrackName;
                ReRollMysteryProperties(championship);
            }

            championship.ChampionshipState = championship.CurrentEventIndex >= championship.Events.Count ? ChampionshipState.Finished : ChampionshipState.Started;
        }

        public void ReRollMysteryProperties(ChampionshipDto championship)
        {
            EventDto currentEvent = championship.GetCurrentOrLastEvent();
            if (currentEvent.IsMysteryTrack)
            {
                List<string> usedTracks = championship.Events.Select(x => x.TrackName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
                Track track = _simulatorContentProvider.GetRandomTrack(championship.SimulatorName, usedTracks);

                if (track != null)
                {
                    currentEvent.TrackName = track.Name;
                    championship.NextTrack = currentEvent.TrackName;
                    currentEvent.Sessions
                        .Where(x => x.IsDistanceBased)
                        .ForEach(x => x.DistanceDescription =
                            $"{(int)Math.Round(x.DistanceInMeters / track.LapDistance)} Laps / {Distance.FromMeters(x.DistanceInMeters).GetByUnit(_displaySettingsViewModel.DistanceUnits)}{Distance.GetUnitsSymbol(_displaySettingsViewModel.DistanceUnits)}");
                }
            }

            if (championship.IsMysteryClass)
            {
                championship.ClassName = championship.AllowedClasses.Count == 0
                    ? _simulatorContentProvider.GetRandomClass(championship.SimulatorName)?.ClassName ?? string.Empty
                    : championship.AllowedClasses[_random.Next(championship.AllowedClasses.Count)];
            }
        }

        public void ResetChampionship(ChampionshipDto championship, bool reshuffleEvent)
        {
            championship.Events.SelectMany(x => x.Sessions).ForEach(x => x.SessionResult = null);
            championship.Events.ForEach(x => x.EventStatus = EventStatus.NotStarted);
            championship.CurrentEventIndex = 0;
            championship.CurrentSessionIndex = 0;
            championship.ChampionshipState = ChampionshipState.NotStarted;
            championship.Drivers.Clear();
            _classChampionshipManipulator.ResetChampionship(championship);
            _subChampionshipManipulators.ForEach(x => x.ResetChampionship(championship));

            if (reshuffleEvent)
            {
                Random random = Random.Shared;
                championship.Events = championship.Events.OrderBy(x => random.NextDouble()).ToList();
                championship.ResetEventNames();
            }

            EventDto firstEvent = championship.GetCurrentOrLastEvent();
            championship.NextTrack = firstEvent.TrackName;
        }

        private SessionResultDto CreateResultDto(ChampionshipDto championship, SimulatorDataSet dataSet, ILapInfo bestLap, double completionPercentage)
        {
            string bestLapDriverId = bestLap?.Driver.DriverId ?? string.Empty;
            ScoringDto scoring = championship.Scoring[championship.CurrentSessionIndex];
            IReadOnlyCollection<DriverInfo> eligibleDrivers = championship.IsTrackAllClassesEnabled ? dataSet.DriversInfo : dataSet.GetDriversInfoByMultiClass();
            Dictionary<string, (int positionInClass, int postionOverall)> positionMap = eligibleDrivers.ToDictionary(x => x.DriverLongName, x => (x.PositionInClass, x.Position));
            SessionResultDto resultDto = new SessionResultDto()
            {
                CompletionPercentage = completionPercentage
            };

            logger.Info($"Completed % is {completionPercentage}");

            if (completionPercentage < 1)
            {
                resultDto.AddRemark(GetSessionRemainingRemark(dataSet));
            }

            double pointsCoef = completionPercentage switch
            {
                > 0.75 => 1,
                > 0.25 => .5,
                _ => 0
            };

            foreach (DriverDto championshipDriver in championship.Drivers.Where(x => !x.IsInactive))
            {
                DriverInfo sessionDriver = dataSet.DriversInfo.FirstOrDefault(x => x.DriverLongName == championshipDriver.LastUsedName);
                if (sessionDriver == null)
                {
                    championshipDriver.IsInactive = true;
                    logger.Warn($"Driver {championshipDriver.LastUsedName} not found");
                    continue;
                }

                (int positionInClass, int positionOverall) = positionMap[sessionDriver.DriverLongName];
                int position = championship.IsTrackAllClassesEnabled ? positionOverall : positionInClass;
                DriverSessionResultDto driverResult = new()
                {
                    DriverGuid = championshipDriver.GlobalKey,
                    DriverName = championshipDriver.LastUsedName,
                    ClassFinishPosition = positionInClass,
                    FinishPosition = position,
                    PointsGain = position <= scoring.Scoring.Count ? (int)(scoring.Scoring[position - 1] * pointsCoef) : 0,
                    BeforeEventPosition = championshipDriver.Position,
                    IsPlayer = championshipDriver.IsPlayer,
                    CarName = sessionDriver.CarDisplayName,
                    ClassId = ClassIdentifier.RemoveClassSuffixes(sessionDriver.CarClassId),
                    ClassName = ClassIdentifier.RemoveClassSuffixes(sessionDriver.CarClassName),
                    TeamName = sessionDriver.TeamName,
                };

                if (bestLapDriverId == sessionDriver.DriverSessionId && position <= scoring.MaxPositionForFastestLap)
                {
                    logger.Info($"Fastest Lap for ${bestLapDriverId}, on position {position}");
                    driverResult.WasFastestLap = true;
                    driverResult.PointsGain += (int)(scoring.PointsForFastestLap * pointsCoef);
                }

                driverResult.TotalPoints = championshipDriver.TotalPoints + driverResult.PointsGain;

                resultDto.DriverSessionResult.Add(driverResult);
            }

            DriverSessionResultComparer comparer = new(championship, resultDto);
            List<DriverSessionResultDto> driversAfterRaceOrdered = resultDto.DriverSessionResult.OrderBy(x => x, comparer).ToList();
            for (int i = 0; i < driversAfterRaceOrdered.Count; i++)
            {
                driversAfterRaceOrdered[i].AfterEventPosition = i + 1;
            }

            ClassIdentifier overviewClassIdentifier = championship.GetOverviewClassIdentifiers();
            _subChampionshipManipulators.ForEach(x => x.AddResults(championship, resultDto, scoring, overviewClassIdentifier));
            if (championship.IsTrackAllClassesEnabled)
            {
                List<ClassIdentifier> classesOrdered = championship.GetClassesOrdered();
                classesOrdered.ForEach(x => _classChampionshipManipulator.AddResults(championship, resultDto, scoring, x));
            }

            return resultDto;
        }

        private SessionResultDto CreatedAmendedLastSessionResultDto(ChampionshipDto championship, AmendedSessionResult amendedSessionResult, ScoringDto scoring)
        {
            SessionResultDto resultDto = new SessionResultDto()
            {
                CompletionPercentage = amendedSessionResult.OriginalResuts.CompletionPercentage,
                ResultCreationTime = amendedSessionResult.OriginalResuts.ResultCreationTime,
                Remarks = amendedSessionResult.OriginalResuts.Remarks,
            };

            double pointsCoef = resultDto.CompletionPercentage switch
            {
                > 0.75 => 1,
                > 0.25 => .5,
                _ => 0
            };

            foreach (AmendedDriverResult amendedDriverResult in amendedSessionResult.AmendedDriverResults)
            {
                DriverDto championshipDriver = championship.Drivers.Single(x => x.GlobalKey == amendedDriverResult.DriverGuid);
                int position = championship.IsTrackAllClassesEnabled ? amendedDriverResult.AmendedDriverFinishPosition : amendedDriverResult.AmendedDriverClassFinishPosition;
                DriverSessionResultDto driverResult = new()
                {
                    DriverGuid = amendedDriverResult.DriverGuid,
                    DriverName = amendedDriverResult.DriverName,
                    FinishPosition = position,
                    ClassFinishPosition = amendedDriverResult.AmendedDriverClassFinishPosition,
                    PointsGain = scoring.GetPointsForPosition(position),
                    BeforeEventPosition = championshipDriver.Position,
                    IsPlayer = championshipDriver.IsPlayer,
                    CarName = amendedDriverResult.CarName,
                    ClassId = ClassIdentifier.RemoveClassSuffixes(amendedDriverResult.ClassId),
                    ClassName = ClassIdentifier.RemoveClassSuffixes(amendedDriverResult.ClassName),
                    TeamName = amendedDriverResult.TeamName,
                };

                if (amendedDriverResult.WasFastestLap && position <= scoring.MaxPositionForFastestLap)
                {
                    driverResult.WasFastestLap = true;
                    driverResult.PointsGain += (int)(scoring.PointsForFastestLap * pointsCoef);
                }

                driverResult.TotalPoints = championshipDriver.TotalPoints + driverResult.PointsGain;

                resultDto.DriverSessionResult.Add(driverResult);
            }

            DriverSessionResultComparer comparer = new(championship, resultDto);
            List<DriverSessionResultDto> driversAfterRaceOrdered = resultDto.DriverSessionResult.OrderBy(x => x, comparer).ToList();
            for (int i = 0; i < driversAfterRaceOrdered.Count; i++)
            {
                driversAfterRaceOrdered[i].AfterEventPosition = i + 1;
            }

            ClassIdentifier overviewClassIdentifier = championship.GetOverviewClassIdentifiers();
            _subChampionshipManipulators.ForEach(x => x.AddResults(championship, resultDto, scoring, overviewClassIdentifier));
            if (championship.IsTrackAllClassesEnabled)
            {
                List<ClassIdentifier> classesOrdered = championship.GetClassesOrdered();
                classesOrdered.ForEach(x => _classChampionshipManipulator.AddResults(championship, resultDto, scoring, x));
            }

            return resultDto;
        }

        private string GetSessionRemainingRemark(SimulatorDataSet dataSet)
        {
            return dataSet.SessionInfo.SessionLengthType switch
            {
                SessionLengthType.Laps => $"Stopped with {Math.Max(1, dataSet.SessionInfo.TotalNumberOfLaps - dataSet.LeaderInfo.CompletedLaps)} laps to go.",
                SessionLengthType.Time or SessionLengthType.TimeWithExtraLap =>
                    $"Stopped with {TimeSpan.FromSeconds(dataSet.SessionInfo.SessionTimeRemaining).FormatToHoursMinutesSeconds()} to go.",
                _ => string.Empty
            };
        }

        private void InitializeDrivers(ChampionshipDto championship, SimulatorDataSet dataSet)
        {
            int position = 0;
            IReadOnlyCollection<DriverInfo> eligibleDrivers = championship.IsTrackAllClassesEnabled ? dataSet.DriversInfo : dataSet.GetDriversInfoByMultiClass();
            championship.ClassName = dataSet.PlayerInfo.CarClassName;
            championship.TotalDrivers = eligibleDrivers.Count;
            championship.Drivers = eligibleDrivers.Select(x => new DriverDto()
            {
                LastUsedName = x.DriverLongName,
                FirstUsedName = x.DriverLongName,
                IsPlayer = x.IsPlayer,
                Position = ++position,
                LastCarName = x.CarDisplayName,
                LastClassId = ClassIdentifier.RemoveClassSuffixes(x.CarClassId),
                LastClassName = ClassIdentifier.RemoveClassSuffixes(x.CarClassName),
                TeamName = x.TeamName,
            }).ToList();
        }
    }
}
