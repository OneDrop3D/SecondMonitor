namespace SecondMonitor.Remote.Debug.Simulation
{
    using DataModel.BasicProperties;
    public class Track
    {
        private readonly int _gridSlotSpacing = 8;

        public string TrackName => "Some Test Track";

        public int PitEntryLapDistance => TrackLength - 25;
        public int PitExitLapDistance => 25;
        public double PitSpeedLimit => Velocity.FromKph(80).InMs;

        public int TrackLength => 400;
        public int PitBoxLapDistance => TrackLength - 10;

        public double GridSlotDistance(int position)
        {
            return -_gridSlotSpacing * position;
        }
    }
}