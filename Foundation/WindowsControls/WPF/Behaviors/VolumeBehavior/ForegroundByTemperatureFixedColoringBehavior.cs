namespace SecondMonitor.WindowsControls.WPF.Behaviors.VolumeBehavior
{
    using DataModel.BasicProperties;

    public class ForegroundByTemperatureFixedColoringBehavior : ForegroundByOptimalVolumeBehavior<Temperature>
    {
        public ForegroundByTemperatureFixedColoringBehavior()
        {
            AboveColorTransitionRange = 100;
            BelowColorTransitionRange = 100;
        }

        public double AboveColorTransitionRange { get; set; }

        public double BelowColorTransitionRange { get; set; }

        protected override double GetAboveColoringRange()
        {
            return AboveColorTransitionRange;
        }

        protected override double GetBelowColoringRange()
        {
            return BelowColorTransitionRange;
        }
    }
}