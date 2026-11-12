namespace SecondMonitor.Rating.Common.DataModel.Championship
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Serialization;

    [Serializable]
    public class ClassChampionshipDto
    {
        [XmlAttribute]
        public string ClassName { get; set; } = string.Empty;

        [XmlAttribute]
        public string ClassId { get; set; } = string.Empty;

        public List<ClassDriverDto> Drivers { get; set; } = new List<ClassDriverDto>();

        public ClassIdentifier GetClassIdentifier => new ClassIdentifier(ClassName, ClassId);

        public ClassDriverDto GetOrCreateDriver(Guid driverGlobalKey)
        {
            ClassDriverDto classDriverDto = Drivers.SingleOrDefault(x => x.DriverGlobalKey == driverGlobalKey);
            if (classDriverDto != null)
            {
                return classDriverDto;
            }

            classDriverDto = new ClassDriverDto()
            {
                Position = Drivers.Count + 1,
                IsPlayer = false,
                DriverGlobalKey = driverGlobalKey,
                TotalPoints = 0,
            };

            Drivers.Add(classDriverDto);

            return classDriverDto;
        }
    }
}
