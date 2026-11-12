namespace SecondMonitor.ViewModels.Layouts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Windows.Forms;
    using System.Windows.Input;
    using Contracts.Commands;
    using Editor;
    using Factory;
    using Newtonsoft.Json;
    using NLog;
    using Settings.Model.Layout;
    using ViewModels.Factory;

    public class LayoutEditorManipulator : ILayoutEditorManipulator
    {
        private readonly List<ILayoutContainer> _layoutContainers;
        private readonly IDefaultLayoutFactory _defaultLayoutFactory;
        private readonly ILayoutConfigurationViewModelFactory _layoutConfigurationViewModelFactory;
        private readonly ILayoutElementNamesProvider _layoutElementNamesProvider;
        private ILayoutConfigurationViewModel _selectedViewModel;

        public LayoutEditorManipulator(IDefaultLayoutFactory defaultLayoutFactory, ILayoutConfigurationViewModelFactory layoutConfigurationViewModelFactory, IViewModelFactory viewModelFactory, ILayoutElementNamesProvider layoutElementNamesProvider)
        {
            _layoutContainers = new List<ILayoutContainer>();
            _defaultLayoutFactory = defaultLayoutFactory;
            _layoutConfigurationViewModelFactory = layoutConfigurationViewModelFactory;
            _layoutElementNamesProvider = layoutElementNamesProvider;
            layoutConfigurationViewModelFactory.LayoutEditorManipulator = this;
            LayoutEditorViewModel = viewModelFactory.Create<LayoutEditorViewModel>();
            LayoutEditorViewModel.RevertToDefaultLayoutCommand = new RelayCommand(RevertToDefaultLayout);
            LayoutEditorViewModel.CreateEmptyLayoutCommand = new RelayCommand(CreateEmpty);
            LayoutEditorViewModel.LoadLayoutCommand = new RelayCommand(LoadLayout);
            LayoutEditorViewModel.SaveLayoutCommand = new RelayCommand(SaveLayout);
            RegisterLayoutContainer(LayoutEditorViewModel.LayoutViewModel);
            NewRowSize = new LengthDefinitionSetting()
            {
                SizeKind = SizeKind.Automatic,
                ManualSize = 100,
            };

            NewColumnSize = new LengthDefinitionSetting()
            {
                SizeKind = SizeKind.Automatic,
                ManualSize = 100,
            };
        }

        public LengthDefinitionSetting NewRowSize { get; set; }

        public LengthDefinitionSetting NewColumnSize { get; set; }

        public ICommand ApplyLayoutCommand
        {
            get => LayoutEditorViewModel.ApplyLayoutCommand;
            set => LayoutEditorViewModel.ApplyLayoutCommand = value;
        }

        public LayoutEditorViewModel LayoutEditorViewModel { get; }

        private static void SaveLayout(LayoutDescription layoutDescription, string filePath)
        {
            try
            {
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    Formatting = Formatting.Indented
                };
                File.WriteAllText(filePath, JsonConvert.SerializeObject(layoutDescription, settings));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error While Saving Layout:\n {ex.Message}", "Error While Saving", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                LogManager.GetCurrentClassLogger().Error(ex, "Error while saving display settingsView.");
            }
        }

        private static bool TryLoadLayout(string filePath, out LayoutDescription layoutDescription)
        {
            try
            {
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    Formatting = Formatting.Indented
                };

                object deserializedObject = JsonConvert.DeserializeObject<LayoutDescription>(File.ReadAllText(filePath), settings);
                if (deserializedObject is LayoutDescription ld)
                {
                    layoutDescription = ld;
                    return true;
                }

                MessageBox.Show($@"Error While Loading Layout. Loaded Layout is empty.", @"Error While Loading", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                LogManager.GetCurrentClassLogger().Error("Error while loading display settingsView.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error While Loading Layout:\n {ex.Message}", "Error While Loading", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                LogManager.GetCurrentClassLogger().Error(ex, "Error while loading display settingsView.");
            }

            layoutDescription = null;
            return false;
        }

        public void Select(ILayoutConfigurationViewModel layoutElement)
        {
            if (_selectedViewModel != null)
            {
                _selectedViewModel.IsSelected = false;
            }

            _selectedViewModel = layoutElement;

            if (_selectedViewModel != null)
            {
                _selectedViewModel.IsSelected = true;
                LayoutEditorViewModel.SelectedElementPropertiesViewModel = _selectedViewModel.PropertiesViewModel;
            }
        }

        public LayoutDescription CreateDefaultLayout()
        {
            return _defaultLayoutFactory.CreateDefaultLayout();
        }

        public void SetLayoutDescription(LayoutDescription layoutDescription)
        {
            ClearLayoutContainers();
            LayoutEditorViewModel.LayoutName = layoutDescription.Name;
            LayoutEditorViewModel.LayoutViewModel.LayoutElement = _layoutConfigurationViewModelFactory.Create(layoutDescription?.RootElement ?? _defaultLayoutFactory.CreateDefaultLayout().RootElement);
        }

        public LayoutDescription GetLayoutDescription()
        {
            return new LayoutDescription()
            {
                Name = LayoutEditorViewModel.LayoutName,
                RootElement = _layoutConfigurationViewModelFactory.ToContentSettings(LayoutEditorViewModel.LayoutViewModel.LayoutElement),
            };
        }

        public void RegisterLayoutContainer(ILayoutContainer layoutContainer)
        {
            _layoutContainers.Add(layoutContainer);
        }

        public void UnRegisterLayoutContainer(ILayoutContainer layoutContainer)
        {
            _layoutContainers.Remove(layoutContainer);
        }

        public void ClearLayoutContainers()
        {
            _layoutContainers.Clear();
            RegisterLayoutContainer(LayoutEditorViewModel.LayoutViewModel);
        }

        public void RemoveLayoutElement(ILayoutConfigurationViewModel layoutElement)
        {
            ILayoutContainer layoutContainer = _layoutContainers.FirstOrDefault(x => x.LayoutElement == layoutElement);
            if (layoutContainer == null)
            {
                return;
            }

            layoutContainer.SetLayout(_layoutConfigurationViewModelFactory.CreateEmpty());
            Select(layoutContainer.LayoutElement);
        }

        public void ReplaceForNamedContent(ILayoutConfigurationViewModel layoutElement)
        {
            ILayoutContainer layoutContainer = _layoutContainers.FirstOrDefault(x => x.LayoutElement == layoutElement);
            if (layoutContainer == null)
            {
                return;
            }

            layoutContainer.SetLayout(CreateNamedContent());
            Select(layoutContainer.LayoutElement);
        }

        public void ReplaceForRows(ILayoutConfigurationViewModel layoutElement)
        {
            ILayoutContainer layoutContainer = _layoutContainers.FirstOrDefault(x => x.LayoutElement == layoutElement);
            if (layoutContainer == null)
            {
                return;
            }

            layoutContainer.SetLayout(CreateRowsContent());
            Select(layoutContainer.LayoutElement);
        }

        public void ReplaceForColumns(ILayoutConfigurationViewModel layoutElement)
        {
            ILayoutContainer layoutContainer = _layoutContainers.FirstOrDefault(x => x.LayoutElement == layoutElement);
            if (layoutContainer == null)
            {
                return;
            }

            layoutContainer.SetLayout(CreateColumnsContent());
            Select(layoutContainer.LayoutElement);
        }

        public (GenericContentSetting Content, LengthDefinitionSetting RowSize) CreateDefaultRowLayout()
        {
            return (null, NewRowSize);
        }

        public (GenericContentSetting Content, LengthDefinitionSetting ColumnSize) CreateDefaultColumnLayout()
        {
            LengthDefinitionSetting newColumnSize = new LengthDefinitionSetting()
            {
                SizeKind = NewColumnSize.SizeKind,
                ManualSize = NewColumnSize.ManualSize,
            };
            return (null, newColumnSize);
        }

        private ILayoutConfigurationViewModel CreateNamedContent()
        {
            NamedContentSetting newContentSetting = new NamedContentSetting()
            {
                ContentName = _layoutElementNamesProvider.GetAllowableContentNames()[0],
            };
            return _layoutConfigurationViewModelFactory.Create(newContentSetting);
        }

        private ILayoutConfigurationViewModel CreateRowsContent()
        {
            RowsDefinitionSetting rowsDefinitionSetting = new RowsDefinitionSetting()
            {
                RowCount = 2,
                RowsSize = new[] { new LengthDefinitionSetting(NewRowSize.SizeKind, NewRowSize.ManualSize), new LengthDefinitionSetting(NewRowSize.SizeKind, NewRowSize.ManualSize) },
                RowsContent = new GenericContentSetting[] { null, null }
            };
            return _layoutConfigurationViewModelFactory.Create(rowsDefinitionSetting);
        }

        private ILayoutConfigurationViewModel CreateColumnsContent()
        {
            ColumnsDefinitionSetting columnsDefinitionSetting = new ColumnsDefinitionSetting()
            {
                ColumnsCount = 2,
                ColumnsSize = new[] { new LengthDefinitionSetting(NewColumnSize.SizeKind, NewColumnSize.ManualSize), new LengthDefinitionSetting(NewColumnSize.SizeKind, NewColumnSize.ManualSize) },
                ColumnsContent = new GenericContentSetting[] { null, null }
            };
            return _layoutConfigurationViewModelFactory.Create(columnsDefinitionSetting);
        }

        public List<string> GetAllowableContentNames()
        {
            return _layoutElementNamesProvider.GetAllowableContentNames();
        }

        private void RevertToDefaultLayout()
        {
            LayoutEditorViewModel.LayoutViewModel.LayoutElement = _layoutConfigurationViewModelFactory.Create(_defaultLayoutFactory.CreateDefaultLayout().RootElement);
        }

        private void CreateEmpty()
        {
            ClearLayoutContainers();
            LayoutEditorViewModel.LayoutViewModel.LayoutElement = _layoutConfigurationViewModelFactory.CreateEmpty();
        }

        private void SaveLayout()
        {
            LayoutDescription layoutDescription = GetLayoutDescription();
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.DefaultExt = ".json";
                saveFileDialog.Filter = @"Json Files (*.json)|*.json";
                DialogResult result = saveFileDialog.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(saveFileDialog.FileName))
                {
                    SaveLayout(layoutDescription, saveFileDialog.FileName);
                }
            }
        }

        private void LoadLayout()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.DefaultExt = ".json";
                openFileDialog.Filter = @"Json Files (*.json)|*.json";
                DialogResult result = openFileDialog.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(openFileDialog.FileName) && TryLoadLayout(openFileDialog.FileName, out LayoutDescription layoutDescription))
                {
                    SetLayoutDescription(layoutDescription);
                }
            }
        }
    }
}