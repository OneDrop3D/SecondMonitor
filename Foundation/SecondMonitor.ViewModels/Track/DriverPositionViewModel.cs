namespace SecondMonitor.ViewModels.Track
{
    using Contracts.Session;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using Factory;
    using PitStop;
    using Settings;
    using Settings.ViewModel;

    public class DriverPositionViewModel : AbstractViewModel
    {
        private readonly ViewModelFactory _viewModelFactory;
        private readonly ISessionInformationProvider _sessionInformationProvider;
        private readonly DisplaySettingsViewModel _displaySettingsViewModel;
        private double _x;
        private double _y;
        private double _lapCompletedPercentage;
        private int _position;
        private bool _isPlayer;
        private ColorDto _classIndicationColor;
        private ColorDto _outLineColor;
        private DriverState _driverState;
        private AbstractViewModel _pitStopsVisualization;
        private bool _isSafetyCar;

        public DriverPositionViewModel(string driverLongName, string driverId, ViewModelFactory viewModelFactory, ISessionInformationProvider sessionInformationProvider, ISettingsProvider settingsProvider)
        {
            _viewModelFactory = viewModelFactory;
            _sessionInformationProvider = sessionInformationProvider;
            PitStopsVisualization = new BlankPitStopsVisualization();
            DriverLongName = driverLongName;
            DriverId = driverId;
            _displaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
        }

        public string DriverLongName { get; }
        public string DriverId { get; }

        public double X
        {
            get => _x;
            set => SetProperty(ref _x, value);
        }

        public double Y
        {
            get => _y;
            set => SetProperty(ref _y, value);
        }

        public int Position
        {
            get => _position;
            set => SetProperty(ref _position, value);
        }

        public bool IsPlayer
        {
            get => _isPlayer;
            set => SetProperty(ref _isPlayer, value);
        }

        public ColorDto ClassIndicationColor
        {
            get => _classIndicationColor;
            set => SetProperty(ref _classIndicationColor, value);
        }

        public ColorDto OutLineColor
        {
            get => _outLineColor;
            set => SetProperty(ref _outLineColor, value);
        }

        public double LapCompletedPercentage
        {
            get => _lapCompletedPercentage;
            set => SetProperty(ref _lapCompletedPercentage, value);
        }

        public DriverState DriverState
        {
            get => _driverState;
            set => SetProperty(ref _driverState, value);
        }

        public AbstractViewModel PitStopsVisualization
        {
            get => _pitStopsVisualization;
            set => SetProperty(ref _pitStopsVisualization, value);
        }

        public bool IsSafetyCar
        {
            get => _isSafetyCar;
            set => SetProperty(ref _isSafetyCar, value);
        }

        public void UpdateStatus(SimulatorDataSet dataSet, IDriverInfo driverInfo)
        {
            IsSafetyCar = driverInfo.IsSafetyCar;
            UpdatePitsInformation(dataSet, driverInfo);
            switch (driverInfo.FinishStatus)
            {
                case DriverFinishStatus.Finished:
                    DriverState = DriverState.Finished;
                    return;
                case DriverFinishStatus.Dnf or DriverFinishStatus.Dq or DriverFinishStatus.Dnq when driverInfo.Speed.InKph < 5:
                    DriverState = driverInfo.InPits ? DriverState.InPits : DriverState.Dnf;
                    return;
            }

            if (driverInfo.InPits)
            {
                DriverState = driverInfo.Speed.InKph <= 10 ? DriverState.InPits : DriverState.InPitsMoving;
                return;
            }

            if (driverInfo.IsCausingYellow)
            {
                DriverState = DriverState.Yellow;
                return;
            }

            if (driverInfo.IsBeingLappedByPlayer || (dataSet.SessionInfo.SessionType != SessionType.Race && !_sessionInformationProvider.IsDriverOnValidLap(driverInfo)))
            {
                DriverState = DriverState.Lapped;
                return;
            }

            if (driverInfo.IsLappingPlayer)
            {
                DriverState = DriverState.Lapping;
                return;
            }

            DriverState = DriverState.Normal;
        }

        private void UpdatePitsInformation(SimulatorDataSet dataSet, IDriverInfo driverInfo)
        {
            if (!_displaySettingsViewModel.MapDisplaySettingsViewModel.ShowPitsInformation
                || dataSet.SessionInfo.SessionType != SessionType.Race
                || DriverState == DriverState.Dnf
                || DriverState == DriverState.InPits)
            {
                CheckPitStopsVisualization<BlankPitStopsVisualization>();
                return;
            }

            bool isInPitWindow = dataSet.SessionInfo.PitWindow.PitWindowState == PitWindowState.InPitWindow;
            if (isInPitWindow)
            {
                var pitWindowVisualization = CheckPitStopsVisualization<PitWindowVisualization>();
                pitWindowVisualization.IsPitStopCompleted = _sessionInformationProvider.HasDriverCompletedPitWindowStop(driverInfo);
                return;
            }

            bool isPitRace = dataSet.SessionInfo.PitWindow.PitWindowState != PitWindowState.None;
            if (isPitRace || _isPlayer)
            {
                CheckPitStopsVisualization<BlankPitStopsVisualization>();
                return;
            }

            int? playerPitStopCount = _sessionInformationProvider.GetPlayerPitStopCount();
            if (!playerPitStopCount.HasValue)
            {
                CheckPitStopsVisualization<BlankPitStopsVisualization>();
                return;
            }

            int driverPitStopsCount = _sessionInformationProvider.GetDriverPitStopCount(driverInfo);
            int difference = driverPitStopsCount - playerPitStopCount.Value;

            var textPitStopVisualization = CheckPitStopsVisualization<TextPitStopDifferenceVisualization>();
            textPitStopVisualization.Information = difference == 0 ? string.Empty : difference.ToString("+#;-#;0");
        }

        private T CheckPitStopsVisualization<T>() where T : AbstractViewModel
        {
            if (_pitStopsVisualization is T castedPitStopsVisualization)
            {
                return castedPitStopsVisualization;
            }

            castedPitStopsVisualization = _viewModelFactory.Create<T>();
            PitStopsVisualization = castedPitStopsVisualization;

            return castedPitStopsVisualization;
        }
    }
}