namespace SecondMonitor.ViewModels.Layouts.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Input;
    using Settings.Model.Layout;

    public class NamedContentPropertiesViewModel : AbstractViewModel<NamedContentSetting>
    {
        private string _selectedContent;
        private List<string> _allowableContent;

        public NamedContentPropertiesViewModel()
        {
            GenericSettingsPropertiesViewModel = new GenericSettingsPropertiesViewModel();
        }

        public GenericSettingsPropertiesViewModel GenericSettingsPropertiesViewModel { get; }

        public string SelectedContent
        {
            get => _selectedContent;
            set => SetProperty(ref _selectedContent, value);
        }

        public ICommand RemoveCommand { get; set; }

        public List<string> AllowableContent
        {
            get => _allowableContent;
            set => SetProperty(ref _allowableContent, value);
        }

        protected override void ApplyModel(NamedContentSetting model)
        {
            SelectedContent = model.ContentName;
            GenericSettingsPropertiesViewModel.FromModel(model);
        }

        public override NamedContentSetting SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}