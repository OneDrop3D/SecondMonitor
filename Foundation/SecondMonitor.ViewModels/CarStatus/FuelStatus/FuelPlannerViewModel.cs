namespace SecondMonitor.ViewModels.CarStatus.FuelStatus
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using Contracts.Commands;
    using DataModel.BasicProperties;
    using DataModel.Summary.FuelConsumption;
    using Factory;

    using SecondMonitor.DataModel.Extensions;

    using Settings;

    public class FuelPlannerViewModel : AbstractViewModel<ICollection<SessionFuelConsumptionDto>>
    {
        private readonly ISettingsProvider _settingsProvider;
        private ISessionFuelConsumptionViewModel _selectedSession;
        private SessionFuelConsumptionViewModel _averageConsumption;
        private bool _isVisible;

        public FuelPlannerViewModel(ISettingsProvider settingsProvider, IViewModelFactory viewModelFactory)
        {
            _settingsProvider = settingsProvider;
            IsVisible = false;
            Sessions = new ObservableCollection<ISessionFuelConsumptionViewModel>();
            CalculatorForSelectedSession = viewModelFactory.Create<FuelCalculatorViewModel>();
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public ObservableCollection<ISessionFuelConsumptionViewModel> Sessions { get; }

        public bool HasAnySession => Sessions.Count > 0;

        public ISessionFuelConsumptionViewModel SelectedSession
        {
            get => _selectedSession;
            set
            {
                _selectedSession = value;
                UpdateCalculatorViewModel();
                NotifyPropertyChanged();
            }
        }

        public FuelCalculatorViewModel CalculatorForSelectedSession { get; }

        private void UpdateCalculatorViewModel()
        {
            if (SelectedSession != null)
            {
                CalculatorForSelectedSession.FuelConsumption = SelectedSession.FuelConsumption;
                CalculatorForSelectedSession.LapDistance = SelectedSession.LapDistance.InMeters;
            }
        }

        protected override void ApplyModel(ICollection<SessionFuelConsumptionDto> model)
        {
            Sessions.ForEach(x => x.PropertyChanged -= SessionInfoOnPropertyChanged);
            Sessions.Clear();
            if (model.Count == 0)
            {
                SelectedSession = null;
                CalculatorForSelectedSession.FuelConsumption = null;
                NotifyPropertyChanged(nameof(HasAnySession));
                return;
            }

            foreach (var sessionFuelConsumptionDto in model.Where(x => x != null))
            {
                SessionFuelConsumptionViewModel consumptionViewModel = new SessionFuelConsumptionViewModel(_settingsProvider)
                {
                    IsCheckedVisible = true,
                    IsChecked = false,
                    IsDeleteButtonVisible = true,
                    DeleteCommand = new RelayCommand(DeleteSelectedConsumptionInfo),
                };

                consumptionViewModel.FromModel(sessionFuelConsumptionDto);
                Sessions.Add(consumptionViewModel);
            }

            CreateAggregateConsumption();

            SelectedSession = Sessions.First();
            Sessions.ForEach(x => x.PropertyChanged += SessionInfoOnPropertyChanged);
            NotifyPropertyChanged(nameof(HasAnySession));
        }

        private void CreateAggregateConsumption()
        {
            List<ISessionFuelConsumptionViewModel> sessionsToUse = Sessions.Where(x => !x.OriginalModel.IsWetSession && x.OriginalModel.SessionKind == SessionType.Race).ToList();
            string aggregateName = "Total (Dry Race)";
            if (sessionsToUse.Count == 0)
            {
                sessionsToUse = Sessions.Where(x => !x.OriginalModel.IsWetSession).ToList();
                aggregateName = "Total (Dry)";
            }

            if (sessionsToUse.Count == 0)
            {
                return;
            }

            _averageConsumption = new SessionFuelConsumptionViewModel(_settingsProvider) { IsCheckedVisible = false };
            sessionsToUse.ForEach(x => x.IsChecked = true);
            _averageConsumption.FromModel(CreateAggregated(sessionsToUse.Select(x => x.OriginalModel).ToList()));
            _averageConsumption.SessionType = aggregateName;
            Sessions.Insert(0, _averageConsumption);
        }

        private void DeleteSelectedConsumptionInfo()
        {
            if (SelectedSession == null)
            {
                return;
            }

            int previouslySelectedIndex = Sessions.IndexOf(SelectedSession);
            Sessions.Remove(SelectedSession);
            SelectedSession = previouslySelectedIndex >= Sessions.Count ? Sessions.LastOrDefault() : Sessions[previouslySelectedIndex];
            RecalculateAverage();
            NotifyPropertyChanged(nameof(HasAnySession));
        }

        private void SessionInfoOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ISessionFuelConsumptionViewModel.IsChecked) || _averageConsumption == null)
            {
                return;
            }

            RecalculateAverage();
        }

        private void RecalculateAverage()
        {
            _averageConsumption.FromModel(CreateAggregated(Sessions.Where(x => x.IsChecked).Select(x => x.OriginalModel).ToList()));
            UpdateCalculatorViewModel();
        }

        private SessionFuelConsumptionDto CreateAggregated(ICollection<SessionFuelConsumptionDto> consumptionEntries)
        {
            if (!consumptionEntries.Any())
            {
                return new SessionFuelConsumptionDto(string.Empty, string.Empty, 0, string.Empty, SessionType.Na, 0, 0, Volume.FromLiters(0), DateTime.Now, false);
            }

            SessionFuelConsumptionDto baseConsumption = consumptionEntries.First();
            List<SessionFuelConsumptionDto> entriesWithVirtualEnergyCoef = consumptionEntries.Where(x => x.HasVirtualEnergyFuelCoef).ToList();

            if (entriesWithVirtualEnergyCoef.Count == 0)
            {
                return new SessionFuelConsumptionDto(baseConsumption.Simulator, baseConsumption.TrackFullName, baseConsumption.LapDistance, baseConsumption.CarName,
                    baseConsumption.SessionKind, consumptionEntries.Sum(x => x.ElapsedSeconds), consumptionEntries.Sum(x => x.TraveledDistanceMeters),
                    Volume.FromLiters(consumptionEntries.Select(x => x.ConsumedFuel.InLiters).Sum(x => x)),
                    DateTime.Now, baseConsumption.IsWetSession);
            }

            double virtualTankCoefAvg = entriesWithVirtualEnergyCoef.Sum(x => x.VirtualEnergyFuelCoef * x.TraveledDistanceMeters) /
                                        entriesWithVirtualEnergyCoef.Sum(x => x.TraveledDistanceMeters);
            return new SessionFuelConsumptionDto(baseConsumption.Simulator, baseConsumption.TrackFullName, baseConsumption.LapDistance, baseConsumption.CarName,
                baseConsumption.SessionKind, consumptionEntries.Sum(x => x.ElapsedSeconds), consumptionEntries.Sum(x => x.TraveledDistanceMeters),
                Volume.FromLiters(consumptionEntries.Select(x => x.ConsumedFuel.InLiters).Sum(x => x)),
                DateTime.Now, baseConsumption.IsWetSession, virtualTankCoefAvg);
        }

        public override ICollection<SessionFuelConsumptionDto> SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}