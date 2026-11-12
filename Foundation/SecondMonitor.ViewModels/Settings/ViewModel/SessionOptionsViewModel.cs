namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using System.Collections.Generic;
    using System.Linq;
    using Columns;
    using Factory;
    using Model;

    public class SessionOptionsViewModel : AbstractViewModel
    {
        private readonly IViewModelFactory _viewModelFactory;
        private readonly DriverOrderKindMap _driverOrderKindMap;
        private DisplayModeEnum _timesDisplayMode;
        private string _selectedOrderingMode;
        private string _sessionName;
        private bool _highlightPlayer;
        private int _highlightFontSize;
        private ColumnsSettingsViewModel _columnsSettingsViewModelModel;

        public SessionOptionsViewModel(IViewModelFactory viewModelFactory)
        {
            _driverOrderKindMap = new DriverOrderKindMap();
            _viewModelFactory = viewModelFactory;
            AvailableOrderingModes = _driverOrderKindMap.GetAllHumanReadableValue().OrderBy(x => x).ToList();
        }

        public DisplayModeEnum TimesDisplayMode
        {
            get => _timesDisplayMode;
            set => SetProperty(ref _timesDisplayMode, value);
        }

        public string SelectedOrderingMode
        {
            get => _selectedOrderingMode;
            set
            {
                SetProperty(ref _selectedOrderingMode, value);
                OrderingMode = _driverOrderKindMap.FromHumanReadable(value);
            }
        }

        public DriverOrderKind OrderingMode
        {
            get;
            private set;
        }

        public List<string> AvailableOrderingModes { get; }

        public string SessionName
        {
            get => _sessionName;
            set => SetProperty(ref _sessionName, value);
        }

        public int HighlightFontSize
        {
            get => _highlightFontSize;
            set => SetProperty(ref _highlightFontSize, value);
        }

        public ColumnsSettingsViewModel ColumnsSettingsViewModel
        {
            get => _columnsSettingsViewModelModel;
            set => SetProperty(ref _columnsSettingsViewModelModel, value);
        }

        public bool HighlightPlayer
        {
            get => _highlightPlayer;
            set => SetProperty(ref _highlightPlayer, value);
        }

        public static SessionOptionsViewModel CreateFromModel(SessionOptions model, IViewModelFactory viewModelFactory)
        {
            SessionOptionsViewModel newViewModel = viewModelFactory.Create<SessionOptionsViewModel>();
            newViewModel.FromModel(model);
            return newViewModel;
        }

        public void FromModel(SessionOptions model)
        {
            TimesDisplayMode = model.TimesDisplayMode;
            SelectedOrderingMode = _driverOrderKindMap.ToHumanReadable(model.OrderingDisplayMode);
            SessionName = model.SessionName;
            ColumnsSettingsViewModel = ColumnsSettingsViewModel.CreateFromModel(model.ColumnsSettings, _viewModelFactory);
            HighlightPlayer = model.HighlightPlayer;
            HighlightFontSize = model.HighlightFontSize;
        }

        public SessionOptions ToModel()
        {
            return new SessionOptions()
            {
                OrderingDisplayMode = _driverOrderKindMap.FromHumanReadable(SelectedOrderingMode),
                TimesDisplayMode = TimesDisplayMode,
                SessionName = SessionName,
                ColumnsSettings = ColumnsSettingsViewModel.SaveToNewModel(),
                HighlightPlayer = HighlightPlayer,
                HighlightFontSize = HighlightFontSize,
            };
        }
    }
}
