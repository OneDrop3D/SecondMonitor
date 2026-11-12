namespace SecondMonitor.Rating.Application.Championship.Repository.Migration
{
    using System.Linq;

    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.ViewModels.Repository;

    public class V01ManufacturerStandingsForEachClass : IDtoMigration<AllChampionshipsDto>
    {
        public int VersionAfterMigration => 1;

        public AllChampionshipsDto ApplyMigration(AllChampionshipsDto oldVersion)
        {
            oldVersion.Championships.ForEach(x => MigrateUp(x));
            return oldVersion;
        }

        private void MigrateUp(ChampionshipDto championshipDto)
        {
            if (!championshipDto.IsManufacturerScoringEnabled || !championshipDto.Manufacturers.Any() || !championshipDto.Drivers.Any())
            {
                return;
            }

            DriverDto driver = championshipDto.Drivers.FirstOrDefault(x => x.IsInactive);
            if (driver == null)
            {
                return;
            }

            championshipDto.GetOrCreateManufacturerChampionship(new ClassIdentifier(driver.LastClassName, driver.LastClassId)).Manufacturers = championshipDto.Manufacturers.ToList();
        }
    }
}
