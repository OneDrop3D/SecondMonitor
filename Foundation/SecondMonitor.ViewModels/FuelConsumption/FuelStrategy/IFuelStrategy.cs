namespace SecondMonitor.ViewModels.FuelConsumption.FuelStrategy
{
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Summary.FuelConsumption;
    using SecondMonitor.DataModel.Summary.FuelStrategy;
    using SecondMonitor.ViewModels.CarStatus.FuelStatus;

    public interface IFuelStrategy
    {
        string StrategyName { get; }

        //Bigger number, higher priority
        int Priority { get; }

        int FuelMultiplier { get; set; }

        IFuelStrategyViewModel StrategyViewModel { get; }

        IPitStopsViewModel PitStopsViewModel { get; }

        bool IsEligible(SimulatorDataSet dataSet);

        void UpdateConsumptionStatistics(SessionFuelConsumptionDto fuelConsumptionDto);

        void UpdateRequiredFuel(Volume requiredFuel);

        void LoadSettings(ClassFuelStrategy classFuelStrategy);

        void SaveToSettings(ClassFuelStrategy classFuelStrategy);

        void UpdateInRaceFuelPrediction(SimulatorDataSet dataSet, IFuelOverviewViewModel fuelOverviewViewModel, ConsumptionMonitors consumptionMonitors);

        void UpdateNextPitStopInfo(SimulatorDataSet dataSet, IFuelOverviewViewModel fuelOverviewViewModel,  ConsumptionMonitors consumptionMonitors);
    }
}
