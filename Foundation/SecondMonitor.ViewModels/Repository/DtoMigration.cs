namespace SecondMonitor.ViewModels.Repository
{
    using SecondMonitor.DataModel.Dtos;

    public interface IDtoMigration<T> where T : IDtoWithVersion
    {
        int VersionAfterMigration { get; }

        T ApplyMigration(T oldVersion);
    }
}
