namespace SecondMonitor.DataModel.Summary.FuelStrategy.TimedStintStrategy
{
    using System;

    [Serializable]
    public class TimedStintStrategySettings
    {
        public int StintDurationMinutes { get; set; } = 20;

        public StintLengthKind StintLengthKind { get; set; } = StintLengthKind.LastShort;

        public StintPitStopInfo StintPitStopInfo { get; set; } = StintPitStopInfo.Both;
    }
}
