namespace SecondMonitor.ViewModels.PitBoard
{
    using StatusIcon;

    public class SafetyCarPitBoardViewModel : AbstractViewModel
    {
        private string _additionalText;
        private StatusIconState _safetyCarIconState;
        private StatusIconState _additionalTextState;

        public string AdditionalText
        {
            get => _additionalText;
            set => SetProperty(ref _additionalText, value);
        }

        public StatusIconState SafetyCarIconState
        {
            get => _safetyCarIconState;
            set => SetProperty(ref _safetyCarIconState, value);
        }

        public StatusIconState AdditionalTextState
        {
            get => _additionalTextState;
            set => SetProperty(ref _additionalTextState, value);
        }
    }
}