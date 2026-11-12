namespace SecondMonitor.ViewModels.Layouts.Editor
{
    using System;
    using System.Windows.Input;
    using Settings.Model.Layout;

    public class RowsDefinitionPropertiesViewModel : AbstractViewModel<RowsDefinitionSetting>
    {
        private ICommand _removeCommand;
        private ICommand _addNewRowCommand;
        private bool _isGridSplitterEnabled;

        public RowsDefinitionPropertiesViewModel()
        {
            GenericSettingsPropertiesViewModel = new GenericSettingsPropertiesViewModel();
        }

        public GenericSettingsPropertiesViewModel GenericSettingsPropertiesViewModel { get; }

        public ICommand RemoveCommand
        {
            get => _removeCommand;
            set => SetProperty(ref _removeCommand, value);
        }

        public ICommand AddNewRowCommand
        {
            get => _addNewRowCommand;
            set => SetProperty(ref _addNewRowCommand, value);
        }

        public bool IsGridSplitterEnabled
        {
            get => _isGridSplitterEnabled;
            set => SetProperty(ref _isGridSplitterEnabled, value);
        }

        protected override void ApplyModel(RowsDefinitionSetting model)
        {
            GenericSettingsPropertiesViewModel.FromModel(model);
            IsGridSplitterEnabled = model.AddGridSplitters;
        }

        public override RowsDefinitionSetting SaveToNewModel()
        {
            throw new NotSupportedException();
        }

        public void ApplyGenericSettings(RowsDefinitionSetting newModel)
        {
            GenericSettingsPropertiesViewModel.ApplyGenericSettings(newModel);
            newModel.AddGridSplitters = IsGridSplitterEnabled;
        }
    }
}