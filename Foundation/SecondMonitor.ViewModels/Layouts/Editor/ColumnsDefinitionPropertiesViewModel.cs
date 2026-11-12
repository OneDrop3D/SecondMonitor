namespace SecondMonitor.ViewModels.Layouts.Editor
{
    using System;
    using System.Windows.Input;
    using Settings.Model.Layout;

    public class ColumnsDefinitionPropertiesViewModel : AbstractViewModel<ColumnsDefinitionSetting>
    {
        private ICommand _removeCommand;
        private ICommand _addNewColumnCommand;
        private bool _isGridSplitterEnabled;

        public ColumnsDefinitionPropertiesViewModel()
        {
            GenericSettingsPropertiesViewModel = new GenericSettingsPropertiesViewModel();
        }

        public GenericSettingsPropertiesViewModel GenericSettingsPropertiesViewModel { get; }

        public ICommand RemoveCommand
        {
            get => _removeCommand;
            set => SetProperty(ref _removeCommand, value);
        }

        public ICommand AddNewColumnCommand
        {
            get => _addNewColumnCommand;
            set => SetProperty(ref _addNewColumnCommand, value);
        }

        public bool IsGridSplitterEnabled
        {
            get => _isGridSplitterEnabled;
            set => SetProperty(ref _isGridSplitterEnabled, value);
        }

        protected override void ApplyModel(ColumnsDefinitionSetting model)
        {
            GenericSettingsPropertiesViewModel.FromModel(model);
            IsGridSplitterEnabled = model.AddGridSplitter;
        }

        public override ColumnsDefinitionSetting SaveToNewModel()
        {
            throw new NotSupportedException();
        }

        public void ApplyGenericSettings(ColumnsDefinitionSetting newModel)
        {
            GenericSettingsPropertiesViewModel.ApplyGenericSettings(newModel);
            newModel.AddGridSplitter = IsGridSplitterEnabled;
        }
    }
}