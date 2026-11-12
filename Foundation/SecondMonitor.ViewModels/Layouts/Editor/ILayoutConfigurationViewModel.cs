namespace SecondMonitor.ViewModels.Layouts.Editor
{
    public interface ILayoutConfigurationViewModel : IViewModel
    {
        ILayoutConfigurationViewModelFactory LayoutConfigurationViewModelFactory { get; set; }

        ILayoutEditorManipulator LayoutEditorManipulator { get; set; }

        bool IsSelected { get; set; }

        IViewModel PropertiesViewModel { get; }
    }
}