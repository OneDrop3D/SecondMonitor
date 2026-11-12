namespace SecondMonitor.ViewModels.FuelConsumption.FuelStrategy
{
    public interface IFuelStrategyViewModel : IViewModel
    {
        bool IsStrategyValid { get; }

        string StrategyRemark { get; }
    }
}
