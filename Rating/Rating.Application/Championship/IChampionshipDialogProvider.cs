namespace SecondMonitor.Rating.Application.Championship
{
    using Common.DataModel.Championship;
    using DataModel.Snapshot;

    using SecondMonitor.Rating.Application.Championship.ViewModels.Overview;

    public interface IChampionshipDialogProvider
    {
        void ShowWelcomeScreen(SimulatorDataSet dataSet, ChampionshipDto championship);
        void ShowLastEvenResultWindow(ChampionshipDto championship);
        ChampionshipDetailViewModel OpenChampionshipDetailsWindow(ChampionshipDto championship);

        void CloseChampionshipDetailsWindow(ChampionshipDto championship);
    }
}