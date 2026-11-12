namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.SettingsWindow
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Forms;
    using System.Xml.Serialization;
    using Contracts.Commands;
    using NLog;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Controllers;
    using SecondMonitor.ViewModels.Dialogs;
    using SecondMonitor.ViewModels.Layouts;
    using SecondMonitor.ViewModels.Settings.Model.Layout;
    using TelemetryApplication.Settings.DTO;
    using TelemetryApplication.Settings.Workspace;
    using ViewModels.SettingsWindow.Workspace;
    using Workspaces.Providers;
    using MessageBox = System.Windows.Forms.MessageBox;

    public class WorkspaceEditorController : IChildController<ISettingsWindowController>
    {
        private const string WorkspaceExportFileExtension = ".wspace";
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IWorkspaceProvider _workspaceProvider;
        private readonly IChartSetProvider _chartSetProvider;
        private readonly ILayoutEditorManipulator _layoutEditorManipulator;
        private readonly IWindowService _windowService;
        private readonly IDialogService _dialogService;
        private WorkspacesSettingsViewModel _workspacesSettingsViewModel;

        public WorkspaceEditorController(IWorkspaceProvider workspaceProvider, IChartSetProvider chartSetProvider, ILayoutEditorManipulator layoutEditorManipulator, IWindowService windowService, IDialogService dialogService)
        {
            _workspaceProvider = workspaceProvider;
            _chartSetProvider = chartSetProvider;
            _layoutEditorManipulator = layoutEditorManipulator;
            _layoutEditorManipulator.NewRowSize = new LengthDefinitionSetting()
            {
                ManualSize = 200,
                SizeKind = SizeKind.Manual,
            };

            _layoutEditorManipulator.NewColumnSize = new LengthDefinitionSetting()
            {
                ManualSize = 100,
                SizeKind = SizeKind.Remaining,
            };
            _windowService = windowService;
            _dialogService = dialogService;
        }

        public ISettingsWindowController ParentController { get; set; }

        public Task StartControllerAsync()
        {
            return Task.CompletedTask;
        }

        public void UpdateWorkspaceViewmodel(WorkspacesSettingsViewModel workspacesSettingsViewModel, TelemetrySettingsDto telemetrySettingsDto)
        {
            _workspacesSettingsViewModel = workspacesSettingsViewModel;
            BindCommands();
            workspacesSettingsViewModel.RefreshAvailableChartSets(_chartSetProvider.GetAllChartSets().OrderBy(x => x.Name));
            workspacesSettingsViewModel.ClearWorkspaces();
            foreach (Workspace workspace in _workspaceProvider.GetAllWorkspaces().OrderBy(x => x.IsFavourite).ThenBy(x => x.Name))
            {
                workspacesSettingsViewModel.AddWorkSpace(workspace);
            }

            workspacesSettingsViewModel.SelectedWorkspace(telemetrySettingsDto.SelectedWorkspaceGuid);
        }

        private void BindCommands()
        {
            _workspacesSettingsViewModel.NewWorkspaceCommand = new RelayCommand(CreateNewWorkspace);
            _workspacesSettingsViewModel.RemoveSelectedWorkspaceCommand = new RelayCommand(RemoveSelectedWorkspace);
            _workspacesSettingsViewModel.DuplicateWorkspaceCommand = new RelayCommand(DuplicateSelectedWorkspace);
            _workspacesSettingsViewModel.ExportWorkspaceCommand = new RelayCommand(ExportSelectedWorkspace);
            _workspacesSettingsViewModel.ImportWorkspaceCommand = new RelayCommand(ImportWorkspace);

            _workspacesSettingsViewModel.NewChartListSetCommand = new RelayCommand(CreateChartSet);
            _workspacesSettingsViewModel.DuplicateChartListCommand = new RelayCommand(DuplicateChartSet);
            _workspacesSettingsViewModel.RemoveSelectedChartListCommand = new RelayCommand(RemoveSelectedChartSet);
            _workspacesSettingsViewModel.OpenChartListDetailCommand = new RelayCommand(OpenChartListDetail);
        }

        private void OpenChartListDetail()
        {
            ChartSetSummaryViewModel selectedChartSet = _workspacesSettingsViewModel.SelectedChartSet;
            if (selectedChartSet == null)
            {
                return;
            }

            selectedChartSet.LayoutDescription.Name = selectedChartSet.Name;
            _layoutEditorManipulator.SetLayoutDescription(selectedChartSet.LayoutDescription);
            _layoutEditorManipulator.LayoutEditorViewModel.IsBottomBarVisible = false;
            _layoutEditorManipulator.LayoutEditorViewModel.IsLayoutNameVisible = true;
            bool? returnValue = _windowService.OpenDialog(new BasicDialogViewModel(_layoutEditorManipulator.LayoutEditorViewModel), selectedChartSet.Name, WindowState.Maximized, SizeToContent.Manual, WindowStartupLocation.CenterScreen);
            if (returnValue != true)
            {
                return;
            }

            var newLayoutDescription = _layoutEditorManipulator.GetLayoutDescription();
            selectedChartSet.Name = newLayoutDescription.Name;
            selectedChartSet.LayoutDescription = newLayoutDescription;

            var selectedWorkspace = _workspacesSettingsViewModel.SelectedWorkspaceSetting;
            selectedWorkspace?.UpdateChartSetName(selectedChartSet.Guid, selectedChartSet.Name);
        }

        private void DuplicateChartSet()
        {
            if (_workspacesSettingsViewModel.SelectedChartSet == null)
            {
                return;
            }

            var duplicateChartSet = _workspacesSettingsViewModel.SelectedChartSet.SaveToNewModel();
            duplicateChartSet.Name = "Copy - " + duplicateChartSet.Name;
            duplicateChartSet.Guid = Guid.NewGuid();

            _workspacesSettingsViewModel.AddChartSet(duplicateChartSet);
            _workspacesSettingsViewModel.SelectChartSet(duplicateChartSet.Guid);
        }

        private void RemoveSelectedChartSet()
        {
            _workspacesSettingsViewModel.RemoveSelectedChartList();
        }

        private void CreateChartSet()
        {
            var newChartSet = new ChartSet()
            {
                Name = "New Chart Set",
                LayoutDescription = new LayoutDescription()
                {
                    Name = string.Empty,
                    RootElement = RowsDefinitionSettingBuilder.Create().WithContent(null, SizeKind.Manual, 200).WithContent(null, SizeKind.Manual, 200).Build(),
                }
            };

            _workspacesSettingsViewModel.AddChartSet(newChartSet);
            _workspacesSettingsViewModel.SelectChartSet(newChartSet.Guid);
            OpenChartListDetail();
        }

        private void RemoveSelectedWorkspace()
        {
            _workspacesSettingsViewModel.RemoveSelectedWorkspace();
        }

        private void CreateNewWorkspace()
        {
            var newWorkspace = new Workspace()
            {
                Name = "New Workspace",
            };

            _workspacesSettingsViewModel.AddWorkSpace(newWorkspace);
            _workspacesSettingsViewModel.SelectedWorkspace(newWorkspace.Guid);
        }

        private void DuplicateSelectedWorkspace()
        {
            if (_workspacesSettingsViewModel.SelectedWorkspaceSetting == null)
            {
                return;
            }

            var duplicatedWorkspace = _workspacesSettingsViewModel.SelectedWorkspaceSetting.SaveToNewModel();
            duplicatedWorkspace.Name = "Copy - " + duplicatedWorkspace.Name;
            duplicatedWorkspace.Guid = Guid.NewGuid();

            _workspacesSettingsViewModel.AddWorkSpace(duplicatedWorkspace);
            _workspacesSettingsViewModel.SelectedWorkspace(duplicatedWorkspace.Guid);
        }

        private void ImportWorkspace()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.DefaultExt = WorkspaceExportFileExtension;
                openFileDialog.Filter = $"Workspace Files (*{WorkspaceExportFileExtension})|*{WorkspaceExportFileExtension}";
                DialogResult result = openFileDialog.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(openFileDialog.FileName) && TryImportWorkspace(openFileDialog.FileName, out WorkspaceExportDto workspaceExportDto))
                {
                    ImportWorkspace(workspaceExportDto);
                }
            }
        }

        private void ImportWorkspace(WorkspaceExportDto workspaceExportDto)
        {
            if (_workspacesSettingsViewModel.Workspaces.Any(x => x.WorkspaceId == workspaceExportDto.Workspace.Guid))
            {
                if (_dialogService.ShowYesNoDialog("Workspace exists", "Workspace already exists in application.\nDo you want to overwrite the existing workspace?") == false)
                {
                    return;
                }

                _workspacesSettingsViewModel.RemoveWorkspace(workspaceExportDto.Workspace.Guid);
            }

            var existingChartSetsGuid = _workspacesSettingsViewModel.AvailableChartSets.Select(x => x.Guid).ToList();
            var existingChartSets = workspaceExportDto.ChartSets.Where(x => existingChartSetsGuid.Contains(x.Guid)).ToList();
            if (existingChartSets.Any())
            {
                StringBuilder sb = new StringBuilder("These chart sets already exist in application:\n");
                existingChartSets.ForEach(x => sb.AppendLine(x.Name));
                sb.AppendLine("Do you want to overwrite them?\nNo will make the workspace use the already existing version of these chart sets.");
                if (_dialogService.ShowYesNoDialog("Chart Sets exists", sb.ToString()))
                {
                    existingChartSets.ForEach(x => _workspacesSettingsViewModel.ReplaceChartSetLayout(x.Guid, x.LayoutDescription));
                }
            }

            var chartSetToImport = workspaceExportDto.ChartSets.Except(existingChartSets).ToList();
            chartSetToImport.ForEach(_workspacesSettingsViewModel.AddChartSet);
            _workspacesSettingsViewModel.AddWorkSpace(workspaceExportDto.Workspace);
            _workspacesSettingsViewModel.SelectedWorkspace(workspaceExportDto.Workspace.Guid);
            MessageBox.Show("File Imported successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExportSelectedWorkspace()
        {
            var selectedWorkspace = _workspacesSettingsViewModel.SelectedWorkspaceSetting;
            if (selectedWorkspace == null)
            {
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.DefaultExt = WorkspaceExportFileExtension;
                saveFileDialog.Filter = $"Workspace Files (*{WorkspaceExportFileExtension})|*{WorkspaceExportFileExtension}";
                DialogResult result = saveFileDialog.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(saveFileDialog.FileName))
                {
                    ExportWorkspace(selectedWorkspace.SaveToNewModel(), _workspacesSettingsViewModel.AvailableChartSets.ToList(), saveFileDialog.FileName);
                }
            }

            MessageBox.Show("File Exported successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExportWorkspace(Workspace workspace, List<ChartSetSummaryViewModel> chartSetSummaryViewModels, string fileName)
        {
            WorkspaceExportDto workspaceExportDto = new WorkspaceExportDto()
            {
                Workspace = workspace,
                ChartSets = chartSetSummaryViewModels.Where(x => !x.IsBuildIn && workspace.ChartSetsGuid.Contains(x.Guid.ToString())).Select(x => x.SaveToNewModel()).ToList(),
            };
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(WorkspaceExportDto));
            using (FileStream file = File.Exists(fileName) ? File.Open(fileName, FileMode.Truncate) : File.Create(fileName))
            {
                xmlSerializer.Serialize(file, workspaceExportDto);
            }
        }

        private bool TryImportWorkspace(string fileName, out WorkspaceExportDto workspaceExportDto)
        {
            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(WorkspaceExportDto));
                using (FileStream file = File.Open(fileName, FileMode.Open))
                {
                    workspaceExportDto = xmlSerializer.Deserialize(file) as WorkspaceExportDto;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unable to import Workspace");
                MessageBox.Show($"Error While Loading Workspace:\n {ex.Message}", "Error While Loading", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                workspaceExportDto = null;
            }

            return workspaceExportDto != null;
        }

        public Task StopControllerAsync()
        {
            return Task.CompletedTask;
        }
    }
}