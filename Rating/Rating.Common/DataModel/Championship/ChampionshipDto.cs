namespace SecondMonitor.Rating.Common.DataModel.Championship
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Serialization;
    using Events;

    using SecondMonitor.DataModel.Championship;

    [Serializable]
    public partial class ChampionshipDto
    {
        public ChampionshipDto()
        {
            AllowedClasses = new List<string>();
            ChampionshipGlobalId = Guid.NewGuid();
            Events = new List<EventDto>();
            Scoring = new List<ScoringDto>();
            Drivers = new List<DriverDto>();
            CreationDateTime = DateTime.UtcNow;
            ManufacturersChampionshipDtos = new List<ManufacturersChampionshipDto>();
            TeamsChampionshipDtos = new List<TeamsChampionshipDto>();
            ClassChampionshipDtos = new List<ClassChampionshipDto>();
        }

        public DateTime CreationDateTime { get; set; }

        public List<EventDto> Events { get; set; }

        [XmlAttribute]
        public string SimulatorName { get; set; }

        [XmlAttribute]
        public Guid ChampionshipGlobalId { get; set; }

        [XmlAttribute]
        public string ChampionshipName { get; set; }

        [XmlAttribute]
        public string NextTrack { get; set; }

        [XmlAttribute]
        public ChampionshipState ChampionshipState { get; set; }

        [XmlAttribute]
        public int TotalEvents { get; set; }

        [XmlAttribute]
        public int CurrentEventIndex { get; set; }

        [XmlAttribute]
        public int CurrentSessionIndex { get; set; }

        [XmlIgnore]
        public int Position => Drivers?.FirstOrDefault(x => x.IsPlayer)?.Position ?? 0;

        [XmlAttribute]
        public int TotalDrivers { get; set; }

        [XmlAttribute]
        public string ClassName { get; set; }

        [XmlAttribute]
        public bool AiNamesCanChange { get; set; }

        [XmlAttribute]
        public bool IsClassChangeEnabled { get;  set; }

        [XmlAttribute]
        public bool IsMysteryClass { get; set; }

        [XmlAttribute]
        public bool IsManufacturerScoringEnabled { get; set; }

        [XmlAttribute]
        public ScoringKind ManufacturerScoringKind { get;  set; }

        [XmlAttribute]
        public bool IsTeamScoringEnabled { get; set; }

        [XmlAttribute]
        public ScoringKind TeamScoringKind { get;  set; }

        [XmlAttribute]
        public bool IsTrackAllClassesEnabled { get; set; }

        public List<ScoringDto> Scoring { get; set; }

        public List<DriverDto> Drivers { get; set; }

        public List<ManufacturerDto> Manufacturers { get; set; }

        public List<ManufacturersChampionshipDto> ManufacturersChampionshipDtos { get; set; }

        public List<TeamsChampionshipDto> TeamsChampionshipDtos { get; set; }

        public List<ClassChampionshipDto> ClassChampionshipDtos { get; set; }

        public List<string> AllowedClasses { get; set; }

        [XmlIgnore]
        public bool HasTeamsChampionships => IsTeamScoringEnabled && TeamsChampionshipDtos.Any(x => x.TeamDtos.Count > 1);

        [XmlIgnore]
        public int CompletedRaces => (CurrentEventIndex * Events[0].Sessions.Count) + CurrentSessionIndex + 1;

        [XmlIgnore]
        public DateTime LastRunDateTime => GetAllResults().OrderBy(x => x.ResultCreationTime).LastOrDefault()?.ResultCreationTime ?? CreationDateTime;

        public Dictionary<Guid, DriverDto> GetGuidToDriverDictionary() => Drivers.ToDictionary(x => x.GlobalKey, x => x);

        public EventDto GetCurrentOrLastEvent()
        {
            return CurrentEventIndex < Events.Count ? Events[CurrentEventIndex] : Events.Last();
        }

        public EventDto GetCurrentEvent()
        {
            return CurrentEventIndex < Events.Count ? Events[CurrentEventIndex] : null;
        }

        public IEnumerable<SessionDto> GetAllSessions()
        {
            return Events.SelectMany(x => x.Sessions);
        }

        public IEnumerable<SessionResultDto> GetAllResults()
        {
            return Events.SelectMany(x => x.Sessions).Select(x => x.SessionResult).Where(x => x != null);
        }

        public (EventDto EventDto, SessionDto SessionDto) GetLastSessionWithResults()
        {
            var lastEvent = Events.LastOrDefault(x => x.Sessions.Any(y => y.SessionResult != null));
            return lastEvent == null ? (null, null) : (lastEvent, lastEvent.Sessions.Last(x => x.SessionResult != null));
        }

        public void ResetEventNames()
        {
            for (int i = 0; i < Events.Count; i++)
            {
                EventDto currentEvent = Events[i];
                if (MyRegex().IsMatch(currentEvent.EventName))
                {
                    currentEvent.EventName = $"Round {i + 1}";
                }
            }
        }

        public ClassChampionshipDto GetOrCreateClassChampionship(ClassIdentifier classIdentifier)
        {
            ClassChampionshipDto classChampionshipDto = ClassChampionshipDtos.SingleOrDefault(x => x.ClassId == classIdentifier.ClassId);
            if (classChampionshipDto != null)
            {
                return classChampionshipDto;
            }

            classChampionshipDto = new ClassChampionshipDto()
            {
                ClassId = classIdentifier.ClassId,
                ClassName = classIdentifier.ClassName,
            };

            ClassChampionshipDtos.Add(classChampionshipDto);

            return classChampionshipDto;
        }

        public ManufacturersChampionshipDto GetOrCreateManufacturerChampionship(ClassIdentifier classIdentifier)
        {
            ManufacturersChampionshipDto manufacturerDto = ManufacturersChampionshipDtos.SingleOrDefault(x => x.ClassId == classIdentifier.ClassId);
            if (manufacturerDto != null)
            {
                return manufacturerDto;
            }

            manufacturerDto = new ManufacturersChampionshipDto()
            {
                ClassId = classIdentifier.ClassId,
                ClassName = classIdentifier.ClassName,
            };

            ManufacturersChampionshipDtos.Add(manufacturerDto);

            return manufacturerDto;
        }

        public TeamsChampionshipDto GetOrCreateTeamChampionship(ClassIdentifier classIdentifier)
        {
            TeamsChampionshipDto teamsChampionshipDto = TeamsChampionshipDtos.SingleOrDefault(x => x.ClassId == classIdentifier.ClassId);
            if (teamsChampionshipDto != null)
            {
                return teamsChampionshipDto;
            }

            teamsChampionshipDto = new TeamsChampionshipDto()
            {
                ClassId = classIdentifier.ClassId,
                ClassName = classIdentifier.ClassName,
            };

            TeamsChampionshipDtos.Add(teamsChampionshipDto);

            return teamsChampionshipDto;
        }

        public bool IsTrackingMultipleClasses => IsTrackAllClassesEnabled && ClassChampionshipDtos.Count(x => !string.IsNullOrWhiteSpace(x.ClassId)) > 1;

        public void SanityCheck()
        {
            if (CurrentEventIndex >= Events.Count && ChampionshipState != ChampionshipState.Finished)
            {
                EventDto lastEvent = GetCurrentOrLastEvent();
                if (lastEvent.EventStatus == EventStatus.Finished)
                {
                    ChampionshipState = ChampionshipState.Finished;
                }
                else
                {
                    CurrentEventIndex = Events.Count - 1;
                }
            }
        }

        public ClassIdentifier GetOverviewClassIdentifiers()
        {
            if (IsTrackAllClassesEnabled)
            {
                return ClassIdentifier.OverviewIdentifier;
            }

            DriverDto driverDto = Drivers.FirstOrDefault(x => x.IsPlayer) ?? Drivers.First();
            return new ClassIdentifier(driverDto.LastClassName, driverDto.LastClassId);
        }

        [GeneratedRegex(@"Round \d+")]
        private static partial Regex MyRegex();

        public List<ClassIdentifier> GetClassesOrdered()
        {
            return Drivers.OrderBy(x => x.Position).GroupBy(x => x.LastClassId).Where(x => x.Any()).Select(x => {
                DriverDto firstDriverInClass = x.First();
                return new ClassIdentifier(firstDriverInClass.LastClassName, firstDriverInClass.LastClassId);
            }).ToList();
        }
    }
}