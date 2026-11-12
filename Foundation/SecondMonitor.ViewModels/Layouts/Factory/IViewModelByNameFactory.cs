namespace SecondMonitor.ViewModels.Layouts.Factory
{
    public interface IViewModelByNameFactory
    {
        IViewModel Create(string viewModelName);
    }
}