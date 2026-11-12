namespace SecondMonitor.ViewModels.Layouts
{
    using Editor;

    public interface ILayoutContainer
    {
        ILayoutConfigurationViewModel LayoutElement { get; }

        void SetLayout(ILayoutConfigurationViewModel layoutElement);
    }
}