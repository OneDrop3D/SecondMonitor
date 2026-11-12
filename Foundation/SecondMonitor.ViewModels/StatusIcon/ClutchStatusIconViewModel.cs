namespace SecondMonitor.ViewModels.StatusIcon
{
    public class ClutchStatusIconViewModel : DamageStatusIconViewModel
    {
        private string _temperature;

        public string Temperature
        {
            get => _temperature;
            set => SetProperty(ref _temperature, value);
        }
    }
}