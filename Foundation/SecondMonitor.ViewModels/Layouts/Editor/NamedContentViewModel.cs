namespace SecondMonitor.ViewModels.Layouts.Editor
{
    using System.ComponentModel;
    using System.Windows.Input;
    using Contracts.Commands;
    using Settings.Model.Layout;

    public class NamedContentViewModel : AbstractViewModel<NamedContentSetting>, ILayoutConfigurationViewModel
    {
        private readonly NamedContentPropertiesViewModel _namedContentPropertiesViewModel;
        private string _contentName;
        private bool _isSelected;
        private ICommand _selectCommand;

        public NamedContentViewModel()
        {
            SelectCommand = new RelayCommand(Select);
            _namedContentPropertiesViewModel = new NamedContentPropertiesViewModel();
            _namedContentPropertiesViewModel.RemoveCommand = new RelayCommand(Remove);
            _namedContentPropertiesViewModel.PropertyChanged += NamedContentPropertiesViewModelOnPropertyChanged;
        }

        public ILayoutConfigurationViewModelFactory LayoutConfigurationViewModelFactory { get; set; }
        public ILayoutEditorManipulator LayoutEditorManipulator { get; set; }

        public string ContentName
        {
            get => _contentName;
            set => SetProperty(ref _contentName, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public IViewModel PropertiesViewModel => _namedContentPropertiesViewModel;

        public ICommand SelectCommand
        {
            get => _selectCommand;
            set => SetProperty(ref _selectCommand, value);
        }

        protected override void ApplyModel(NamedContentSetting model)
        {
            ContentName = model.ContentName;
            _namedContentPropertiesViewModel.AllowableContent = LayoutEditorManipulator.GetAllowableContentNames();
            _namedContentPropertiesViewModel.FromModel(model);
        }

        public override NamedContentSetting SaveToNewModel()
        {
            NamedContentSetting newModel = new NamedContentSetting()
            {
                ContentName = ContentName,
            };
            _namedContentPropertiesViewModel.GenericSettingsPropertiesViewModel.ApplyGenericSettings(newModel);
            return newModel;
        }

        private void Select()
        {
            LayoutEditorManipulator?.Select(this);
        }

        private void NamedContentPropertiesViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(NamedContentPropertiesViewModel.SelectedContent))
            {
                ContentName = _namedContentPropertiesViewModel.SelectedContent;
            }
        }

        private void Remove()
        {
            LayoutEditorManipulator.RemoveLayoutElement(this);
        }
    }
}