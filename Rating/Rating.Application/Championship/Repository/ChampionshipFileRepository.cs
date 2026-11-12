namespace SecondMonitor.Rating.Application.Championship.Repository
{
    using System.Collections.Generic;

    using Common.DataModel.Championship;
    using SecondMonitor.ViewModels.Repository;
    using SecondMonitor.ViewModels.Settings;

    public class ChampionshipFileRepository : AbstractXmlRepositoryWithMigration<AllChampionshipsDto>, IChampionshipsRepository
    {
        public ChampionshipFileRepository(ISettingsProvider settingsProvider, List<IDtoMigration<AllChampionshipsDto>> migrations) : base(migrations)
        {
            RepositoryDirectory = settingsProvider.ChampionshipRepositoryPath;
        }

        protected override string RepositoryDirectory { get; }
        protected override string FileName => "Championships.xml";
    }
}