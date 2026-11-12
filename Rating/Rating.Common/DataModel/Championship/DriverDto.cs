namespace SecondMonitor.Rating.Common.DataModel.Championship
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using SecondMonitor.DataModel.Snapshot.Drivers;

    [Serializable]
    public class DriverDto : ParticipantDto
    {
        public DriverDto()
        {
            OtherNames = new List<string>();
            GlobalKey = Guid.NewGuid();
        }

        [XmlAttribute]
        public Guid GlobalKey { get; set; }

        [XmlAttribute]
        public string LastUsedName { get; set; }

        [XmlAttribute]
        public string LastCarName { get; set; }

        [XmlAttribute]
        public string FirstUsedName { get; set; }

        [XmlAttribute]
        public string LastClassName { get; set; }

        [XmlAttribute]
        public string LastClassId { get; set; }

        public List<string> OtherNames { get; set; }

        [XmlAttribute]
        public bool IsInactive { get; set; }

        [XmlAttribute]
        public string TeamName { get; set; }

        public void SetAnotherName(string driverName)
        {
            if (!OtherNames.Contains(LastUsedName))
            {
                OtherNames.Add(LastUsedName);
            }

            LastUsedName = driverName;
        }

        public void UpdateCarAndClass(IDriverInfo driverInfo)
        {
            LastCarName = driverInfo.CarName;
            LastClassName = ClassIdentifier.RemoveClassSuffixes(driverInfo.CarClassName);
            LastClassId = ClassIdentifier.RemoveClassSuffixes(driverInfo.CarClassId);
            TeamName = driverInfo.TeamName;
        }
    }
}