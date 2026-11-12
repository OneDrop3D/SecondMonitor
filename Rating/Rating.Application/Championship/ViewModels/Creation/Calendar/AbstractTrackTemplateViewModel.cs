namespace SecondMonitor.Rating.Application.Championship.ViewModels.Creation.Calendar
{
    using System.Windows.Input;
    using SecondMonitor.ViewModels;

    public abstract class AbstractTrackTemplateViewModel : AbstractViewModel
    {
        private string _trackName;
        private ICommand _useTemplateInCalendarCommand;
        public string TrackName
        {
            get => _trackName;
            set => SetProperty(ref _trackName, value);
        }

        public ICommand UseTemplateInCalendarCommand
        {
            get => _useTemplateInCalendarCommand;
            set => SetProperty(ref _useTemplateInCalendarCommand, value);
        }
    }
}