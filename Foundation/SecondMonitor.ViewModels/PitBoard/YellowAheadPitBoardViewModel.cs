namespace SecondMonitor.ViewModels.PitBoard
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    using SecondMonitor.ViewModels.Factory;

    public class YellowAheadPitBoardViewModel : AbstractViewModel
    {
        private readonly IViewModelFactory _viewModelFactory;
        private string _distance;
        private string _sectorInformation;
        private bool _showDriverInformation;
        private bool _isSectorAhead;
        private bool _isSectorAheadArrowShown;

        public YellowAheadPitBoardViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
            SecondaryDrivers = new ObservableCollection<DriverWithClassModelViewModel>();
            PrimaryDriver = _viewModelFactory.Create<DriverWithClassModelViewModel>();
            Distance = "200 m";
        }

        public DriverWithClassModelViewModel PrimaryDriver { get; }

        public string Distance
        {
            get => _distance;
            set => SetProperty(ref _distance, value);
        }

        public ObservableCollection<DriverWithClassModelViewModel> SecondaryDrivers { get; }

        public string SectorInformation
        {
            get => _sectorInformation;
            set => SetProperty(ref _sectorInformation, value);
        }

        public bool ShowDriverInformation
        {
            get => _showDriverInformation;
            set => SetProperty(ref _showDriverInformation, value);
        }

        public bool IsSectorAhead
        {
            get => _isSectorAhead;
            set => SetProperty(ref _isSectorAhead, value);
        }

        public bool IsSectorAheadArrowShown
        {
            get => _isSectorAheadArrowShown;
            set => SetProperty(ref _isSectorAheadArrowShown, value);
        }

        public void SetClosestDriver(DriverWithClassModel closesDriver, string distance)
        {
            PrimaryDriver.FromModel(closesDriver);
            Distance = distance;
            ShowDriverInformation = true;
        }

        public void SetSectorInformation(string sectorInformation, List<DriverWithClassModel> drivers)
        {
            SectorInformation = sectorInformation;
            for (int i = 0; i < drivers.Count && i < SecondaryDrivers.Count; i++)
            {
                SecondaryDrivers[i].FromModel(drivers[i]);
            }

            while (drivers.Count > SecondaryDrivers.Count)
            {
                DriverWithClassModelViewModel newViewModel = _viewModelFactory.Create<DriverWithClassModelViewModel>();
                newViewModel.FromModel(drivers[SecondaryDrivers.Count]);
                SecondaryDrivers.Add(newViewModel);
            }

            while (drivers.Count < SecondaryDrivers.Count)
            {
                SecondaryDrivers.RemoveAt(SecondaryDrivers.Count - 1);
            }

            ShowDriverInformation = false;
        }
    }
}
