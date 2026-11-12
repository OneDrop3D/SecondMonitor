namespace SecondMonitor.Telemetry.TelemetryApplication.Checks.Evaluators.RideHeight
{
    using Result;

    public class RideHeightViewModel : AbstractWheelEvaluationResultViewModel
    {
        public RideHeightViewModel() : base("Ride Height")
        {
        }

        public EvaluationResult RakeEvaluationResults { get; set; }
    }
}