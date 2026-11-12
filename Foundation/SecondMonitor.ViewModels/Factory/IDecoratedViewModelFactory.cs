namespace SecondMonitor.ViewModels.Factory
{
    public interface IDecoratedViewModelFactory
    {
        void OnViewModelCreated<T>(T newViewModel) where T : IViewModel;
    }
}