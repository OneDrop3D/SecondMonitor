namespace SecondMonitor.ViewModels.FuelConsumption
{
    using SecondMonitor.DataModel.Summary.FuelStrategy;
    using SecondMonitor.ViewModels.Repository;

    using Settings;

    public class FuelStrategiesRepository : AbstractXmlRepository<FuelStrategies>
    {
        public FuelStrategiesRepository(ISettingsProvider settingsProvider)
        {
            RepositoryDirectory = settingsProvider.SimulatorContentRepository;
        }

        protected override string RepositoryDirectory { get; }

        protected override string FileName => "FuelStrategies.xml";
    }
}