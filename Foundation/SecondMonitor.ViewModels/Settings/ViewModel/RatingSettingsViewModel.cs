namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using Model;

    public class RatingSettingsViewModel : AbstractViewModel<RatingSettings>
    {
        private bool _isEnabled;
        private string _selectedReferenceRatingProvider;
        private string[] _availableReferenceRatingProviders;
        private int _graceLapsCount;
        private int _eloK;
        private int _normalizedFieldSize;
        private double _aiCompetitiveRange;

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        public int GraceLapsCount
        {
            get => _graceLapsCount;
            set => SetProperty(ref _graceLapsCount, value);
        }

        public string SelectedReferenceRatingProvider
        {
            get => _selectedReferenceRatingProvider;
            set => SetProperty(ref _selectedReferenceRatingProvider, value);
        }

        public string[] AvailableReferenceRatingProviders
        {
            get => _availableReferenceRatingProviders;
            set => SetProperty(ref _availableReferenceRatingProviders, value);
        }

        public int NormalizedFieldSize
        {
            get => _normalizedFieldSize;
            set => SetProperty(ref _normalizedFieldSize, value);
        }

        public double AiCompetitiveRange
        {
            get => _aiCompetitiveRange;
            set => SetProperty(ref _aiCompetitiveRange, value);
        }

        public int EloK
        {
            get => _eloK;
            set => SetProperty(ref _eloK, value);
        }

        protected override void ApplyModel(RatingSettings model)
        {
            IsEnabled = model.IsEnabled;
            SelectedReferenceRatingProvider = model.SelectedReferenceRatingProvider;
            GraceLapsCount = model.GraceLapsCount;
            EloK = model.EloK;
            NormalizedFieldSize = model.NormalizedFieldSize;
            AiCompetitiveRange = model.AiCompetitiveRange;
        }

        public override RatingSettings SaveToNewModel()
        {
            return new RatingSettings()
            {
                IsEnabled = IsEnabled,
                SelectedReferenceRatingProvider = SelectedReferenceRatingProvider,
                GraceLapsCount = GraceLapsCount,
                EloK = EloK,
                NormalizedFieldSize = NormalizedFieldSize,
                AiCompetitiveRange = AiCompetitiveRange,
            };
        }
    }
}