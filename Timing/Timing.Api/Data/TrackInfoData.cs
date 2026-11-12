namespace SecondMonitor.Timing.Api.Data
{
    using SecondMonitor.DataModel.Snapshot;

    public class TrackInfoData
    {
        public TrackInfoData(TrackInfo trackInfo)
        {
            TrackName = trackInfo.TrackName;
            TrackLayoutName = trackInfo.TrackLayoutName;
            LayoutLengthMeters = trackInfo.LayoutLength.InMeters;
        }

        private TrackInfoData()
        {
            TrackName = string.Empty;
            TrackLayoutName = string.Empty;
            LayoutLengthMeters = 0;
        }

        public string TrackName { get; set; }

        public string TrackLayoutName { get; set; }

        public double LayoutLengthMeters { get; set; }

        public string TrackFullName => string.IsNullOrEmpty(TrackLayoutName) ? TrackName : $"{TrackName}-{TrackLayoutName}";
        public static TrackInfoData Empty => new TrackInfoData();
    }
}
