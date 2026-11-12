namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.LapsChart
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Input;

    using SecondMonitor.Contracts.Commands;
    using SecondMonitor.Contracts.Statistics;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.Telemetry.TelemetryApplication.Controllers.Synchronization;
    using SecondMonitor.Telemetry.TelemetryApplication.ViewModels.LapPicker;
    using SecondMonitor.Telemetry.TelemetryManagement.DTO;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;

    public class LapsChartWindowViewModel : AbstractViewModel
    {
        private const string AllLapsItem = "-All Laps-";
        private readonly IViewModelFactory _viewModelFactory;
        private readonly ILapColorSynchronization _lapColorSynchronization;
        private TimeSpan _stdDeviation;
        private TimeSpan _medianLapTime;
        private TimeSpan _meanLapTime;
        private TimeSpan _bestLapTime;
        private TimeSpan _worstLapTime;
        private TimeSpan _worsLapTimeDifference;
        private bool _suppressPropertyChangedProcessing;
        private string _selectedDriverName;

        public LapsChartWindowViewModel(IViewModelFactory viewModelFactory, ILapColorSynchronization lapColorSynchronization)
        {
            _viewModelFactory = viewModelFactory;
            _lapColorSynchronization = lapColorSynchronization;
            LapsChartViewModel = viewModelFactory.Create<LapsChartViewModel>();
            SelectAllLapsCommand = new RelayCommand(SelectAllLaps);
            DeselectAllLapsCommand = new RelayCommand(DeselectAllLaps);
        }

        public ObservableCollection<ILapSummaryViewModel> AllLapSummaryViewModels { get; } = [];

        public LapsChartViewModel LapsChartViewModel { get; }

        public TimeSpan StdDeviation
        {
            get => _stdDeviation;
            set => SetProperty(ref _stdDeviation, value);
        }

        public TimeSpan MedianLapTime
        {
            get => _medianLapTime;
            set => SetProperty(ref _medianLapTime, value);
        }

        public TimeSpan MeanLapTime
        {
            get => _meanLapTime;
            set => SetProperty(ref _meanLapTime, value);
        }

        public TimeSpan BestLapTime
        {
            get => _bestLapTime;
            set => SetProperty(ref _bestLapTime, value);
        }

        public TimeSpan WorstLapTime
        {
            get => _worstLapTime;
            set => SetProperty(ref _worstLapTime, value);
        }

        public TimeSpan WorsLapTimeDifference
        {
            get => _worsLapTimeDifference;
            set => SetProperty(ref _worsLapTimeDifference, value);
        }

        public ICommand SelectAllLapsCommand { get; }
        public ICommand DeselectAllLapsCommand { get; }

        public string SelectedDriverName
        {
            get => _selectedDriverName;
            set => SetProperty(ref _selectedDriverName, value);
        }

        public ObservableCollection<string> AllDriverNames { get; } = [];

        public void SetLapSummaries(IEnumerable<LapSummaryDto> allSummariesDtos, IReadOnlyList<string> selectedLapsIds)
        {
            AllLapSummaryViewModels.ForEach(x => x.PropertyChanged -= LapSummaryOnPropertyChanged);
            AllLapSummaryViewModels.Clear();
            string playerName = string.Empty;
            HashSet<string> otherDrivers = [];
            foreach (LapSummaryDto lapSummaryDto in allSummariesDtos)
            {
                LapSummaryViewModel newViewModel = _viewModelFactory.Create<LapSummaryViewModel>();
                newViewModel.FromModel(lapSummaryDto);
                newViewModel.Selected = selectedLapsIds.Contains(lapSummaryDto.Id);
                if (_lapColorSynchronization.TryGetColorForLap(lapSummaryDto.Id, out ColorDto lapColorDto))
                {
                    newViewModel.LapColor = lapColorDto.ToColor();
                }

                if (lapSummaryDto.IsPlayer)
                {
                    playerName = lapSummaryDto.DriverName;
                }
                else
                {
                    otherDrivers.Add(lapSummaryDto.DriverName);
                }

                newViewModel.IsDriverNameVisible = !lapSummaryDto.IsPlayer && !string.IsNullOrWhiteSpace(lapSummaryDto.DriverName);
                AllLapSummaryViewModels.Add(newViewModel);
            }

            AllLapSummaryViewModels.ForEach(x => x.PropertyChanged += LapSummaryOnPropertyChanged);
            AllDriverNames.Clear();
            AllDriverNames.Add(AllLapsItem);
            if (!string.IsNullOrWhiteSpace(playerName))
            {
                AllDriverNames.Add(playerName);
                SelectedDriverName = playerName;
            }

            otherDrivers.ForEach(x => AllDriverNames.Add(x));
            RefreshStatistics();
        }

        private void LapSummaryOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ILapSummaryViewModel.Selected) || _suppressPropertyChangedProcessing)
            {
                return;
            }

            RefreshStatistics();
        }

        private void RefreshStatistics()
        {
            List<LapSummaryDto> eligibleLaps = AllLapSummaryViewModels.Where(x => x.Selected).Select(x => x.OriginalModel).ToList();
            List<double> lapsInSeconds = eligibleLaps.Select(x => x.LapTimeSeconds).ToList();
            if (eligibleLaps.Count == 0)
            {
                MedianLapTime = TimeSpan.Zero;
                MeanLapTime = TimeSpan.Zero;
                StdDeviation = TimeSpan.Zero;
                BestLapTime = TimeSpan.Zero;
                WorstLapTime = TimeSpan.Zero;
                WorsLapTimeDifference = TimeSpan.Zero;
            }
            else
            {
                StandardDeviationResult<double> standardDeviationResult = StandardDeviationCalculator.ComputeDeviation(lapsInSeconds);
                MedianLapTime = TimeSpan.FromSeconds(StandardDeviationCalculator.Median(lapsInSeconds));
                MeanLapTime = TimeSpan.FromSeconds(standardDeviationResult.MeanValue);
                StdDeviation = TimeSpan.FromSeconds(standardDeviationResult.StandardDeviation);
                BestLapTime = TimeSpan.FromSeconds(lapsInSeconds.Min());
                WorstLapTime = TimeSpan.FromSeconds(lapsInSeconds.Max());
                WorsLapTimeDifference = WorstLapTime - BestLapTime;
            }

            LapsChartViewModel.FromModel((eligibleLaps, MeanLapTime, MedianLapTime));
        }

        private void SelectAllLaps()
        {
            try
            {
                _suppressPropertyChangedProcessing = true;
                if (SelectedDriverName == AllLapsItem)
                {
                    AllLapSummaryViewModels.ForEach(x => x.Selected = true);
                }
                else
                {
                    AllLapSummaryViewModels.Where(x => x.OriginalModel.DriverName == SelectedDriverName).ForEach(x => x.Selected = true);
                }

                RefreshStatistics();
            }
            finally
            {
                _suppressPropertyChangedProcessing = false;
            }
        }

        private void DeselectAllLaps()
        {
            try
            {
                _suppressPropertyChangedProcessing = true;
                if (SelectedDriverName == AllLapsItem)
                {
                    AllLapSummaryViewModels.ForEach(x => x.Selected = false);
                }
                else
                {
                    AllLapSummaryViewModels.Where(x => x.OriginalModel.DriverName == SelectedDriverName).ForEach(x => x.Selected = false);
                }

                RefreshStatistics();
            }
            finally
            {
                _suppressPropertyChangedProcessing = false;
            }
        }
    }
}
