namespace SecondMonitor.ViewModels.FuelConsumption.Repository
{
    using System.Collections.Generic;

    using SecondMonitor.DataModel.Summary.FuelConsumption;
    using SecondMonitor.ViewModels.Repository;
    using SecondMonitor.ViewModels.Settings;

    public class FuelConsumptionRepository : AbstractXmlRepositoryWithMigration<OverallFuelConsumptionHistory>
    {
        public FuelConsumptionRepository(ISettingsProvider settingsProvider, IEnumerable<IDtoMigration<OverallFuelConsumptionHistory>> migrations) : base(migrations)
        {
            RepositoryDirectory = settingsProvider.SimulatorContentRepository;
        }

        protected override string RepositoryDirectory { get; }

        protected override string FileName => "FuelConsumption.xml";
    }
}
