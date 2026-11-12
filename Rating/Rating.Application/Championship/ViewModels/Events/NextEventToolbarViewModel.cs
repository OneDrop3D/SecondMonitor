namespace SecondMonitor.Rating.Application.Championship.ViewModels.Events
{
    using System.Windows.Input;

    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.Rating.Common.DataModel.Championship.Events;
    using SecondMonitor.ViewModels;

    public class NextEventToolbarViewModel : AbstractViewModel<ChampionshipDto>
    {
        public const string ViewModelLayoutName = "Next Championship Event";
        private bool _isVisible;
        private string _track;
        private string _eventDate;
        private string _championshipName;
        private ICommand _nextChampionshipCommand;
        private ICommand _randomChampionshipCommand;
        private string _eventLength;

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public string Track
        {
            get => _track;
            set => SetProperty(ref _track, value);
        }

        public string EventDate
        {
            get => _eventDate;
            set => SetProperty(ref _eventDate, value);
        }

        public string EventLength
        {
            get => _eventLength;
            set => SetProperty(ref _eventLength, value);
        }

        public string ChampionshipName
        {
            get => _championshipName;
            set => SetProperty(ref _championshipName, value);
        }

        public ICommand NextChampionshipCommand
        {
            get => _nextChampionshipCommand;
            set => SetProperty(ref _nextChampionshipCommand, value);
        }

        public ICommand RandomChampionshipCommand
        {
            get => _randomChampionshipCommand;
            set => SetProperty(ref _randomChampionshipCommand, value);
        }

        protected override void ApplyModel(ChampionshipDto model)
        {
            if (model == null)
            {
                IsVisible = false;
                return;
            }

            ChampionshipName = model.ChampionshipName;
            EventDto eventDto = model.GetCurrentEvent();
            if (eventDto?.TrackName == null || eventDto.Sessions == null)
            {
                IsVisible = false;
                return;
            }

            Track = eventDto.TrackName.Length < 35 ? eventDto.TrackName : eventDto.TrackName[..34] + "...";
            EventDate = eventDto.IsEventDateDefined ? eventDto.EventDate.ToShortDateString() : string.Empty;

            if (eventDto.Sessions.Count < model.CurrentSessionIndex)
            {
                return;
            }

            EventLength = eventDto.Sessions[model.CurrentSessionIndex].DistanceDescription;
        }

        public override ChampionshipDto SaveToNewModel() => OriginalModel;
    }
}
