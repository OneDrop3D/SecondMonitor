namespace SecondMonitor.Timing.Application.LapTimings.ViewModel
{
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows;
    using Common.SessionTiming.Drivers;
    using Common.SessionTiming.Drivers.Lap;
    using DataModel.BasicProperties;

    using SecondMonitor.DataModel.Extensions;

    using SimdataManagement.DriverPresentation;
    using ViewModels;
    using ViewModels.Factory;

    public class DriverLapsViewModel : AbstractViewModel
    {
        private readonly DriverPresentationsManager _driverPresentationsManager;
        private readonly IViewModelFactory _viewModelFactory;
        private ColorDto _outLineColor;
        private bool _hasCustomOutline;
        private bool _isPlayer;
        private string _driverName;
        private DriverTiming _driverTiming;

        public DriverLapsViewModel(DriverTiming driverTiming, DriverPresentationsManager driverPresentationsManager, IViewModelFactory viewModelFactory)
        {
            Laps = new ObservableCollection<LapViewModel>();
            _driverPresentationsManager = driverPresentationsManager;
            _viewModelFactory = viewModelFactory;

            DriverTiming = driverTiming;
            RatingViewModel = viewModelFactory.Create<RatingViewModel>();
            RatingViewModel.FromModel(driverTiming.DriverInfo.RatingInfo);
        }

        public RatingViewModel RatingViewModel { get; }

        public DriverTiming DriverTiming
        {
            get => _driverTiming;
            set => SetProperty(ref _driverTiming, value, (_, __) => InitializeDriverProperties());
        }

        public ObservableCollection<LapViewModel> Laps
        {
            get;
        }

        public ColorDto OutLineColor
        {
            get => _outLineColor;
            set
            {
                _outLineColor = value;
                _driverPresentationsManager.SetOutLineColor(DriverTiming.DriverInfo.DriverLongName, value);
                NotifyPropertyChanged();
            }
        }

        public bool IsPlayer
        {
            get => _isPlayer;
            set => SetProperty(ref _isPlayer, value);
        }

        public bool HasCustomOutline
        {
            get => _hasCustomOutline;
            set
            {
                _hasCustomOutline = value;
                _driverPresentationsManager.SetOutLineColorEnabled(DriverTiming.DriverLongName, value);
                NotifyPropertyChanged();
            }
        }

        public string DriverName
        {
            get => _driverName;
            private set => SetProperty(ref _driverName, value);
        }

        public void AddLap(ILapInfo lapInfo)
        {
            if (!Application.Current.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => AddLap(lapInfo));
                return;
            }

            var newLapViewModel = _viewModelFactory.Set<ILapInfo>().To(lapInfo).Create<LapViewModel>();
            if (Laps.Any() && Laps.Last().LapNumber == newLapViewModel.LapNumber)
            {
                Laps.Remove(Laps.Last());
            }

            Laps.Add(newLapViewModel);
        }

        public void RemoveLap(ILapInfo lapInfo)
        {
            if (!Application.Current.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => RemoveLap(lapInfo));
                return;
            }

            Laps.Where(x => x.LapInfo.LapGuid == lapInfo.LapGuid).ToList().ForEach(x => Laps.Remove(x));
        }

        public void RefreshAllLaps()
        {
            Laps.ForEach(x => x.RefreshInfo());
        }

        private void InitializeDriverProperties()
        {
            HasCustomOutline = _driverPresentationsManager.IsCustomOutlineEnabled(DriverTiming.DriverLongName);
            OutLineColor = _driverPresentationsManager.TryGetOutLineColor(DriverTiming.DriverLongName, out ColorDto color) ? color : null;
            BuildLapsViewModel();
        }

        private void BuildLapsViewModel()
        {
            if (DriverTiming == null)
            {
                return;
            }

            Laps.Clear();

            foreach (var lap in DriverTiming.Laps)
            {
                if (Laps.Any() && Laps.Last().LapNumber == lap.LapNumber)
                {
                    Laps.Remove(Laps.Last());
                }

                Laps.Add(_viewModelFactory.Set<ILapInfo>().To(lap).Create<LapViewModel>());
            }
        }
    }
}