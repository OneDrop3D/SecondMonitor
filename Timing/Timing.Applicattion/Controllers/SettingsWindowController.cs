namespace SecondMonitor.Timing.Application.Controllers
{
    using System;
    using System.Threading.Tasks;
    using System.Windows;
    using Contracts.Commands;
    using DataModel.BasicProperties;
    using PluginsConfiguration.Common.Controller;
    using Presentation.View;
    using ViewModels.Controllers;
    using ViewModels.DataGrid;
    using ViewModels.Layouts;
    using ViewModels.Settings;

    public class SettingsWindowController : IController
    {
        private readonly ISettingsProvider _settingsProvider;
        private readonly ILayoutEditorManipulator _layoutEditorManipulator;
        private readonly TimingGridColumnsFactory _timingGridColumnsFactory;
        private readonly IPluginSettingsProvider _pluginSettingsProvider;
        private DisplaySettingsWindow _settingsWindow;

        public SettingsWindowController(ISettingsProvider settingsProvider, ILayoutEditorManipulator layoutEditorManipulator, TimingGridColumnsFactory timingGridColumnsFactory, IPluginSettingsProvider pluginSettingsProvider)
        {
            _settingsProvider = settingsProvider;
            _layoutEditorManipulator = layoutEditorManipulator;
            _timingGridColumnsFactory = timingGridColumnsFactory;
            _pluginSettingsProvider = pluginSettingsProvider;
            _layoutEditorManipulator.ApplyLayoutCommand = new RelayCommand(ApplyNewLayout);
        }

        public void OpenSettingWindow()
        {
            if (_settingsWindow?.IsVisible == true)
            {
                _settingsWindow.Focus();
                return;
            }

            CreateSettingsWindow();
        }

        private void CreateSettingsWindow()
        {
            _layoutEditorManipulator.SetLayoutDescription(_settingsProvider.DisplaySettingsViewModel.LayoutSettingsViewModel.LayoutDescription);
            _settingsProvider.DisplaySettingsViewModel.SessionOptionsViewModel.RefreshColumnDescriptorsViewModels();
            _settingsWindow = new DisplaySettingsWindow
            {
                DataContext = _settingsProvider.DisplaySettingsViewModel,
                Owner = Application.Current.MainWindow,
            };
            _settingsWindow.Show();
            _settingsWindow.Closed += SettingsWindowOnClosed;
        }

        public Task StartControllerAsync()
        {
            _settingsProvider.DisplaySettingsViewModel.DefaultLayoutCallback = _layoutEditorManipulator.CreateDefaultLayout;
            _settingsProvider.DisplaySettingsViewModel.LayoutEditorViewModel = _layoutEditorManipulator.LayoutEditorViewModel;
            if (_settingsProvider.DisplaySettingsViewModel.LayoutSettingsViewModel.LayoutDescription == null)
            {
                _settingsProvider.DisplaySettingsViewModel.LayoutSettingsViewModel.LayoutDescription = _layoutEditorManipulator.CreateDefaultLayout();
            }

            if (_settingsProvider.DisplaySettingsViewModel.SessionOptionsViewModel.PracticeSessionDisplayOptionsView.ColumnsSettingsViewModel.ColumnDescriptorViewModels.Count == 0)
            {
                _settingsProvider.DisplaySettingsViewModel.SessionOptionsViewModel.PracticeSessionDisplayOptionsView.ColumnsSettingsViewModel.ApplyModel(_timingGridColumnsFactory.GetColumnDescriptors(SessionType.Practice));
            }

            if (_settingsProvider.DisplaySettingsViewModel.SessionOptionsViewModel.QualificationSessionDisplayOptionsView.ColumnsSettingsViewModel.ColumnDescriptorViewModels.Count == 0)
            {
                _settingsProvider.DisplaySettingsViewModel.SessionOptionsViewModel.QualificationSessionDisplayOptionsView.ColumnsSettingsViewModel.ApplyModel(_timingGridColumnsFactory.GetColumnDescriptors(SessionType.Qualification));
            }

            if (_settingsProvider.DisplaySettingsViewModel.SessionOptionsViewModel.RaceSessionDisplayOptionsView.ColumnsSettingsViewModel.ColumnDescriptorViewModels.Count == 0)
            {
                _settingsProvider.DisplaySettingsViewModel.SessionOptionsViewModel.RaceSessionDisplayOptionsView.ColumnsSettingsViewModel.ApplyModel(_timingGridColumnsFactory.GetColumnDescriptors(SessionType.Race));
            }

            _settingsProvider.DisplaySettingsViewModel.PluginsConfigurationViewModel.FromModel(_pluginSettingsProvider.PluginConfiguration);

            return Task.CompletedTask;
        }

        private void SettingsWindowOnClosed(object sender, EventArgs e)
        {
            _settingsWindow.Closed -= SettingsWindowOnClosed;
            var pluginsConfiguration = _settingsProvider.DisplaySettingsViewModel.PluginsConfigurationViewModel.SaveToNewModel();

            _pluginSettingsProvider.SaveConfiguration(pluginsConfiguration);
        }

        private void ApplyNewLayout()
        {
            _settingsProvider.DisplaySettingsViewModel.LayoutSettingsViewModel.LayoutDescription = _layoutEditorManipulator.GetLayoutDescription();
        }

        public Task StopControllerAsync()
        {
            return Task.CompletedTask;
        }
    }
}