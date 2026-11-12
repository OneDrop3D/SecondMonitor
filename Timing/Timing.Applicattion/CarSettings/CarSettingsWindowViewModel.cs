namespace SecondMonitor.Timing.Application.CarSettings
{
    using SimdataManagement.ViewModel;
    using ViewModels;
    using ViewModels.Factory;

    public class CarSettingsWindowViewModel : AbstractViewModel
    {
        private CarModelPropertiesViewModel _carModelPropertiesViewModel;

        public CarSettingsWindowViewModel(IViewModelFactory viewModelFactory)
        {
            LastCarDataViewMode = viewModelFactory.Create<LastCarDataViewModel>();
        }

        public LastCarDataViewModel LastCarDataViewMode { get; }

        public CarModelPropertiesViewModel CarModelPropertiesViewModel
        {
            get => _carModelPropertiesViewModel;
            set
            {
                _carModelPropertiesViewModel = value;
                NotifyPropertyChanged();
            }
        }
    }
}