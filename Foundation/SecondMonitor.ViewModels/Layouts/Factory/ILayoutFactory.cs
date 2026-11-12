namespace SecondMonitor.ViewModels.Layouts.Factory
{
    using Settings.Model.Layout;

    public interface ILayoutFactory
    {
        IViewModel Create(GenericContentSetting rootElement, IViewModelByNameFactory viewModelByNameFactory);
    }
}