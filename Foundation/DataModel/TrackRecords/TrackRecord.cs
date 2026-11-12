namespace SecondMonitor.DataModel.TrackRecords
{
    using System.Collections.Generic;
    using System.Linq;
    using BasicProperties;

    public class TrackRecord
    {
        public TrackRecord()
        {
            VehicleRecords = new List<NamedRecordSet>();
            ClassRecords = new List<NamedRecordSet>();
            OverallRecord = new RecordSet();
        }

        public string TrackName { get; set; }

        public RecordSet OverallRecord { get; set; }

        public List<NamedRecordSet> VehicleRecords { get; set; }

        public List<NamedRecordSet> ClassRecords { get; set; }

        public NamedRecordSet GetOrCreateClassRecord(string className)
        {
            var classRecord = ClassRecords.FirstOrDefault(x => x.Name == className);
            if (classRecord == null)
            {
                classRecord = new NamedRecordSet()
                {
                    Name = className,
                };
                ClassRecords.Add(classRecord);
            }

            return classRecord;
        }

        public NamedRecordSet GetOrCreateVehicleRecord(string vehicleName)
        {
            var vehicleRecord = VehicleRecords.FirstOrDefault(x => x.Name == vehicleName);
            if (vehicleRecord == null)
            {
                vehicleRecord = new NamedRecordSet()
                {
                    Name = vehicleName,
                };
                VehicleRecords.Add(vehicleRecord);
            }

            return vehicleRecord;
        }

        public void RemoveClassRecord(string className)
        {
            ClassRecords.RemoveAll(x => x.Name == className);
            VehicleRecords.RemoveAll(x => x.GetOverAllBest()?.CarClass == className);
            OverallRecord.SetProperEntry(SessionType.Practice, ClassRecords.OrderBy(x => x.BestPracticeRecord?.LapTimeSeconds ?? double.MaxValue).FirstOrDefault()?.BestPracticeRecord);
            OverallRecord.SetProperEntry(SessionType.Qualification, ClassRecords.OrderBy(x => x.BestQualiRecord?.LapTimeSeconds ?? double.MaxValue).FirstOrDefault()?.BestQualiRecord);
            OverallRecord.SetProperEntry(SessionType.Race, ClassRecords.OrderBy(x => x.BestRaceRecord?.LapTimeSeconds ?? double.MaxValue).FirstOrDefault()?.BestRaceRecord);
        }
    }
}