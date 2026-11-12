namespace SecondMonitor.Rating.Common.DataModel.Championship
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Serialization;

    [Serializable]
    public class ManufacturersChampionshipDto
    {
        [XmlAttribute]
        public string ClassName { get; set; } = string.Empty;

        [XmlAttribute]
        public string ClassId { get; set; } = string.Empty;

        public List<ManufacturerDto> Manufacturers { get; set; } = new();

        public ManufacturerDto GetOrCreateManufacturer(string manufacturerName)
        {
            ManufacturerDto manufacturerDto = Manufacturers.SingleOrDefault(x => x.ManufacturerName == manufacturerName);
            if (manufacturerDto != null)
            {
                return manufacturerDto;
            }

            manufacturerDto = new ManufacturerDto()
            {
                Position = Manufacturers.Count + 1,
                IsPlayer = false,
                ManufacturerName = manufacturerName,
                TotalPoints = 0,
            };

            Manufacturers.Add(manufacturerDto);

            return manufacturerDto;
        }
    }
}
