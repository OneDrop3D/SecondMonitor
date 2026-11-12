namespace SecondMonitor.AccConnector.UDPData.Structs
{
    using System.Collections.Generic;

    public struct TrackData
    {
        public string TrackName { get; internal set; }
        public int TrackId { get; internal set; }
        public float TrackMeters { get; internal set; }
        public Dictionary<string, List<string>> CameraSets { get; internal set; }
        public IEnumerable<string> HUDPages { get; internal set; }
    }
}
