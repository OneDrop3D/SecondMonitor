namespace SecondMonitor.ViewModels.Layouts.Editor
{
    using Settings.Model.Layout;

    public interface ILayoutConfigurationViewModelFactory
    {
        ILayoutEditorManipulator LayoutEditorManipulator { get; set; }

        ILayoutConfigurationViewModel Create(GenericContentSetting rootElement);

        ILayoutConfigurationViewModel CreateEmpty();
        
        GenericContentSetting ToContentSettings(ILayoutConfigurationViewModel layoutViewModel);
    }
}