namespace SecondMonitor.ViewModels.Track.MapView
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using NLog;

    using SecondMonitor.Contracts.Session;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Snapshot.Drivers;
    using SecondMonitor.ViewModels.Colors;
    using SecondMonitor.ViewModels.Factory;

    public class DefaultSituationOverviewViewModel : AbstractViewModel, ISituationOverviewViewModel
    {
        public const string ViewModelLayoutName = "Track Map";

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IViewModelFactory _viewModelFactory;
        private readonly IClassColorProvider _classColorProvider;
        private readonly Stopwatch _driversRecheckWatch;

        private string _informationText;
        private bool _animateDrivers;
        private bool _showMultiClassIndicator;
        private int _currentCarUpdateIndex;
        private bool _isPitReturnPredictionVisible;

        public DefaultSituationOverviewViewModel(IViewModelFactory viewModelFactory, IClassColorProvider classColorProvider, double layoutLength,  bool animateDrivers, int driversUpdatePerTick)
        {
            _currentCarUpdateIndex = 0;
            DriversUpdatedPerTick = driversUpdatePerTick;
            _driversRecheckWatch = Stopwatch.StartNew();
            MapSidePanelViewModel = viewModelFactory.Create<IMapSidePanelViewModel>();
            _viewModelFactory = viewModelFactory;
            _classColorProvider = classColorProvider;
            DriversDictionary = new Dictionary<string, DriverPositionViewModel>();
            AnimateDrivers = animateDrivers;
            LayoutLength = layoutLength;
            Drivers = new ObservableCollection<DriverPositionViewModel>();
            PitPrediction = viewModelFactory.Set("driverLongName").To("--Driver-Prediction--").Set("driverId").To("--Driver-Prediction--").Create<DriverPositionViewModel>();
            PitPrediction.LapCompletedPercentage = 0.5;
            PitPrediction.ClassIndicationColor = ColorDto.GreenColor;
        }

        public IMapSidePanelViewModel MapSidePanelViewModel { get; set; }

        public double LayoutLength { get; }

        public ObservableCollection<DriverPositionViewModel> Drivers { get; }

        public DriverPositionViewModel PitPrediction { get; }

        public string InformationText
        {
            get => _informationText;
            set => SetProperty(ref _informationText, value);
        }

        public bool AnimateDrivers
        {
            get => _animateDrivers;
            set => SetProperty(ref _animateDrivers, value);
        }

        public bool IsPitReturnPredictionVisible
        {
            get => _isPitReturnPredictionVisible;
            set => SetProperty(ref _isPitReturnPredictionVisible, value);
        }

        public int DriversUpdatedPerTick
        {
            get;
            set;
        }

        public bool ShowMultiClassIndicator
        {
            get => _showMultiClassIndicator;
            set => SetProperty(ref _showMultiClassIndicator, value);
        }

        protected Dictionary<string, DriverPositionViewModel> DriversDictionary { get; }

        public virtual List<DriverInfo> Update(SimulatorDataSet simulatorDataSet, ISessionInformationProvider sessionInformationProvider, bool usePositionInClass)
        {
            ShowMultiClassIndicator = simulatorDataSet.SessionInfo.IsMultiClass;
            return UpdateDrivers(simulatorDataSet, sessionInformationProvider, usePositionInClass);
        }

        protected List<DriverInfo> UpdateDrivers(SimulatorDataSet dataSet, ISessionInformationProvider sessionInformationProvider, bool usePositionInClass)
        {
            List<DriverInfo> unknownDrivers = new List<DriverInfo>();
            foreach (DriverInfo driverInfo in dataSet.DriversInfo.Skip(_currentCarUpdateIndex).Take(DriversUpdatedPerTick))
            {
                if (!DriversDictionary.TryGetValue(driverInfo.DriverSessionId, out DriverPositionViewModel driverPositionViewModel))
                {
                    unknownDrivers.Add(driverInfo);
                    continue;
                }

                driverPositionViewModel.IsPlayer = driverInfo.IsPlayer;
                driverPositionViewModel.Position = usePositionInClass ? driverInfo.PositionInClass : driverInfo.Position;
                driverPositionViewModel.X = driverInfo.WorldPosition.X.InMeters;
                driverPositionViewModel.Y = driverInfo.WorldPosition.Z.InMeters;
                driverPositionViewModel.LapCompletedPercentage = driverInfo.LapDistance / LayoutLength;
                driverPositionViewModel.UpdateStatus(dataSet, driverInfo);
            }

            _currentCarUpdateIndex += DriversUpdatedPerTick;

            if (_currentCarUpdateIndex >= dataSet.DriversInfo.Length)
            {
                _currentCarUpdateIndex = 0;
            }

            if (_driversRecheckWatch.Elapsed.TotalSeconds > 5)
            {
                var driversNotInSet = DriversDictionary.Keys.Where(x => dataSet.DriversInfo.Any(y => y.DriverSessionId == x) == false).ToList();
                try
                {
                    driversNotInSet.ForEach(x => RemoveDriver(DriversDictionary[x]));
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }

                _driversRecheckWatch.Restart();
            }

            if (dataSet.DriversInfo.Length < Drivers.Count)
            {
                dataSet.DriversInfo.Where(x => !DriversDictionary.ContainsKey(x.DriverSessionId)).ForEach(RemoveDriver);
            }

            return unknownDrivers;
        }

        public void UpdateCustomOutline(string driverLongName, ColorDto outlineColor)
        {
            if (!Application.Current.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => UpdateCustomOutline(driverLongName, outlineColor));
                return;
            }

            Drivers.ToList().Where(x => x.DriverLongName == driverLongName).ToList().ForEach(x => x.OutLineColor = outlineColor);
        }

        public void RemoveAllDrivers()
        {
            if (!Application.Current.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(RemoveAllDrivers);
                return;
            }

            DriversDictionary.Clear();
            Drivers.Clear();
        }

        public void RemoveDriver(IDriverInfo driver)
        {
            if (!Application.Current.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => RemoveDriver(driver));
                return;
            }

            if (DriversDictionary.TryGetValue(driver.DriverSessionId, out DriverPositionViewModel driverPositionViewModel))
            {
                DriversDictionary.Remove(driver.DriverSessionId);
                Drivers.Remove(driverPositionViewModel);
            }
        }

        public void RemoveDriver(DriverPositionViewModel driverPositionViewModel)
        {
            if (!Application.Current.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => RemoveDriver(driverPositionViewModel));
                return;
            }

            DriversDictionary.Remove(driverPositionViewModel.DriverId);
            Drivers.Remove(driverPositionViewModel);
        }

        public void AddDriver(IDriverInfo driver)
        {
            AddDriver(driver, null);
        }

        public void AddDriver(IDriverInfo driver, ColorDto customOutline)
        {
            if (Application.Current == null)
            {
                return;
            }

            if (!Application.Current.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => AddDriver(driver, customOutline));
                return;
            }

            if (string.IsNullOrEmpty(driver.DriverSessionId))
            {
                return;
            }

            if (DriversDictionary.ContainsKey(driver.DriverSessionId))
            {
                RemoveDriver(driver);
            }

            DriverPositionViewModel driverPositionViewModel = _viewModelFactory.Set("driverLongName").To(driver.DriverLongName).Set("driverId").To(driver.DriverSessionId).Create<DriverPositionViewModel>();
            driverPositionViewModel.IsSafetyCar = driver.IsSafetyCar;
            driverPositionViewModel.IsPlayer = driver.IsPlayer;
            driverPositionViewModel.Position = driver.PositionInClass;
            driverPositionViewModel.ClassIndicationColor = _classColorProvider.GetColorForClass(driver.CarClassId);
            driverPositionViewModel.OutLineColor = customOutline;

            Drivers.Add(driverPositionViewModel);
            DriversDictionary.Add(driver.DriverSessionId, driverPositionViewModel);
        }

        public void UpdatePitStopReturnPrediction(PitReturnPrediction pitReturnPrediction, bool usePositionInClass)
        {
            if (pitReturnPrediction.PitTime == TimeSpan.Zero)
            {
                return;
            }

            double newLapCompletedPercentage = pitReturnPrediction.ReturnLapDistance / LayoutLength;
            if (!(Math.Abs(newLapCompletedPercentage - PitPrediction.LapCompletedPercentage) > 0.01))
            {
                return;
            }

            PitPrediction.LapCompletedPercentage = newLapCompletedPercentage;
            PitPrediction.Position = usePositionInClass ? pitReturnPrediction.ClassReturnPosition.Position : pitReturnPrediction.OverallReturnPosition.Position;
            PitPrediction.X = pitReturnPrediction.ReturnWorldPos.X.InMeters;
            PitPrediction.Y = pitReturnPrediction.ReturnWorldPos.Z.InMeters;
        }
    }
}