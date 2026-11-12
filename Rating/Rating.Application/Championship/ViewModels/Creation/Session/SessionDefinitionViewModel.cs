namespace SecondMonitor.Rating.Application.Championship.ViewModels.Creation.Session
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows.Input;

    using Common.Championship.Scoring.Templates;

    using SecondMonitor.Contracts.Commands;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;

    using SessionLength;

    public class SessionDefinitionViewModel : AbstractViewModel
    {
        private string _originalSessionName;
        private string _customSessionName;
        private ScoringTemplate _selectedScoringTemplate;
        private int _pointsForFastestLap;
        private int _maxPositionForFastestLap;

        private ISessionLengthDefinitionViewModel _selectedSessionLengthDefinitionViewModel;

        public SessionDefinitionViewModel(IViewModelFactory viewModelFactory)
        {
            AvailableSessionLengthDefinitionViewModels = viewModelFactory.CreateAll<ISessionLengthDefinitionViewModel>().ToList();
            SelectedSessionLengthDefinitionViewModel = AvailableSessionLengthDefinitionViewModels.First();
            ScoringTemplates = Common.Championship.Scoring.Templates.ScoringTemplates.AllTemplates.OrderBy(x => x.Name).ToArray();
            Points =
            [
                new PositionPointsViewModel(1, 25),
                new PositionPointsViewModel(2, 18),
                new PositionPointsViewModel(3, 15),
                new PositionPointsViewModel(4, 12),
                new PositionPointsViewModel(5, 10),
                new PositionPointsViewModel(6, 8),
                new PositionPointsViewModel(7, 6),
                new PositionPointsViewModel(8, 4),
                new PositionPointsViewModel(9, 2),
                new PositionPointsViewModel(10, 1)
            ];
            AddPointPositionCommand = new RelayCommand(AddPointPosition);
            RemovePointPositionCommand = new RelayCommand(RemovePointPosition);
        }

        public IReadOnlyCollection<ISessionLengthDefinitionViewModel> AvailableSessionLengthDefinitionViewModels { get; set; }

        public ISessionLengthDefinitionViewModel SelectedSessionLengthDefinitionViewModel
        {
            get => _selectedSessionLengthDefinitionViewModel;
            set => SetProperty(ref _selectedSessionLengthDefinitionViewModel, value);
        }

        public ScoringTemplate[] ScoringTemplates { get; }

        public ScoringTemplate SelectedScoringTemplate
        {
            get => _selectedScoringTemplate;
            set
            {
                SetProperty(ref _selectedScoringTemplate, value);
                ApplyScoringTemplate();
            }
        }

        public ObservableCollection<PositionPointsViewModel> Points { get; }

        public ICommand AddPointPositionCommand { get; }

        public ICommand RemovePointPositionCommand { get; }

        public int PointsForFastestLap
        {
            get => _pointsForFastestLap;
            set => SetProperty(ref _pointsForFastestLap, value);
        }

        public int MaxPositionForFastestLap
        {
            get => _maxPositionForFastestLap;
            set => SetProperty(ref _maxPositionForFastestLap, value);
        }

        public string OriginalSessionName
        {
            get => _originalSessionName;
            set => SetProperty(ref _originalSessionName, value, nameof(CustomSessionName));
        }

        public string CustomSessionName
        {
            get => string.IsNullOrWhiteSpace(_customSessionName) ? _originalSessionName : _customSessionName;
            set => SetProperty(ref _customSessionName, value);
        }

        private void ApplyScoringTemplate()
        {
            ScoringTemplate scoringTemplate = _selectedScoringTemplate;

            if (scoringTemplate == null)
            {
                return;
            }

            Points.Clear();

            for (int i = 0; i < scoringTemplate.Scoring.Length; i++)
            {
                Points.Add(new PositionPointsViewModel(i + 1, scoringTemplate.Scoring[i]));
            }

            _maxPositionForFastestLap = scoringTemplate.MaxPositionForFastestLap;
            _pointsForFastestLap = scoringTemplate.PointsForFastestLap;
            NotifyPropertyChanged(string.Empty);
        }

        private void AddPointPosition()
        {
            Points.Add(new PositionPointsViewModel(Points.Count + 1, 0));
        }

        private void RemovePointPosition()
        {
            if (Points.Count <= 1)
            {
                return;
            }

            Points.RemoveAt(Points.Count - 1);
        }
    }
}
