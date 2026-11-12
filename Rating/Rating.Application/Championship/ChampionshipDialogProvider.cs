namespace SecondMonitor.Rating.Application.Championship
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using Common.DataModel.Championship;
    using Common.DataModel.Championship.Events;
    using Contracts.Commands;
    using Contracts.TrackMap;
    using Contracts.TrackRecords;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.TrackMap;
    using DataModel.TrackRecords;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.Rating.Application.Championship.ViewModels.Overview.ProgressChart;
    using SecondMonitor.Rating.Application.Championship.ViewModels.Overview.ResultGrid;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.Settings;
    using ViewModels.Events;
    using ViewModels.Overview;

    public class ChampionshipDialogProvider : IChampionshipDialogProvider
    {
        private readonly IWindowService _windowService;
        private readonly IViewModelFactory _viewModelFactory;
        private readonly ITrackRecordsProvider _trackRecordsProvider;
        private readonly ISettingsProvider _settingsProvider;
        private readonly IMapsLoader _mapsLoader;
        private readonly Dictionary<Guid, Window> _championshipDetailWindowsMap = new Dictionary<Guid, Window>();

        public ChampionshipDialogProvider(IWindowService windowService, IViewModelFactory viewModelFactory, ITrackRecordsProvider trackRecordsProvider, IMapsLoaderFactory mapsLoaderFactory, ISettingsProvider settingsProvider)
        {
            _windowService = windowService;
            _viewModelFactory = viewModelFactory;
            _trackRecordsProvider = trackRecordsProvider;
            _settingsProvider = settingsProvider;
            _mapsLoader = mapsLoaderFactory.Create();
        }

        public void ShowWelcomeScreen(SimulatorDataSet dataSet, ChampionshipDto championship)
        {
            EventStartingViewModel eventStartingViewModel = _viewModelFactory.Create<EventStartingViewModel>();

            EventDto currentEvent = championship.GetCurrentOrLastEvent();
            SessionDto currentSession = currentEvent.Sessions[championship.CurrentSessionIndex];
            eventStartingViewModel.EventTitleViewModel.FromModel((championship, currentEvent, currentSession));

            eventStartingViewModel.Screens.Add(CreateTrackOverviewViewModel(dataSet, championship));
            eventStartingViewModel.Screens.AddRange(CreateDriversStandingOverviewViewModel(championship));

            ClassIdentifier classIdentifiers = championship.GetOverviewClassIdentifiers();
            ManufacturersChampionshipDto manufacturersChampionshipDto = championship.GetOrCreateManufacturerChampionship(classIdentifiers);
            TeamsChampionshipDto teamsChampionshipDto = championship.GetOrCreateTeamChampionship(classIdentifiers);

            if (championship.IsTeamScoringEnabled && teamsChampionshipDto.TeamDtos.Count > 2)
            {
                eventStartingViewModel.Screens.Add(CreateTeamsStandingOverviewViewModel(teamsChampionshipDto));
                if (championship.IsTrackingMultipleClasses)
                {
                    championship.TeamsChampionshipDtos.Where(x => !string.IsNullOrWhiteSpace(x.ClassName))
                        .ForEach(x => eventStartingViewModel.Screens.Add(CreateTeamsStandingOverviewViewModel(x)));
                }
            }

            if (championship.IsManufacturerScoringEnabled && manufacturersChampionshipDto.Manufacturers.Count > 2)
            {
                eventStartingViewModel.Screens.Add(CreateManufacturersStandingOverviewViewModel(manufacturersChampionshipDto));
                if (championship.IsTrackingMultipleClasses)
                {
                    championship.ManufacturersChampionshipDtos.Where(x => !string.IsNullOrWhiteSpace(x.ClassName))
                        .ForEach(x => eventStartingViewModel.Screens.Add(CreateManufacturersStandingOverviewViewModel(x)));
                }
            }

            Window window = _windowService.OpenWindow(eventStartingViewModel, "Event Starting", WindowState.Maximized, SizeToContent.Manual, WindowStartupLocation.CenterOwner);
            eventStartingViewModel.CloseCommand = new RelayCommand(() => CloseWindow(window));
        }

        public void ShowLastEvenResultWindow(ChampionshipDto championship)
        {
            SessionCompletedViewModel sessionCompletedViewmodel = _viewModelFactory.Create<SessionCompletedViewModel>();
            (EventDto eventDto, SessionDto sessionDto) = championship.GetLastSessionWithResults();
            SessionResultDto lastResult = sessionDto.SessionResult;
            if (lastResult == null)
            {
                return;
            }

            sessionCompletedViewmodel.Title = "Session Completed";

            PodiumViewModel podiumViewModel = _viewModelFactory.Create<PodiumViewModel>();
            podiumViewModel.FromModel(lastResult);
            sessionCompletedViewmodel.Screens.Add(podiumViewModel);

            DriversSessionResultViewModel driversFinishViewModel = _viewModelFactory.Create<DriversSessionResultViewModel>();
            driversFinishViewModel.FromModel(lastResult);
            driversFinishViewModel.Header = "Session Driver Results";
            sessionCompletedViewmodel.Screens.Add(driversFinishViewModel);

            DriversNewStandingsViewModel driversNewStandingsViewModel = _viewModelFactory.Create<DriversNewStandingsViewModel>();
            driversNewStandingsViewModel.EventTitleViewModel.FromModel((championship, eventDto, sessionDto));
            driversNewStandingsViewModel.EventTitleViewModel.ChampionshipName = "Current Drivers Standings";
            driversNewStandingsViewModel.FromModel(lastResult);
            sessionCompletedViewmodel.Screens.Add(driversNewStandingsViewModel);

            DriverDto player = championship.Drivers.First(x => x.IsPlayer);
            // Add overview for all classes if championship is tracking all classes
            ClassIdentifier classIdentifier = championship.GetOverviewClassIdentifiers();
            AddTeamsResults(championship, lastResult, eventDto, sessionDto, sessionCompletedViewmodel, classIdentifier);
            AddManufacturingResults(championship, lastResult, eventDto, sessionDto, sessionCompletedViewmodel, classIdentifier);
            AddClassStandings(championship, lastResult, sessionCompletedViewmodel, eventDto, sessionDto);

            if (championship.ChampionshipState == ChampionshipState.Finished)
            {
                AddFinalStandings(championship, player, sessionCompletedViewmodel, classIdentifier);
            }

            Window window = _windowService.OpenWindow(sessionCompletedViewmodel, "Session Completed", WindowState.Maximized, SizeToContent.Manual, WindowStartupLocation.CenterOwner);
            sessionCompletedViewmodel.CloseCommand = new RelayCommand(() => CloseWindow(window));
        }

        private void AddFinalStandings(ChampionshipDto championship, DriverDto player, SessionCompletedViewModel sessionCompletedViewmodel, ClassIdentifier classIdentifier)
        {
            if (player.Position <= 3)
            {
                TrophyViewModel trophyViewModel = _viewModelFactory.Create<TrophyViewModel>();
                trophyViewModel.Position = player.Position;
                trophyViewModel.DriverName = player.LastUsedName;
                sessionCompletedViewmodel.Screens.Add(trophyViewModel);
            }

            DriversStandingOverviewViewModel championshipFinalStandings = _viewModelFactory.Create<DriversStandingOverviewViewModel>();
            championshipFinalStandings.Header = "Final Driver Standing: ";
            championshipFinalStandings.FromModel(championship.Drivers.OrderBy(x => x.Position));
            sessionCompletedViewmodel.Screens.Add(championshipFinalStandings);

            if (!championship.IsTrackingMultipleClasses)
            {
                DriversResultGridViewModel driversResultGridViewModel = _viewModelFactory.Create<DriversResultGridViewModel>();
                driversResultGridViewModel.FromModel(championship);
                sessionCompletedViewmodel.Screens.Add(driversResultGridViewModel);

                ChampionshipProgressChartViewModel championshipProgressChartViewModel = _viewModelFactory.Create<ChampionshipProgressChartViewModel>();
                championshipProgressChartViewModel.FromModel(championship);
                sessionCompletedViewmodel.Screens.Add(championshipProgressChartViewModel);
            }

            AddTeamsFinalStandings(championship, sessionCompletedViewmodel, classIdentifier);
            AddManufacturerFinalStandings(championship, sessionCompletedViewmodel, classIdentifier);
            AddClassFinalStandings(championship, sessionCompletedViewmodel);
        }

        private void AddClassStandings(ChampionshipDto championship, SessionResultDto lastResult, SessionCompletedViewModel sessionCompletedViewmodel, EventDto eventDto, SessionDto sessionDto)
        {
            if (lastResult.ClassSessionResults.Count(x => x.ClassId != ClassIdentifier.OverviewIdentifier.ClassId) < 2)
            {
                return;
            }

            foreach (ClassSessionResultDto lastResultClassSessionResult in lastResult.ClassSessionResults)
            {
                sessionCompletedViewmodel.Screens.Add(_viewModelFactory.CreateAndApply<ClassPodiumViewModel, ClassSessionResultDto>(lastResultClassSessionResult));

                DriversClassSessionResultViewModel driversClassSessionResultViewModel = _viewModelFactory.Create<DriversClassSessionResultViewModel>();
                driversClassSessionResultViewModel.FromModel(lastResultClassSessionResult);
                driversClassSessionResultViewModel.Header = $"Session Driver Results ({lastResultClassSessionResult.ClassName})";
                sessionCompletedViewmodel.Screens.Add(driversClassSessionResultViewModel);

                DriversNewClassStandingsViewModel driverNewClassStandingViewModel = _viewModelFactory.Create<DriversNewClassStandingsViewModel>();
                driverNewClassStandingViewModel.EventTitleViewModel.FromModel((championship, eventDto, sessionDto));
                driverNewClassStandingViewModel.EventTitleViewModel.ChampionshipName = $"Current Drivers Standings ({lastResultClassSessionResult.ClassName})";
                driverNewClassStandingViewModel.FromModel(lastResultClassSessionResult);
                sessionCompletedViewmodel.Screens.Add(driverNewClassStandingViewModel);

                AddTeamsResults(championship, lastResultClassSessionResult, eventDto, sessionDto, sessionCompletedViewmodel, lastResultClassSessionResult.GetClassIdentifier());
                AddManufacturingResults(championship, lastResultClassSessionResult, eventDto, sessionDto, sessionCompletedViewmodel, lastResultClassSessionResult.GetClassIdentifier());
            }
        }

        private void AddClassFinalStandings(ChampionshipDto championship, SessionCompletedViewModel sessionCompletedViewmodel)
        {
            if (!championship.IsTrackingMultipleClasses)
            {
                return;
            }

            foreach (ClassChampionshipDto classChampionshipDto in championship.ClassChampionshipDtos)
            {
                ClassDriverDto player = classChampionshipDto.Drivers.FirstOrDefault(x => x.IsPlayer);
                if (player?.Position < 3)
                {
                    TrophyViewModel trophyViewModel = _viewModelFactory.Create<TrophyViewModel>();
                    trophyViewModel.Header = classChampionshipDto.ClassName;
                    trophyViewModel.Position = player.Position;
                    trophyViewModel.DriverName = player.DriverName;
                    sessionCompletedViewmodel.Screens.Add(trophyViewModel);
                }

                DriversClassStandingOverviewViewModel driverClassStandingViewModel = _viewModelFactory.Create<DriversClassStandingOverviewViewModel>();
                driverClassStandingViewModel.Header = $"Final Driver Standing ({classChampionshipDto.ClassName}): ";
                driverClassStandingViewModel.FromModel(classChampionshipDto.Drivers);
                sessionCompletedViewmodel.Screens.Add(driverClassStandingViewModel);

                ClassIdentifier classIdentifier = classChampionshipDto.GetClassIdentifier;

                if (player != null)
                {
                    DriversClassResultGridViewModel driversResultGridViewModel = _viewModelFactory.Create<DriversClassResultGridViewModel>();
                    driversResultGridViewModel.FromModel((championship, classIdentifier));
                    sessionCompletedViewmodel.Screens.Add(driversResultGridViewModel);

                    ChampionshipClassProgressChartViewModel championshipProgressChartViewModel = _viewModelFactory.Create<ChampionshipClassProgressChartViewModel>();
                    championshipProgressChartViewModel.FromModel((championship, classIdentifier));
                    sessionCompletedViewmodel.Screens.Add(championshipProgressChartViewModel);
                }

                AddTeamsFinalStandings(championship, sessionCompletedViewmodel, classIdentifier);
                AddManufacturerFinalStandings(championship, sessionCompletedViewmodel, classIdentifier);
            }
        }

        private void AddManufacturingResults(ChampionshipDto championship, IHaveManufacturersResults lastResult,
            EventDto eventDto, SessionDto sessionDto, SessionCompletedViewModel sessionCompletedViewmodel, ClassIdentifier classIdentifier)
        {
            ManufacturersChampionshipDto manufacturersChampionshipDto = championship.GetOrCreateManufacturerChampionship(classIdentifier);
            if (!championship.IsManufacturerScoringEnabled || manufacturersChampionshipDto.Manufacturers.Count <= 1)
            {
                return;
            }

            ManufacturersSessionResultViewModel manufacturerResultViewModel = _viewModelFactory.Create<ManufacturersSessionResultViewModel>();
            string classTitle = string.IsNullOrWhiteSpace(classIdentifier.ClassName) ? "Overall" : classIdentifier.ClassName;
            manufacturerResultViewModel.Header = $"Session Manufacturers Results ({classTitle})";
            manufacturerResultViewModel.FromModel(lastResult);

            ManufacturerNewStandingsViewModel manufacturerNewStandingsViewModel = _viewModelFactory.Create<ManufacturerNewStandingsViewModel>();
            manufacturerNewStandingsViewModel.EventTitleViewModel.FromModel((championship, eventDto, sessionDto));
            manufacturerNewStandingsViewModel.EventTitleViewModel.ChampionshipName = $"Current Manufacturers Standings ({classTitle})";
            manufacturerNewStandingsViewModel.FromModel(lastResult);

            sessionCompletedViewmodel.Screens.Add(manufacturerResultViewModel);
            sessionCompletedViewmodel.Screens.Add(manufacturerNewStandingsViewModel);
        }

        private void AddTeamsResults(ChampionshipDto championship, IHaveTeamResults lastResult,
            EventDto eventDto, SessionDto sessionDto, SessionCompletedViewModel sessionCompletedViewmodel, ClassIdentifier classIdentifier)
        {
            TeamsChampionshipDto teamsChampionshipDto = championship.GetOrCreateTeamChampionship(classIdentifier);
            if (!championship.HasTeamsChampionships || teamsChampionshipDto.TeamDtos.Count < 2)
            {
                return;
            }

            TeamsSessionResultViewModel teamResultViewModel = _viewModelFactory.Create<TeamsSessionResultViewModel>();
            string classTitle = string.IsNullOrWhiteSpace(classIdentifier.ClassName) ? "Overall" : classIdentifier.ClassName;
            teamResultViewModel.Header = $"Session Team Results ({classTitle})";
            teamResultViewModel.FromModel(lastResult);

            TeamsNewStandingsViewModel teamsNewStandingsViewModel = _viewModelFactory.Create<TeamsNewStandingsViewModel>();
            teamsNewStandingsViewModel.EventTitleViewModel.FromModel((championship, eventDto, sessionDto));
            teamsNewStandingsViewModel.EventTitleViewModel.ChampionshipName = $"Current Teams Standings ({classTitle})";
            teamsNewStandingsViewModel.FromModel(lastResult);

            sessionCompletedViewmodel.Screens.Add(teamResultViewModel);
            sessionCompletedViewmodel.Screens.Add(teamsNewStandingsViewModel);
        }

        private void AddManufacturerFinalStandings(ChampionshipDto championship, SessionCompletedViewModel sessionCompletedViewmodel, ClassIdentifier classIdentifier)
        {
            if (!championship.IsManufacturerScoringEnabled)
            {
                return;
            }

            ManufacturersChampionshipDto manufacturerChampionship = championship.GetOrCreateManufacturerChampionship(classIdentifier);
            if (manufacturerChampionship.Manufacturers.Count < 2)
            {
                return;
            }

            ManufacturerDto playerManufacturer = manufacturerChampionship.Manufacturers.FirstOrDefault(x => x.IsPlayer);
            string className = string.IsNullOrEmpty(classIdentifier.ClassName) ? "Overall" : classIdentifier.ClassName;
            if (playerManufacturer is { Position: <= 3 } && playerManufacturer.Position < manufacturerChampionship.Manufacturers.Count)
            {
                TrophyViewModel trophyViewModel = _viewModelFactory.Create<TrophyViewModel>();
                trophyViewModel.Header = string.IsNullOrEmpty(className) ? "Manufacturers Championship (Overall)" : $"Manufacturers Championship ({className})";
                trophyViewModel.Position = playerManufacturer.Position;
                trophyViewModel.DriverName = playerManufacturer.ManufacturerName;
                sessionCompletedViewmodel.Screens.Add(trophyViewModel);
            }

            ManufacturersStandingOverviewViewModel manufacturersFinalStandings = _viewModelFactory.Create<ManufacturersStandingOverviewViewModel>();
            manufacturersFinalStandings.FromModel(manufacturerChampionship);
            manufacturersFinalStandings.Header = $"Final Manufacturers Standing ({className}): ";
            sessionCompletedViewmodel.Screens.Add(manufacturersFinalStandings);
        }

        private void AddTeamsFinalStandings(ChampionshipDto championship, SessionCompletedViewModel sessionCompletedViewmodel, ClassIdentifier classIdentifier)
        {
            if (!championship.IsTeamScoringEnabled)
            {
                return;
            }

            TeamsChampionshipDto teamsChampionship = championship.GetOrCreateTeamChampionship(classIdentifier);
            if (teamsChampionship.TeamDtos.Count < 2)
            {
                return;
            }

            string className = string.IsNullOrEmpty(classIdentifier.ClassName) ? "Overall" : classIdentifier.ClassName;
            TeamDto playerTeam = teamsChampionship.TeamDtos.FirstOrDefault(x => x.IsPlayer);
            if (playerTeam is { Position: <= 3 } && playerTeam.Position < teamsChampionship.TeamDtos.Count)
            {
                TrophyViewModel trophyViewModel = _viewModelFactory.Create<TrophyViewModel>();
                trophyViewModel.Header = string.IsNullOrEmpty(className) ? "Teams Championship (Overall)" : $"Teams Championship ({className})";
                trophyViewModel.Position = playerTeam.Position;
                trophyViewModel.DriverName = playerTeam.TeamName;
                sessionCompletedViewmodel.Screens.Add(trophyViewModel);
            }
            
            TeamsStandingOverviewViewModel manufacturersFinalStandings = _viewModelFactory.Create<TeamsStandingOverviewViewModel>();
            manufacturersFinalStandings.FromModel(teamsChampionship);
            manufacturersFinalStandings.Header = $"Final Teams Standing ({className}): ";
            sessionCompletedViewmodel.Screens.Add(manufacturersFinalStandings);
        }

        private TrackOverviewViewModel CreateTrackOverviewViewModel(SimulatorDataSet dataSet, ChampionshipDto championship)
        {
            TrackInfo trackInfo = dataSet.SessionInfo.TrackInfo;
            bool hasMap = _mapsLoader.TryLoadMap(championship.SimulatorName, trackInfo.TrackFullName, out TrackMapDto trackMapDto);
            TrackOverviewViewModel trackOverviewViewModel = _viewModelFactory.Create<TrackOverviewViewModel>();
            trackOverviewViewModel.TrackName = trackInfo.TrackFullName;
            trackOverviewViewModel.LayoutLength = $"{trackInfo.LayoutLength.GetByUnit(_settingsProvider.DisplaySettingsViewModel.DistanceUnits):N2} {Distance.GetUnitsSymbol(_settingsProvider.DisplaySettingsViewModel.DistanceUnits)} ";
            if (hasMap)
            {
                trackOverviewViewModel.TrackGeometryViewModel.FromModel(trackMapDto.TrackGeometry);
            }

            if (_trackRecordsProvider.TryGetOverallBestRecord(dataSet.Source, trackInfo.TrackFullName, SessionType.Race, out RecordEntryDto recordEntry))
            {
                trackOverviewViewModel.OverallRecord.FromModel(recordEntry);
            }

            if (_trackRecordsProvider.TryGetCarBestRecord(dataSet.Source, trackInfo.TrackFullName, dataSet.PlayerInfo.CarName, SessionType.Race, out recordEntry))
            {
                trackOverviewViewModel.CarRecord.FromModel(recordEntry);
            }

            if (_trackRecordsProvider.TryGetClassBestRecord(dataSet.Source, trackInfo.TrackFullName, dataSet.PlayerInfo.CarClassName, SessionType.Race, out recordEntry))
            {
                trackOverviewViewModel.ClassRecord.FromModel(recordEntry);
            }

            return trackOverviewViewModel;
        }

        public ChampionshipDetailViewModel OpenChampionshipDetailsWindow(ChampionshipDto championship)
        {
            ChampionshipDetailViewModel detailViewModel = _viewModelFactory.Create<ChampionshipDetailViewModel>();
            detailViewModel.FromModel(championship);
            Window window = _windowService.OpenWindow(detailViewModel, "Championships Details", WindowState.Maximized, SizeToContent.Manual, WindowStartupLocation.CenterOwner, DetailWindowOnClose);

            detailViewModel.CloseCommand = new RelayCommand(() => CloseWindow(window));
            detailViewModel.ChampionshipSessionsResults.CloseCommand = new RelayCommand(() => CloseWindow(window));
            _championshipDetailWindowsMap.Add(championship.ChampionshipGlobalId, window);
            return detailViewModel;
        }

        public void CloseChampionshipDetailsWindow(ChampionshipDto championship)
        {
            if (_championshipDetailWindowsMap.TryGetValue(championship.ChampionshipGlobalId, out Window window))
            {
                window.Close();
            }
        }

        private IEnumerable<IViewModel> CreateDriversStandingOverviewViewModel(ChampionshipDto championship)
        {
            DriversStandingOverviewViewModel standingOverviewViewModel = _viewModelFactory.Create<DriversStandingOverviewViewModel>();
            standingOverviewViewModel.FromModel(championship.Drivers);
            yield return standingOverviewViewModel;

            if (!championship.IsTrackingMultipleClasses)
            {
                yield break;
            }

            foreach (ClassChampionshipDto championshipClassChampionshipDto in championship.ClassChampionshipDtos)
            {
                DriversClassStandingOverviewViewModel driverClassStandingViewModel = _viewModelFactory.Create<DriversClassStandingOverviewViewModel>();
                driverClassStandingViewModel.FromModel(championshipClassChampionshipDto.Drivers);
                driverClassStandingViewModel.Header = $"Current Driver Standings ({championshipClassChampionshipDto.ClassName})";
                yield return driverClassStandingViewModel;
            }
        }

        private ManufacturersStandingOverviewViewModel CreateManufacturersStandingOverviewViewModel(ManufacturersChampionshipDto championship)
        {
            ManufacturersStandingOverviewViewModel standingOverviewViewModel = _viewModelFactory.Create<ManufacturersStandingOverviewViewModel>();
            standingOverviewViewModel.FromModel(championship);
            return standingOverviewViewModel;
        }
        
        private TeamsStandingOverviewViewModel CreateTeamsStandingOverviewViewModel(TeamsChampionshipDto teamsChampionshipDto)
        {
            TeamsStandingOverviewViewModel teamsStandingOverviewViewModel = _viewModelFactory.Create<TeamsStandingOverviewViewModel>();
            teamsStandingOverviewViewModel.FromModel(teamsChampionshipDto);
            return teamsStandingOverviewViewModel;
        }

        private void CloseWindow(Window window)
        {
            window.Close();
        }

        private void DetailWindowOnClose(Window window)
        {
            if (window.Content is not ChampionshipDetailViewModel championshipDetailViewModel)
            {
                return;
            }

            _championshipDetailWindowsMap.Remove(championshipDetailViewModel.OriginalModel.ChampionshipGlobalId);
        }
    }
}