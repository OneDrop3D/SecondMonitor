namespace SecondMonitor.ViewModels.Layouts.Editor
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows.Input;
    using Contracts.Commands;
    using Settings.Model.Layout;
    using ViewModels.Factory;

    public class RowsDefinitionSettingViewModel : AbstractViewModel<RowsDefinitionSetting>, ILayoutConfigurationViewModel
    {
        private readonly RowsDefinitionPropertiesViewModel _rowsDefinitionPropertiesViewModel;
        private readonly IViewModelFactory _viewModelFactory;
        private bool _isSelected;
        private ICommand _selectCommand;

        public RowsDefinitionSettingViewModel(IViewModelFactory viewModelFactory)
        {
            RowsConfigurationViewModels = new ObservableCollection<IViewModel>();
            _viewModelFactory = viewModelFactory;
            _rowsDefinitionPropertiesViewModel = _viewModelFactory.Create<RowsDefinitionPropertiesViewModel>();
            _rowsDefinitionPropertiesViewModel.RemoveCommand = new RelayCommand(RemoveElement);
            _rowsDefinitionPropertiesViewModel.AddNewRowCommand = new RelayCommand(AddNewRow);
            SelectCommand = new RelayCommand(Select);
        }

        public ObservableCollection<IViewModel> RowsConfigurationViewModels { get; }

        public ILayoutConfigurationViewModelFactory LayoutConfigurationViewModelFactory { get; set; }
        public ILayoutEditorManipulator LayoutEditorManipulator { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public IViewModel PropertiesViewModel => _rowsDefinitionPropertiesViewModel;

        public ICommand SelectCommand
        {
            get => _selectCommand;
            set => SetProperty(ref _selectCommand, value);
        }

        protected override void ApplyModel(RowsDefinitionSetting model)
        {
            if (LayoutConfigurationViewModelFactory == null)
            {
                return;
            }

            RowsConfigurationViewModels.Clear();
            for (int i = 0; i < model.RowCount; i++)
            {
                CreateAndAddRow(model.RowsContent[i], model.RowsSize[i]);
            }

            _rowsDefinitionPropertiesViewModel.FromModel(model);
            InitializeModificationButtons();
        }

        public override RowsDefinitionSetting SaveToNewModel()
        {
            List<RowDefinitionSettingViewModel> rowModels = RowsConfigurationViewModels.OfType<RowDefinitionSettingViewModel>().ToList();
            RowsDefinitionSetting newModel = new RowsDefinitionSetting()
            {
                RowCount = RowsConfigurationViewModels.Count,
                RowsSize = rowModels.Select(x => x.RowPropertiesViewModel.SaveToNewModel()).ToArray(),
                RowsContent = rowModels.Select(x => LayoutConfigurationViewModelFactory.ToContentSettings(x.RowContent)).ToArray(),
            };
            _rowsDefinitionPropertiesViewModel.ApplyGenericSettings(newModel);
            return newModel;
        }

        public void MoveRowUp(RowDefinitionSettingViewModel column)
        {
            int index = RowsConfigurationViewModels.IndexOf(column);
            if (index == 0)
            {
                return;
            }

            RowsConfigurationViewModels.RemoveAt(index);
            RowsConfigurationViewModels.Insert(index - 1, column);
            InitializeModificationButtons();
        }

        public void MoveRowDown(RowDefinitionSettingViewModel column)
        {
            int index = RowsConfigurationViewModels.IndexOf(column);
            if (index + 1 == RowsConfigurationViewModels.Count)
            {
                return;
            }

            RowsConfigurationViewModels.RemoveAt(index);
            RowsConfigurationViewModels.Insert(index + 1, column);
            InitializeModificationButtons();
        }

        public void RemoveRow(RowDefinitionSettingViewModel row)
        {
            RowsConfigurationViewModels.Remove(row);
            InitializeModificationButtons();
            LayoutEditorManipulator.UnRegisterLayoutContainer(row);
        }

        private void CreateAndAddRow(GenericContentSetting content, LengthDefinitionSetting rowHeight)
        {
            RowDefinitionSettingViewModel newViewModel = _viewModelFactory.Create<RowDefinitionSettingViewModel>();
            newViewModel.LayoutConfigurationViewModelFactory = LayoutConfigurationViewModelFactory;
            newViewModel.RowContent = LayoutConfigurationViewModelFactory.Create(content);
            newViewModel.LayoutEditorManipulator = LayoutEditorManipulator;
            newViewModel.RowPropertiesViewModel.FromModel(rowHeight);
            newViewModel.ParentViewModel = this;
            RowsConfigurationViewModels.Add(newViewModel);
            LayoutEditorManipulator.RegisterLayoutContainer(newViewModel);
        }

        private void InitializeModificationButtons()
        {
            List<RowDefinitionSettingViewModel> rowModels = RowsConfigurationViewModels.OfType<RowDefinitionSettingViewModel>().ToList();
            for (int i = 0; i < rowModels.Count; i++)
            {
                var columnSettings = rowModels[i];
                columnSettings.RowNumber = i + 1;
                columnSettings.RowPropertiesViewModel.IsMoveUpEnabled = i != 0;
                columnSettings.RowPropertiesViewModel.IsMoveDownEnabled = i + 1 != rowModels.Count;
            }
        }

        private void Select()
        {
            LayoutEditorManipulator?.Select(this);
        }

        private void RemoveElement()
        {
            LayoutEditorManipulator.RemoveLayoutElement(this);
        }

        private void AddNewRow()
        {
            (GenericContentSetting content, LengthDefinitionSetting rowSize) = LayoutEditorManipulator.CreateDefaultRowLayout();
            CreateAndAddRow(content, rowSize);
            InitializeModificationButtons();
        }
    }
}