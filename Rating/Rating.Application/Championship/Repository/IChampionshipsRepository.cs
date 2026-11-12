namespace SecondMonitor.Rating.Application.Championship.Repository
{
    using Common.DataModel.Championship;

    public interface IChampionshipsRepository 
    {
        AllChampionshipsDto LoadOrCreateNew();
        void Save(AllChampionshipsDto allChampionshipsDto);
    }
}