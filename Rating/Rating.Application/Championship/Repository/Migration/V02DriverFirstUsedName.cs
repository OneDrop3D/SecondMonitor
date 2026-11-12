namespace SecondMonitor.Rating.Application.Championship.Repository.Migration
{
    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.ViewModels.Repository;

    public class V02DriverFirstUsedName : IDtoMigration<AllChampionshipsDto>
    {
        public int VersionAfterMigration => 2;

        public AllChampionshipsDto ApplyMigration(AllChampionshipsDto oldVersion)
        {
            oldVersion.Championships.ForEach(x => MigrateUp(x));
            return oldVersion;
        }

        private void MigrateUp(ChampionshipDto championshipDto)
        {
            championshipDto.Drivers.ForEach(x => x.FirstUsedName = x.OtherNames.Count != 0 ? x.OtherNames[0] : x.LastUsedName);
        }
    }
}
