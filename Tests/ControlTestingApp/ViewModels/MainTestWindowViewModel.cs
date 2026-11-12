namespace ControlTestingApp.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Windows.Input;
    using System.Xml.Serialization;

    using Moq;

    using Ninject;
    using SecondMonitor.Bootstrapping;
    using SecondMonitor.Contracts.Commands;
    using SecondMonitor.Contracts.NInject;
    using SecondMonitor.Contracts.PitStatistics;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Snapshot.Drivers;
    using SecondMonitor.DataModel.Summary.FuelConsumption;
    using SecondMonitor.Rating.Application.Championship.ViewModels.Creation.Calendar.Predefined;
    using SecondMonitor.Rating.Application.Championship.ViewModels.Overview;
    using SecondMonitor.Rating.Common.Championship.Calendar.Templates.CalendarGroups;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.CarStatus;
    using SecondMonitor.ViewModels.CarStatus.FuelStatus;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.FuelConsumption;
    using SecondMonitor.ViewModels.FuelConsumption.FuelStrategy;
    using SecondMonitor.ViewModels.PitBoard;
    using SecondMonitor.ViewModels.SessionEvents;
    using SecondMonitor.ViewModels.Settings;
    using SecondMonitor.ViewModels.Settings.ViewModel;
    using SecondMonitor.ViewModels.StatusIcon;
    using SecondMonitor.ViewModels.Text;

    public class MainTestWindowViewModel : AbstractViewModel
    {
        private Mock<ISettingsProvider> _settingsProviderMock;
        private DisplaySettingsViewModel _displaySettingsViewModel;
        private IFuelStrategyController _fuelStrategyController;
        private Mock<ISessionEventProvider> _sessionEventProviderMock;
        private Mock<IPitStopStatisticProvider> _pitStopStatisticsProviderMock;

        public MainTestWindowViewModel()
        {
            var kernel = new StandardKernel();
            kernel.LoadCore();
            IViewModelFactory viewModelFactory = kernel.Get<IViewModelFactory>();

            UpdateFuelStateCommand = new RelayCommand(UpdateFuelState);

            _sessionEventProviderMock = new Mock<ISessionEventProvider>();
            kernel.Rebind<ISessionEventProvider>().ToConstant(_sessionEventProviderMock.Object);

            _pitStopStatisticsProviderMock = new Mock<IPitStopStatisticProvider>();
            kernel.Rebind<IPitStopStatisticProvider>().ToConstant(_pitStopStatisticsProviderMock.Object);

            Mock<IPaceProvider> paceProviderMock = new Mock<IPaceProvider>();
            kernel.Rebind<IPaceProvider>().ToConstant(paceProviderMock.Object);

            CalendarTemplateGroupViewModel = viewModelFactory.Create<PredefinedCalendarSelectionViewModel>();
            CalendarTemplateGroupViewModel.FromModel(AllGroups.MainGroup);

            ClearToRejoinBoardViewModel = viewModelFactory.Create<ClearToRejoinBoardViewModel>();

            PitEntrySpeedBoardViewModel = viewModelFactory.Create<PitEntrySpeedBoardViewModel>();

            SafetyCarPitBoardViewModel = viewModelFactory.Create<SafetyCarPitBoardViewModel>();
            SafetyCarPitBoardViewModel.AdditionalText = "Ending";
            SafetyCarPitBoardViewModel.AdditionalTextState = StatusIconState.Ok;
            SafetyCarPitBoardViewModel.SafetyCarIconState = StatusIconState.Ok;

            PitReturnBoardViewModel = viewModelFactory.Create<PitReturnBoardViewModel>();

            YellowAheadPitBoardViewModel = viewModelFactory.Create<YellowAheadPitBoardViewModel>();
            YellowAheadPitBoardViewModel.SetSectorInformation("Sector 1", 
                new List<DriverWithClassModel>()
                {
                    new DriverWithClassModel(5, "Driver fafa", "Gt3", true),
                    new DriverWithClassModel(16, "Driver Fafa23", "Gt4", true),
                    new DriverWithClassModel(16, "Driver", "Gt3", true)
                });

            YellowAheadPitBoardViewModel.SetSectorInformation("Sector 1", 
                new List<DriverWithClassModel>()
                {
                    new DriverWithClassModel(16, "Driver Fafa23", "Gt4", true),
                });

            _displaySettingsViewModel = viewModelFactory.Create<DisplaySettingsViewModel>();
            _displaySettingsViewModel.VolumeUnits = VolumeUnits.Liters;
            _settingsProviderMock = new Mock<ISettingsProvider>();
            _settingsProviderMock.Setup(x => x.DisplaySettingsViewModel).Returns(_displaySettingsViewModel);
            FuelOverviewTestViewModel = new FuelOverviewTestViewModel()
            {
                RefuelingWindowState = RefuelingWindowState.WithContingency,

                DisplaySettingsViewModel = _displaySettingsViewModel,
                FuelConsumptionMonitor = new FuelConsumptionMonitor(),
                FuelPlannerViewModel = new FuelPlannerViewModel(_settingsProviderMock.Object, viewModelFactory),
                FuelStrategiesViewModel = viewModelFactory.Create<FuelStrategiesViewModel>(),
                HybridOverviewViewModel = new HybridOverviewViewModel(viewModelFactory)
                {
                    IsVisible = true,
                    LapToFullEmptyLabel = "To Empty: ",
                    LapsUntilEmptyFormatted = "3.2"
                }
            };

            FuelOverviewTestViewModel.FuelConsumptionMonitor.Reset();

            List<IDriverInfo> driverInfos = new List<IDriverInfo>()
            {
                new DriverInfo()
                {
                    DriverShortName = "Driver 1",
                    DistanceToPlayer = 342,
                },
                new DriverInfo()
                {
                    DriverShortName = "Driver 2",
                    DistanceToPlayer = 342,
                },
                new DriverInfo()
                {
                    DriverShortName = "Driver 3",
                    DistanceToPlayer = 342,
                },

                new DriverInfo()
                {
                    DriverShortName = "Driver 4",
                    DistanceToPlayer = 342,
                },
                new DriverInfo()
                {
                    DriverShortName = "Driver 5",
                    DistanceToPlayer = 342,
                },
            };

            ClearToRejoinBoardViewModel.FromModel(driverInfos);

            SequenceViewTestViewModel = new SequenceViewTestViewModel();
            TrophyViewModel = new TrophyViewModel()
            {
                DriverName = "Fooo Foookovic",
                Position = 3,
            };

            IPitStopsViewModel pitstopViewModel = SetupFuelOverviewTestViewModel();
            TypeMatchingConstructorArgument<IFuelOverviewViewModel> constructorArgument =
                new TypeMatchingConstructorArgument<IFuelOverviewViewModel>((_, __) => FuelOverviewTestViewModel);
            _fuelStrategyController = kernel.Get<IFuelStrategyController>(constructorArgument);
            _fuelStrategyController.StartControllerAsync();
            FuelOverviewTestViewModel.FuelPlannerViewModel.IsVisible = true;
            FuelOverviewTestViewModel.PitStopsViewModel = pitstopViewModel;

            PitDurationBoardViewModel = new PitDurationBoardViewModel()
            {
                AveragePitStallDuration = "20.3",
                AveragePitStopDuration = string.Empty,
                PitStallDuration = "2.3",
                PitStopDuration = "12.5",
                PitStallDurationStatus = PitInfoBriefDescriptionKind.PitStopFasterThanPlayer,
                PitStopDurationStatus = PitInfoBriefDescriptionKind.PitStopSlowerThanPlayer,
            };
        }

        public PredefinedCalendarSelectionViewModel CalendarTemplateGroupViewModel { get; }

        public SequenceViewTestViewModel SequenceViewTestViewModel { get; }

        public ClearToRejoinBoardViewModel ClearToRejoinBoardViewModel { get; }

        public PitEntrySpeedBoardViewModel PitEntrySpeedBoardViewModel { get; }
        public PitDurationBoardViewModel PitDurationBoardViewModel { get; }

        public PitReturnBoardViewModel PitReturnBoardViewModel { get; }

        public SafetyCarPitBoardViewModel SafetyCarPitBoardViewModel { get; }

        public YellowAheadPitBoardViewModel YellowAheadPitBoardViewModel { get; }

        public FuelOverviewTestViewModel FuelOverviewTestViewModel { get; }

        public ColorTestingViewModel ColorTestingViewModel { get; } = new ColorTestingViewModel();

        public TrophyViewModel TrophyViewModel { get; }

        public ICommand UpdateFuelStateCommand { get; }

        private IPitStopsViewModel SetupFuelOverviewTestViewModel()
        {
            string fuelConsumptionFile = TestResources.FuelConsumption;

            var serializer = new XmlSerializer(typeof(OverallFuelConsumptionHistory));
            OverallFuelConsumptionHistory result;

            using (TextReader reader = new StringReader(fuelConsumptionFile))
            {
                result = (OverallFuelConsumptionHistory)serializer.Deserialize(reader);
            }

            StandardPitStopViewModel pitStopViewModel = new StandardPitStopViewModel(_settingsProviderMock.Object);
            pitStopViewModel.IsPitStopCountVisible = true;
            pitStopViewModel.PitStopCount = 1;
            pitStopViewModel.FuelToAddLastStop = Volume.FromLiters(20);
            pitStopViewModel.FuelForNextPitStop = Volume.FromLiters(23);
            pitStopViewModel.IsFuelToAddVisible = true;
            pitStopViewModel.NextPitStopInfo = $"Pit in 5 laps";
            pitStopViewModel.IsNextPitStopInfoVisible = true;
            pitStopViewModel.FuelForNextPitStopLabel = "Add (Pit Lap):";
            /*pitStopViewModel.CurrentStintTime = "12:36min";
            pitStopViewModel.NextStintTime = "25min";
            pitStopViewModel.IsFuelToAddPitLapVisible = true;
            pitStopViewModel.IsFuelToAddThisLapVisible = true;
            pitStopViewModel.FuelToAddThisLap = Volume.FromLiters(5);*/

            pitStopViewModel.NextPitStopFontStyle = FontStyle.Bold;
            pitStopViewModel.NextPitStopInfo = "Pit in 5 Laps";
            pitStopViewModel.IsNextPitStopInfoVisible = true;
            pitStopViewModel.IsLastStopFuelToAddVisible = true;
            /*pitStopViewModel.LastStintTime = "13min";*/

            FuelOverviewTestViewModel.PitStopsViewModel = pitStopViewModel;

            FuelOverviewTestViewModel.HybridOverviewViewModel.HybridEnduranceState = HybridEnduranceState.Warning;
            FuelOverviewTestViewModel.FuelPlannerViewModel.FromModel(result.GetTrackConsumptionHistoryEntries("AMS 2", "Le Mans-Circuit des 24 Heures du Mans", "Porsche 992 GT3 R - Low Downforce").ToList());
            FuelOverviewTestViewModel.MaximumFuel = Volume.FromLiters(100);

            FuelOverviewTestViewModel.IsVisible = true;
            FuelOverviewTestViewModel.IsFuelCalculatorButtonEnabled = true;
            FuelOverviewTestViewModel.FuelDelta = Volume.FromLiters(20);
            FuelOverviewTestViewModel.LapsDelta = -3.5;
            FuelOverviewTestViewModel.TimeDelta = TimeSpan.FromSeconds(-16);
            FuelOverviewTestViewModel.AvgPerLap = "7.23";
            FuelOverviewTestViewModel.AvgPerMinute = "1.82";
            FuelOverviewTestViewModel.IsRaceDeltaInfoShown = true;
            FuelOverviewTestViewModel.FuelRemainingLabel = "5.0 L";
            FuelOverviewTestViewModel.FuelPlannerViewModel.CalculatorForSelectedSession.RequiredMinutes = 120;
            FuelOverviewTestViewModel.FuelPlannerViewModel.CalculatorForSelectedSession.RequiredLaps = 1;
            FuelOverviewTestViewModel.FuelPlannerViewModel.CalculatorForSelectedSession.ExtraFuel = 10;
            FuelOverviewTestViewModel.FuelMarkerViewModels = new List<IFuelMarkerViewModel>()
                { new FuelToAddMarkerViewModel(_displaySettingsViewModel), new FuelAtRaceEndMarkerViewModel() };

            SimulatorDataSet lastDataSet = new SimulatorDataSet("AMS 2");
            lastDataSet.PlayerInfo.CarClassId = "GT3_Gen2_LD";
            lastDataSet.PlayerInfo.CarInfo.FuelSystemInfo.FuelRemaining = Volume.FromLiters(2);
            lastDataSet.PlayerInfo.CarInfo.FuelSystemInfo.FuelCapacity = Volume.FromLiters(50);
            lastDataSet.SessionInfo.TrackInfo.LayoutLength = Distance.FromMeters(13627.017578125);
            lastDataSet.SessionInfo.PitWindow.PitWindowState = PitWindowState.BeforePitWindow;
            lastDataSet.SessionInfo.PitWindow.PitWindowStart = 10;
            lastDataSet.SessionInfo.PitWindow.PitWindowEnd = 30;

            _sessionEventProviderMock.Setup(x => x.LastDataSet).Returns(lastDataSet);
            return pitStopViewModel;
        }

        private void UpdateFuelState()
        {
            SimulatorDataSet lastDataSet = new SimulatorDataSet("AMS 2");
            lastDataSet.PlayerInfo.CarClassId = "GT3_Gen2_LD";
            lastDataSet.SessionInfo.SessionPhase = SessionPhase.Green;
            lastDataSet.SessionInfo.SessionType = SessionType.Race;
            lastDataSet.PlayerInfo.CarInfo.FuelSystemInfo.FuelRemaining = Volume.FromLiters(2);
            lastDataSet.PlayerInfo.CarInfo.FuelSystemInfo.FuelCapacity = Volume.FromLiters(50);
            lastDataSet.SessionInfo.TrackInfo.LayoutLength = Distance.FromMeters(13627.017578125);
            _fuelStrategyController.ApplyDataSet(lastDataSet);
        }
    }
}