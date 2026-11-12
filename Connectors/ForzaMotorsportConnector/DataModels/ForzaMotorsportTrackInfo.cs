namespace SecondMonitor.ForzaMotorsportConnector.DataModels
{
    using Newtonsoft.Json;

    internal class ForzaMotorsportTrackInfo
    {
        public int TrackOrdinal { get; set; }

        public string Circuit { get; set; }

        public string Location { get; set; }

        [JsonProperty(propertyName: "IOC_Code")]
        public string IOCCode { get; set; }

        public string Track { get; set; }

        [JsonProperty(propertyName: "Length_in_km")]
        public float LengthInKm { get; set; }
    }
}
