namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;
    using Contracts.Commands;
    using DataGrid;
    using DataModel.BasicProperties;

    using Factory;
    using Model;

    public class SessionsOptionsViewModel : AbstractViewModel<DisplaySettings>, IApplyToModel<DisplaySettings>
    {
        private readonly IViewModelFactory _viewModelFactory;
        private readonly IDefaultColumnsFactory _defaultColumnsFactory;
        private SessionOptionsViewModel _practiceSessionDisplayOptionsView;
        private SessionOptionsViewModel _qualificationSessionDisplayOptionsView;
        private SessionOptionsViewModel _raceSessionDisplayOptionsView;

        public SessionsOptionsViewModel(IViewModelFactory viewModelFactory, IDefaultColumnsFactory defaultColumnsFactory, ISettingsProvider settingsProvider, IColumnDescriptorTemplatesFactory columnDescriptorTemplatesFactory)
        {
            _viewModelFactory = viewModelFactory;
            _defaultColumnsFactory = defaultColumnsFactory;
            ApplyColumnsCommand = new RelayCommand(ApplyColumnsSettings);
            RevertToDefaultCommand = new RelayCommand(RevertToDefaultColumns);
            AvailableColumns = columnDescriptorTemplatesFactory.Create().DistinctBy(x => x.Name).OrderBy(x => x.Name).ToList();
        }

        public event EventHandler ColumnDescriptorsChanged;

        public SessionOptionsViewModel PracticeSessionDisplayOptionsView
        {
            get => _practiceSessionDisplayOptionsView;
            set => SetProperty(ref _practiceSessionDisplayOptionsView, value);
        }

        public SessionOptionsViewModel QualificationSessionDisplayOptionsView
        {
            get => _qualificationSessionDisplayOptionsView;
            set => SetProperty(ref _qualificationSessionDisplayOptionsView, value);
        }

        public SessionOptionsViewModel RaceSessionDisplayOptionsView
        {
            get => _raceSessionDisplayOptionsView;
            set => SetProperty(ref _raceSessionDisplayOptionsView, value);
        }

        public ICommand RevertToDefaultCommand { get; set; }

        public ICommand ApplyColumnsCommand { get; set; }

        public List<IColumnDescriptorTemplate> AvailableColumns { get; }

        protected override void ApplyModel(DisplaySettings settings)
        {
            PracticeSessionDisplayOptionsView = SessionOptionsViewModel.CreateFromModel(settings.PracticeOptions, _viewModelFactory);
            QualificationSessionDisplayOptionsView = SessionOptionsViewModel.CreateFromModel(settings.QualificationOptions, _viewModelFactory);
            RaceSessionDisplayOptionsView = SessionOptionsViewModel.CreateFromModel(settings.RaceOptions, _viewModelFactory);
        }

        public override DisplaySettings SaveToNewModel()
        {
            throw new NotSupportedException();
        }

        public void RefreshColumnDescriptorsViewModels()
        {
            PracticeSessionDisplayOptionsView.ColumnsSettingsViewModel.RefreshColumnDescriptorsViewModels();
            QualificationSessionDisplayOptionsView.ColumnsSettingsViewModel.RefreshColumnDescriptorsViewModels();
            RaceSessionDisplayOptionsView.ColumnsSettingsViewModel.RefreshColumnDescriptorsViewModels();
        }

        public void ApplyToModel(DisplaySettings model)
        {
            model.PracticeOptions = PracticeSessionDisplayOptionsView.ToModel();
            model.QualificationOptions = QualificationSessionDisplayOptionsView.ToModel();
            model.RaceOptions = RaceSessionDisplayOptionsView.ToModel();
        }

        private void ApplyColumnsSettings()
        {
            PracticeSessionDisplayOptionsView.ColumnsSettingsViewModel.SaveColumnsDescriptors();
            QualificationSessionDisplayOptionsView.ColumnsSettingsViewModel.SaveColumnsDescriptors();
            RaceSessionDisplayOptionsView.ColumnsSettingsViewModel.SaveColumnsDescriptors();
            ColumnDescriptorsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RevertToDefaultColumns()
        {
            PracticeSessionDisplayOptionsView.ColumnsSettingsViewModel.ApplyModel(_defaultColumnsFactory.GetColumnDescriptors(SessionType.Practice).ToList());
            QualificationSessionDisplayOptionsView.ColumnsSettingsViewModel.ApplyModel(_defaultColumnsFactory.GetColumnDescriptors(SessionType.Qualification).ToList());
            RaceSessionDisplayOptionsView.ColumnsSettingsViewModel.ApplyModel(_defaultColumnsFactory.GetColumnDescriptors(SessionType.Race).ToList());
        }
    }
}