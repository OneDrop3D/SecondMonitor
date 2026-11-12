namespace SecondMonitor.DataModel.TrackRecords
{
    using System.Collections.Generic;
    using System.Linq;

    using SecondMonitor.DataModel.Dtos;

    public class SimulatorsRecords : IDtoWithVersion
    {
        public SimulatorsRecords()
        {
            SimulatorRecords = new List<SimulatorRecords>();
        }

        public int Version { get; set; }

        public List<SimulatorRecords> SimulatorRecords { get; set; }

        public SimulatorRecords GetOrCreateSimulatorRecords(string simulatorName)
        {
            var simulatorRecord = SimulatorRecords.FirstOrDefault(x => x.SimulatorName == simulatorName);
            if (simulatorRecord == null)
            {
                simulatorRecord = new SimulatorRecords()
                {
                    SimulatorName = simulatorName
                };
                SimulatorRecords.Add(simulatorRecord);
            }

            return simulatorRecord;
        }

        public void RemoveRecordsForSimulator(string simulatorName)
        {
            GetOrCreateSimulatorRecords(simulatorName).RemoveAllRecords();
        }

        public void RemoveRecordsForSimulator(string simulatorName, string track, string carClass)
        {
            GetOrCreateSimulatorRecords(simulatorName).RemoveRecordsForClass(carClass, track);
        }

        public void RemoveClasTrackForSimulator(string simulatorName, string trackName)
        {
            GetOrCreateSimulatorRecords(simulatorName).RemoveRecordsForTrack(trackName);
        }

        public void RemoveClassRecordsForSimulator(string simulatorName, string carClass)
        {
            GetOrCreateSimulatorRecords(simulatorName).RemoveRecordsForClass(carClass);
        }
    }
}