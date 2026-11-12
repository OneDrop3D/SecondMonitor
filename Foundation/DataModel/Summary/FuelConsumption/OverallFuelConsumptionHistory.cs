namespace SecondMonitor.DataModel.Summary.FuelConsumption
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using SecondMonitor.DataModel.Dtos;

    [Serializable]
    public class OverallFuelConsumptionHistory : IDtoWithVersion
    {
        public List<CarFuelConsumptionHistory> CarFuelConsumptionHistories { get; set; } = new();
        public int Version { get; set; } = 1;

        public IReadOnlyCollection<SessionFuelConsumptionDto> GetTrackConsumptionHistoryEntries(string simName, string trackFullName, string carName)
        {
            return GetCarFuelConsumption(simName, carName).GetTrackConsumptionHistoryEntries(simName, trackFullName);
        }

        public bool RemoveTrackConsumptionHistoryEntry(SessionFuelConsumptionDto sessionFuelConsumptionDto)
        {
            return GetCarFuelConsumption(sessionFuelConsumptionDto.Simulator, sessionFuelConsumptionDto.CarName).RemoveTrackConsumptionEntry(sessionFuelConsumptionDto);
        }

        public void AddTrackConsumptionHistoryEntry(SessionFuelConsumptionDto sessionFuelConsumptionDto)
        {
            GetCarFuelConsumption(sessionFuelConsumptionDto.Simulator, sessionFuelConsumptionDto.CarName).AddTrackConsumptionHistory(sessionFuelConsumptionDto.Simulator, sessionFuelConsumptionDto.TrackFullName, sessionFuelConsumptionDto);
        }

        private CarFuelConsumptionHistory GetCarFuelConsumption(string simName, string carName)
        {
            var history = CarFuelConsumptionHistories.FirstOrDefault(x => x.CarName == carName && x.SimName == simName);
            if (history == null)
            {
                history = new CarFuelConsumptionHistory() { CarName = carName, SimName = simName };
                CarFuelConsumptionHistories.Add(history);
            }

            return history;
        }
    }
}