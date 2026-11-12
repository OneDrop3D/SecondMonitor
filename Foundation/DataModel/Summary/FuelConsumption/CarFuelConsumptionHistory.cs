namespace SecondMonitor.DataModel.Summary.FuelConsumption
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Serialization;

    [Serializable]
    public class CarFuelConsumptionHistory
    {
        public CarFuelConsumptionHistory()
        {
            TrackConsumptionsHistories = new List<TrackConsumptionsHistory>();
        }

        [XmlAttribute]
        public string CarName { get; set; }

        [XmlAttribute]
        public string SimName { get; set; }

        public List<TrackConsumptionsHistory> TrackConsumptionsHistories { get; set; }

        public IReadOnlyCollection<SessionFuelConsumptionDto> GetTrackConsumptionHistoryEntries(string simName, string trackFullName)
        {
            return GetTrackConsumptionHistory(simName, trackFullName).FuelConsumptionDtos.OrderByDescending(x => x.RecordDate).ToList();
        }

        public bool RemoveTrackConsumptionEntry(SessionFuelConsumptionDto sessionFuelConsumptionDto)
        {
            return GetTrackConsumptionHistory(sessionFuelConsumptionDto.Simulator, sessionFuelConsumptionDto.TrackFullName).FuelConsumptionDtos.Remove(sessionFuelConsumptionDto);
        }

        public void AddTrackConsumptionHistory(string simName, string trackFullName, SessionFuelConsumptionDto sessionFuelConsumption)
        {
            GetTrackConsumptionHistory(simName, trackFullName).AddFuelConsumption(sessionFuelConsumption);
        }

        private TrackConsumptionsHistory GetTrackConsumptionHistory(string simName, string trackFullName)
        {
            TrackConsumptionsHistory history = TrackConsumptionsHistories.FirstOrDefault(x => x.TrackFullName == trackFullName);
            if (history == null)
            {
                history = new TrackConsumptionsHistory()
                {
                    SimulatorName = simName,
                    TrackFullName = trackFullName,
                };
                TrackConsumptionsHistories.Add(history);
            }

            return history;
        }
    }
}