namespace SecondMonitor.DataModel.Summary.FuelConsumption
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Serialization;

    public class TrackConsumptionsHistory
    {
        private const int MaxEntriesPerTrack = 20;

        public TrackConsumptionsHistory()
        {
            FuelConsumptionDtos = new List<SessionFuelConsumptionDto>();
            SimulatorName = string.Empty;
            TrackFullName = string.Empty;
        }

        [XmlAttribute]
        public string SimulatorName { get; set; }

        [XmlAttribute]
        public string TrackFullName { get; set; }

        public List<SessionFuelConsumptionDto> FuelConsumptionDtos { get; set; }

        public void AddFuelConsumption(SessionFuelConsumptionDto sessionFuelConsumption)
        {
            FuelConsumptionDtos.Add(sessionFuelConsumption);
            var entriesToRemove = FuelConsumptionDtos.OrderByDescending(x => x.RecordDate).Skip(MaxEntriesPerTrack).ToList();
            entriesToRemove.ForEach(x => FuelConsumptionDtos.Remove(x));
        }
    }
}