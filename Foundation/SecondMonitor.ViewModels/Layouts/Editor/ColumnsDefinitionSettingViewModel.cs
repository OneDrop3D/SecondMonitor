namespace SecondMonitor.ViewModels.Layouts.Editor
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows.Input;
    using Contracts.Commands;
    using Settings.Model.Layout;
    using ViewModels.Factory;

    public class ColumnsDefinitionSettingViewModel : AbstractViewModel<ColumnsDefinitionSetting>, ILayoutConfigurationViewModel
    {
        private readonly ColumnsDefinitionPropertiesViewModel _columnsDefinitionPropertiesViewModel;
        private readonly IViewModelFactory _viewModelFactory;
        private bool _isSelected;
        private ICommand _selectCommand;

        public ColumnsDefinitionSettingViewModel(IViewModelFactory viewModelFactory)
        {
            ColumnsConfigurationViewModels = new ObservableCollection<IViewModel>();
            _viewModelFactory = viewModelFactory;
            _columnsDefinitionPropertiesViewModel = _viewModelFactory.Create<ColumnsDefinitionPropertiesViewModel>();
            _columnsDefinitionPropertiesViewModel.RemoveCommand = new RelayCommand(RemoveElement);
            _columnsDefinitionPropertiesViewModel.AddNewColumnCommand = new RelayCommand(AddNewColumn);
            SelectCommand = new RelayCommand(Select);
        }

        public ObservableCollection<IViewModel> ColumnsConfigurationViewModels { get; }

        public ILayoutConfigurationViewModelFactory LayoutConfigurationViewModelFactory { get; set; }
        public ILayoutEditorManipulator LayoutEditorManipulator { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public IViewModel PropertiesViewModel => _columnsDefinitionPropertiesViewModel;

        public ICommand SelectCommand
        {
            get => _selectCommand;
            set => SetProperty(ref _selectCommand, value);
        }

        protected override void ApplyModel(ColumnsDefinitionSetting model)
        {
            if (LayoutConfigurationViewModelFactory == null)
            {
                return;
            }

            ColumnsConfigurationViewModels.Clear();
            for (int i = 0; i < model.ColumnsCount; i++)
            {
                CreateAndAddColumn(model.ColumnsContent[i], model.ColumnsSize[i]);
            }

            _columnsDefinitionPropertiesViewModel.FromModel(model);
            InitializeModificationButtons();
        }

        private void CreateAndAddColumn(GenericContentSetting content, LengthDefinitionSetting columnLength)
        {
            ColumnDefinitionSettingViewModel newViewModel = _viewModelFactory.Create<ColumnDefinitionSettingViewModel>();
            newViewModel.LayoutConfigurationViewModelFactory = LayoutConfigurationViewModelFactory;
            newViewModel.LayoutEditorManipulator = LayoutEditorManipulator;
            newViewModel.ColumnContent = LayoutConfigurationViewModelFactory.Create(content);
            newViewModel.ColumnPropertiesViewModel.FromModel(columnLength);
            newViewModel.ParentViewModel = this;
            ColumnsConfigurationViewModels.Add(newViewModel);
            LayoutEditorManipulator.RegisterLayoutContainer(newViewModel);
        }

        public override ColumnsDefinitionSetting SaveToNewModel()
        {
            List<ColumnDefinitionSettingViewModel> columnModels = ColumnsConfigurationViewModels.OfType<ColumnDefinitionSettingViewModel>().ToList();
            ColumnsDefinitionSetting newModel = new ColumnsDefinitionSetting()
            {
                ColumnsCount = ColumnsConfigurationViewModels.Count,
                ColumnsSize = columnModels.Select(x => x.ColumnPropertiesViewModel.SaveToNewModel()).ToArray(),
                ColumnsContent = columnModels.Select(x => LayoutConfigurationViewModelFactory.ToContentSettings(x.ColumnContent)).ToArray()
            };
            _columnsDefinitionPropertiesViewModel.ApplyGenericSettings(newModel);
            return newModel;
        }

        private void Select()
        {
            LayoutEditorManipulator?.Select(this);
        }

        public void MoveColumnLeft(ColumnDefinitionSettingViewModel column)
        {
            int index = ColumnsConfigurationViewModels.IndexOf(column);
            if (index == 0)
            {
                return;
            }

            ColumnsConfigurationViewModels.RemoveAt(index);
            ColumnsConfigurationViewModels.Insert(index - 1, column);
            InitializeModificationButtons();
        }

        public void MoveColumnRight(ColumnDefinitionSettingViewModel column)
        {
            int index = ColumnsConfigurationViewModels.IndexOf(column);
            if (index + 1 == ColumnsConfigurationViewModels.Count)
            {
                return;
            }

            ColumnsConfigurationViewModels.RemoveAt(index);
            ColumnsConfigurationViewModels.Insert(index + 1, column);
            InitializeModificationButtons();
        }

        private void InitializeModificationButtons()
        {
            List<ColumnDefinitionSettingViewModel> columnModels = ColumnsConfigurationViewModels.OfType<ColumnDefinitionSettingViewModel>().ToList();
            for (int i = 0; i < columnModels.Count; i++)
            {
                var columnSettings = columnModels[i];
                columnSettings.ColumnNumber = i + 1;
                columnSettings.ColumnPropertiesViewModel.IsMoveLeftEnabled = i != 0;
                columnSettings.ColumnPropertiesViewModel.IsMoveRightEnabled = i + 1 != columnModels.Count;
            }
        }

        public void RemoveColumn(ColumnDefinitionSettingViewModel column)
        {
            ColumnsConfigurationViewModels.Remove(column);
            LayoutEditorManipulator.UnRegisterLayoutContainer(column);
            InitializeModificationButtons();
        }

        private void RemoveElement()
        {
            LayoutEditorManipulator.RemoveLayoutElement(this);
        }

        private void AddNewColumn()
        {
            (GenericContentSetting content, LengthDefinitionSetting columnSize) = LayoutEditorManipulator.CreateDefaultColumnLayout();
            CreateAndAddColumn(content, columnSize);
            InitializeModificationButtons();
        }
    }
}