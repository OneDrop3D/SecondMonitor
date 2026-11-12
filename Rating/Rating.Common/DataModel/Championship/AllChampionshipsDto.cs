namespace SecondMonitor.Rating.Common.DataModel.Championship
{
    using System;
    using System.Collections.Generic;

    using SecondMonitor.DataModel.Dtos;

    [Serializable]
    public class AllChampionshipsDto : IDtoWithVersion
    {
        public AllChampionshipsDto()
        {
            Championships = new List<ChampionshipDto>();
            Version = 0;
        }

        public List<ChampionshipDto> Championships { get; set; }

        public int Version { get; set; }
    }
}