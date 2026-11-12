namespace ControlTestingApp.ViewModels
{
    using SecondMonitor.ViewModels.StatusIcon;

    public class StatusIconTcVm
    {
        private int _iconStatusInt;
        private int _borderStatusInt;

        public StatusIconTcVm()
        {
            StatusIconViewModel = new StatusIconViewModel()
            {
                IconState = StatusIconState.Ok,
                AdditionalText = "0.5",
                IsVisible = true,
            };
        }

        public StatusIconViewModel StatusIconViewModel { get; set; }

        public int IconStatusInt
        {
            get => _iconStatusInt;
            set
            {
                _iconStatusInt = value;
                StatusIconViewModel.AdditionalText = (value * 10).ToString();
                switch (_iconStatusInt)
                {
                    case 1:
                        StatusIconViewModel.IconState = StatusIconState.Ok;
                        break;
                    case 2:
                        StatusIconViewModel.IconState = StatusIconState.Warning;
                        break;
                    case 3:
                        StatusIconViewModel.IconState = StatusIconState.Error;
                        break;
                    case 4:
                        StatusIconViewModel.IconState = StatusIconState.Information;
                        break;
                    default:
                        StatusIconViewModel.IconState = StatusIconState.Unlit;
                        break;
                }
            }
        }

        public int BorderStatusInt
        {
            get => _borderStatusInt;
            set
            {
                _borderStatusInt = value;
                switch (_borderStatusInt)
                {
                    case 1:
                        StatusIconViewModel.BorderState = StatusIconState.Ok;
                        break;
                    case 2:
                        StatusIconViewModel.BorderState = StatusIconState.Warning;
                        break;
                    case 3:
                        StatusIconViewModel.BorderState = StatusIconState.Error;
                        break;
                    case 4:
                        StatusIconViewModel.BorderState = StatusIconState.Information;
                        break;
                    default:
                        StatusIconViewModel.BorderState = StatusIconState.Unlit;
                        break;
                }
            }
        }
    }
}