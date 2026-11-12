namespace SecondMonitor.ViewModels.StatusIcon
{
    public class StatusIconViewModel : AbstractViewModel
    {
        private StatusIconState _iconState;
        private string _additionalText;
        private bool _isVisible;
        private StatusIconState _borderState;

        public StatusIconViewModel()
        {
            IconState = StatusIconState.Unlit;
            BorderState = StatusIconState.Unlit;
            AdditionalText = string.Empty;
            IsVisible = true;
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public StatusIconState IconState
        {
            get => _iconState;
            set => SetProperty(ref _iconState, value);
        }

        public StatusIconState BorderState
        {
            get => _borderState;
            set => SetProperty(ref _borderState, value);
        }

        public string AdditionalText
        {
            get => _additionalText;
            set => SetProperty(ref _additionalText, value);
        }
    }
}