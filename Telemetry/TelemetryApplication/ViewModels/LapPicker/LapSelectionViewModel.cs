namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.LapPicker
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Data;
    using System.Windows.Input;
    using Controllers.MainWindow.LapPicker;
    using Controllers.Synchronization;

    using OpenWindow;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels;
    using TelemetryManagement.DTO;

    public class LapSelectionViewModel : AbstractViewModel
    {
        private readonly SortingKindMap _sortingKindMap;
        private ILapSummaryViewModel _selected;
        private string _carName;
        private string _trackName;
        private DateTime _sessionTime;
        private string _simulatorName;
        private string _bestLap;
        private IOpenWindowViewModel _openWindowViewModel;
        private string _bestSector1;
        private string _bestSector2;
        private string _bestSector3;
        private IOpenWindowViewModel _addWindowViewModel;
        private ICommand _addCustomLapCommand;
        private ICommand _openAggregatedGraphSelectorCommand;
        private bool _openAggregatedChartSelectorEnabled;
        private ICommand _loadAllLapsCommand;
        private IReadOnlyCollection<string> _availableStints;
        private string _selectedStint;
        private ICommand _unloadAllLapsCommand;
        private ICommand _openSettingsWindowCommand;
        private ICommand _openCarSettingsCommand;
        private bool _isSaveButtonEnabled;
        private ICommand _saveSessionCommand;
        private string _sessionName;
        private ICommand _saveAsNewSessionCommand;
        private ICommand _exportSessionCommand;
        private ICommand _importSessionCommand;
        private string _quickSearchText;
        private string _selectedSortingKind;
        private string _searchTextLowerCase;
        private bool _areLapsGrouped;
        private ILapSummaryViewModel _referenceLap;
        private ICommand _openLapChartCommand;

        public LapSelectionViewModel()
        {
            _sortingKindMap = new SortingKindMap();
            SelectedSortingKind = _sortingKindMap.ToHumanReadable(SortingKind.PlayerFirstByTime);
            LapSummaries = new ObservableCollection<ILapSummaryViewModel>();
            LapSummariesFiltered = CollectionViewSource.GetDefaultView(LapSummaries);
            LapSummariesFiltered.Filter = p => FilterLapSummaries((ILapSummaryViewModel)p);

            SelectedLaps = new CollectionViewSource()
            {
                Source = LapSummaries
            }.View;
            SelectedLaps.Filter = p => ((ILapSummaryViewModel)p).Selected;
            ReApplySorting();
        }

        public event EventHandler<LapSummaryArgs> LapSelected;
        public event EventHandler<LapSummaryArgs> LapUnselected;
        public event EventHandler<LapSummaryArgs> ReferenceLapChanged;

        public string[] AllowedSortingKinds => _sortingKindMap.GetAllHumanReadableValue().ToArray();

        public string SelectedSortingKind
        {
            get => _selectedSortingKind;
            set
            {
                SetProperty(ref _selectedSortingKind, value);
                ReApplySorting();
            }
        }

        public IOpenWindowViewModel OpenWindowViewModel
        {
            get => _openWindowViewModel;
            set => SetProperty(ref _openWindowViewModel, value);
        }

        public IOpenWindowViewModel AddWindowViewModel
        {
            get => _addWindowViewModel;
            set => SetProperty(ref _addWindowViewModel, value);
        }

        public ICollectionView LapSummariesFiltered
        {
            get;
        }

        public ObservableCollection<ILapSummaryViewModel> LapSummaries
        {
            get;
        }

        public ICollectionView SelectedLaps
        {
            get;
        }

        public ICommand AddCustomLapCommand
        {
            get => _addCustomLapCommand;
            set => SetProperty(ref _addCustomLapCommand, value);
        }

        public ICommand OpenCarSettingsCommand
        {
            get => _openCarSettingsCommand;
            set => SetProperty(ref _openCarSettingsCommand, value);
        }

        public ICommand OpenAggregatedChartSelectorCommand
        {
            get => _openAggregatedGraphSelectorCommand;
            set => SetProperty(ref _openAggregatedGraphSelectorCommand, value);
        }

        public ICommand OpenSettingsWindowCommand
        {
            get => _openSettingsWindowCommand;
            set => SetProperty(ref _openSettingsWindowCommand, value);
        }

        public string SelectedStint
        {
            get => _selectedStint;
            set => SetProperty(ref _selectedStint, value);
        }

        public bool IsOpenAggregatedChartSelectorEnabled
        {
            get => _openAggregatedChartSelectorEnabled;
            set => SetProperty(ref _openAggregatedChartSelectorEnabled, value);
        }

        public ILapSummaryViewModel Selected
        {
            get => _selected;
            set => SetProperty(ref _selected, value);
        }

        public string CarName
        {
            get => _carName;
            set => SetProperty(ref _carName, value);
        }

        public string TrackName
        {
            get => _trackName;
            set => SetProperty(ref _trackName, value);
        }

        public string SimulatorName
        {
            get => _simulatorName;
            set => SetProperty(ref _simulatorName, value);
        }

        public DateTime SessionTime
        {
            get => _sessionTime;
            set => SetProperty(ref _sessionTime, value);
        }

        public string BestLap
        {
            get => _bestLap;
            set => SetProperty(ref _bestLap, value);
        }

        public string BestSector1
        {
            get => _bestSector1;
            set => SetProperty(ref _bestSector1, value);
        }

        public string BestSector2
        {
            get => _bestSector2;
            set => SetProperty(ref _bestSector2, value);
        }

        public string BestSector3
        {
            get => _bestSector3;
            set => SetProperty(ref _bestSector3, value);
        }

        public ICommand LoadAllLapsCommand
        {
            get => _loadAllLapsCommand;
            set => SetProperty(ref _loadAllLapsCommand, value);
        }

        public IReadOnlyCollection<string> AvailableStints
        {
            get => _availableStints;
            set => SetProperty(ref _availableStints, value);
        }

        public ICommand UnloadAllLapsCommand
        {
            get => _unloadAllLapsCommand;
            set => SetProperty(ref _unloadAllLapsCommand, value);
        }

        public bool IsSaveButtonEnabled
        {
            get => _isSaveButtonEnabled;
            set => SetProperty(ref _isSaveButtonEnabled, value);
        }

        public ICommand SaveSessionCommand
        {
            get => _saveSessionCommand;
            set => SetProperty(ref _saveSessionCommand, value);
        }

        public string SessionName
        {
            get => _sessionName;
            set => SetProperty(ref _sessionName, value);
        }

        public ICommand SaveAsNewSessionCommand
        {
            get => _saveAsNewSessionCommand;
            set => SetProperty(ref _saveAsNewSessionCommand, value);
        }

        public ICommand ExportSessionCommand
        {
            get => _exportSessionCommand;
            set => SetProperty(ref _exportSessionCommand, value);
        }

        public ICommand ImportSessionCommand
        {
            get => _importSessionCommand;
            set => SetProperty(ref _importSessionCommand, value);
        }

        public bool AreLapsGrouped
        {
            get => _areLapsGrouped;
            set => SetProperty(ref _areLapsGrouped, value);
        }

        public string QuickSearchText
        {
            get => _quickSearchText;
            set
            {
                _searchTextLowerCase = value?.ToLower() ?? string.Empty;
                SetProperty(ref _quickSearchText, value, (_, __) => RefreshLapSummariesView());
            }
        }

        public ILapSummaryViewModel ReferenceLap
        {
            get => _referenceLap;
            set
            {
                SetProperty(ref _referenceLap, value);
                ReferenceLapChanged?.Invoke(this, new LapSummaryArgs(ReferenceLapDto));
            }
        }

        public LapSummaryDto ReferenceLapDto
        {
            get => _referenceLap?.OriginalModel;
            set
            {
                _referenceLap = value == null ? null : LapSummaries.FirstOrDefault(x => x.OriginalModel.Id == value.Id);
                NotifyPropertyChanged(nameof(ReferenceLap));
            }
        }

        public ICommand OpenLapChartCommand
        {
            get => _openLapChartCommand;
            set => SetProperty(ref _openLapChartCommand, value);
        }

        private bool FilterLapSummaries(ILapSummaryViewModel lapSummaryViewModel)
        {
            return string.IsNullOrWhiteSpace(QuickSearchText) ||
                   lapSummaryViewModel.CustomName.ToLower().Contains(_searchTextLowerCase) || lapSummaryViewModel.DriverName.ToLower().Contains(_searchTextLowerCase);
        }

        public void RefreshLapSummariesView()
        {
            LapSummariesFiltered.Refresh();
        }

        public void AddLapSummaryViewModel(ILapSummaryViewModel lapSummaryViewModel)
        {
            lapSummaryViewModel.PropertyChanged += LapSummaryViewModelOnPropertyChanged;
            LapSummaries.Add(lapSummaryViewModel);
        }

        public void Clear()
        {
            foreach (ILapSummaryViewModel lapSummaryViewModel in LapSummaries)
            {
                lapSummaryViewModel.PropertyChanged -= LapSummaryViewModelOnPropertyChanged;
            }

            LapSummaries.Clear();
        }

        private void ReApplySorting()
        {
            if (LapSummaries == null)
            {
                return;
            }

            LapSummariesFiltered.SortDescriptions.Clear();
            LapSummariesFiltered.GroupDescriptions.Clear();
            switch (_sortingKindMap.FromHumanReadable(_selectedSortingKind))
            {
                case SortingKind.PlayerFirstByLap:
                    AreLapsGrouped = false;
                    LapSummariesFiltered.SortDescriptions.Add(new SortDescription(nameof(ILapSummaryViewModel.IsPlayerLap), ListSortDirection.Descending));
                    LapSummariesFiltered.SortDescriptions.Add(new SortDescription(nameof(ILapSummaryViewModel.LapNumber), ListSortDirection.Ascending));
                    break;
                case SortingKind.ByTime:
                    AreLapsGrouped = false;
                    LapSummariesFiltered.SortDescriptions.Add(new SortDescription(nameof(ILapSummaryViewModel.HasLapTime), ListSortDirection.Descending));
                    LapSummariesFiltered.SortDescriptions.Add(new SortDescription(nameof(ILapSummaryViewModel.LapTime), ListSortDirection.Ascending));
                    break;
                case SortingKind.ByLap:
                    AreLapsGrouped = false;
                    LapSummariesFiltered.SortDescriptions.Add(new SortDescription(nameof(ILapSummaryViewModel.LapNumber), ListSortDirection.Ascending));
                    break;
                case SortingKind.GroupByDriverByLap:
                    AreLapsGrouped = true;
                    LapSummariesFiltered.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ILapSummaryViewModel.DriverName)));
                    LapSummariesFiltered.SortDescriptions.Add(new SortDescription(nameof(ILapSummaryViewModel.LapNumber), ListSortDirection.Ascending));
                    break;
                case SortingKind.GroupByDriverTime:
                    AreLapsGrouped = true;
                    LapSummariesFiltered.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ILapSummaryViewModel.DriverName)));
                    LapSummariesFiltered.SortDescriptions.Add(new SortDescription(nameof(ILapSummaryViewModel.HasLapTime), ListSortDirection.Descending));
                    LapSummariesFiltered.SortDescriptions.Add(new SortDescription(nameof(ILapSummaryViewModel.LapTime), ListSortDirection.Ascending));
                    break;
                case SortingKind.PlayerFirstByTime:
                default:
                    AreLapsGrouped = false;
                    LapSummariesFiltered.SortDescriptions.Add(new SortDescription(nameof(ILapSummaryViewModel.IsPlayerLap), ListSortDirection.Descending));
                    LapSummariesFiltered.SortDescriptions.Add(new SortDescription(nameof(ILapSummaryViewModel.HasLapTime), ListSortDirection.Descending));
                    LapSummariesFiltered.SortDescriptions.Add(new SortDescription(nameof(ILapSummaryViewModel.LapTime), ListSortDirection.Ascending));
                    break;
            }

            LapSummaries.ForEach(x => x.IsDriverNameVisible = !_areLapsGrouped && !x.IsPlayerLap);

            RefreshLapSummariesView();
        }

        private void LapSummaryViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ILapSummaryViewModel.Selected) || !(sender is ILapSummaryViewModel lapSummaryViewModel))
            {
                return;
            }

            SelectedLaps.Refresh();

            if (lapSummaryViewModel.Selected)
            {
                LapSelected?.Invoke(this, new LapSummaryArgs(lapSummaryViewModel.OriginalModel));
            }
            else
            {
                LapUnselected?.Invoke(this, new LapSummaryArgs(lapSummaryViewModel.OriginalModel));
            }
        }
    }
}