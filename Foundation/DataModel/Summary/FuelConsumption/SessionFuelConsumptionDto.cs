namespace SecondMonitor.DataModel.Summary.FuelConsumption
{
    using System;
    using System.Globalization;
    using System.Xml.Serialization;
    using BasicProperties;
    using BasicProperties.FuelConsumption;

    [Serializable]
    public class SessionFuelConsumptionDto
    {
        public SessionFuelConsumptionDto(string simulator, string trackFullName, double lapDistance, string carName, SessionType sessionKind, double elapsedSeconds, double traveledDistanceMeters, Volume consumedFuel, DateTime recordDate, bool isWetSession)
        {
            IsWetSession = isWetSession;
            Simulator = simulator;
            TrackFullName = trackFullName;
            LapDistance = lapDistance;
            CarName = carName;
            SessionKind = sessionKind;
            ElapsedSeconds = elapsedSeconds;
            TraveledDistanceMeters = traveledDistanceMeters;
            ConsumedFuel = consumedFuel;
            RecordDate = recordDate;
        }

        public SessionFuelConsumptionDto(string simulator, string trackFullName, double lapDistance, string carName, SessionType sessionKind, double elapsedSeconds, double traveledDistanceMeters, Volume consumedFuel, DateTime recordDate, bool isWetSession, double virtualEnergyFuelCoef)
        : this(simulator, trackFullName, lapDistance, carName, sessionKind, elapsedSeconds, traveledDistanceMeters, consumedFuel, recordDate, isWetSession)
        {
            VirtualEnergyFuelCoef = virtualEnergyFuelCoef;
            HasVirtualEnergyFuelCoef = true;
        }

        public SessionFuelConsumptionDto()
        {
        }

        [XmlAttribute]
        public bool IsWetSession { get; set; }

        [XmlAttribute]
        public string Simulator { get; set; }

        [XmlAttribute]
        public string TrackFullName { get; set; }

        [XmlAttribute]
        public double LapDistance { get; set; }

        [XmlAttribute]
        public string CarName { get; set; }

        [XmlAttribute]
        public SessionType SessionKind { get; set; }

        [XmlAttribute]
        public double ElapsedSeconds { get; set; }

        [XmlAttribute]
        public double TraveledDistanceMeters { get; set; }

        [XmlElement]
        public Volume ConsumedFuel { get; set; }

        [XmlAttribute]
        public double VirtualEnergyFuelCoef { get; set; }

        [XmlAttribute]
        public bool HasVirtualEnergyFuelCoef { get; set; }

        [XmlIgnore]
        public DateTime RecordDate { get; set; }

        [XmlIgnore]
        public FuelPerDistance Consumption => new FuelPerDistance(ConsumedFuel, Distance.FromMeters(TraveledDistanceMeters));

        [XmlAttribute]
        public string RecordDateTime
        {
            get => RecordDate.ToString("O");
            set => RecordDate = DateTime.Parse(value, null, DateTimeStyles.RoundtripKind);
        }

        public Volume GetAveragePerMinute()
        {
            return Volume.FromLiters(ConsumedFuel.InLiters / (ElapsedSeconds / 60.0));
        }

        public Volume GetAveragePerDistance(Distance distance) => GetAveragePerDistance(distance.InMeters);

        public Volume GetAveragePerDistance(double distance)
        {
            double distanceCoef = TraveledDistanceMeters / distance;
            return Volume.FromLiters(ConsumedFuel.InLiters / distanceCoef);
        }

        public Volume GetAveragePerLap() => GetAveragePerDistance(LapDistance);
    }
}