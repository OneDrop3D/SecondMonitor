namespace SecondMonitor.Rating.Common.DataModel.Championship
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Serialization;

    [Serializable]
    public class TeamsChampionshipDto
    {
        [XmlAttribute]
        public string ClassName { get; set; } = string.Empty;

        [XmlAttribute]
        public string ClassId { get; set; } = string.Empty;

        public List<TeamDto> TeamDtos { get; set; } = new();

        public TeamDto GetOrCreateTeam(string teamName)
        {
            TeamDto teamDto = TeamDtos.SingleOrDefault(x => x.TeamName == teamName);
            if (teamDto != null)
            {
                return teamDto;
            }

            teamDto = new TeamDto()
            {
                Position = TeamDtos.Count + 1,
                IsPlayer = false,
                TeamName = teamName,
                TotalPoints = 0,
            };

            TeamDtos.Add(teamDto);

            return teamDto;
        }
    }
}
