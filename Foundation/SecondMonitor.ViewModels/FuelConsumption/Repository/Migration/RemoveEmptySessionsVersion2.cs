namespace SecondMonitor.ViewModels.FuelConsumption.Repository.Migration
{
    using System.Linq;

    using SecondMonitor.DataModel.Summary.FuelConsumption;
    using SecondMonitor.ViewModels.Repository;

    public class RemoveEmptySessionsVersion2 : IDtoMigration<OverallFuelConsumptionHistory>
    {
        public int VersionAfterMigration => 2;

        public OverallFuelConsumptionHistory ApplyMigration(OverallFuelConsumptionHistory oldVersion)
        {
            foreach (TrackConsumptionsHistory trackConsumptionsHistory in oldVersion.CarFuelConsumptionHistories.SelectMany(oldVersionCarFuelConsumptionHistory => oldVersionCarFuelConsumptionHistory.TrackConsumptionsHistories))
            {
                trackConsumptionsHistory.FuelConsumptionDtos = trackConsumptionsHistory.FuelConsumptionDtos.ToList().Where(x => x.ConsumedFuel.InLiters > 0).ToList();
            }

            return oldVersion;
        }
    }
}
