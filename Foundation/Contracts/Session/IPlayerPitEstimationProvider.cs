namespace SecondMonitor.Contracts.Session
{
    public interface IPlayerPitEstimationProvider
    {
        bool IsPlayerExpectedToPit { get; }

        bool IsPredictedReturnPositionFilled { get; }

        PitReturnPrediction CurrentPitReturnPrediction { get; }

        PitPredictionReason PitPredictionReason { get; }

        void Reset();
    }
}