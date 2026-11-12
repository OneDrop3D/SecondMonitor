namespace SecondMonitor.ViewModels.Repository
{
    public interface IAbstractXmlRepository<T> where T : class, new()
    {
        T LoadOrCreateNew();
        void Save(T toSave);
    }
}