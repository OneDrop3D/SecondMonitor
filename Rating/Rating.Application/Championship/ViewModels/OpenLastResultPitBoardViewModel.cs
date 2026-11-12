namespace SecondMonitor.Rating.Application.Championship.ViewModels
{
    using System;
    using System.Windows.Input;
    using Common.DataModel.Championship;
    using SecondMonitor.ViewModels;

    public class OpenLastResultPitBoardViewModel : AbstractViewModel<ChampionshipDto>
    {
        public string ChampionshipName { get; private set; }

        public ICommand OpenCommand { get; set; }

        public bool AlreadyOpened { get; set; }

        protected override void ApplyModel(ChampionshipDto model)
        {
            AlreadyOpened = false;
            ChampionshipName = model.ChampionshipName;
        }

        public override ChampionshipDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}