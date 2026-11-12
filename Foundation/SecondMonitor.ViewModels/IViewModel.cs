namespace SecondMonitor.ViewModels
{
    using System.ComponentModel;

    public interface IViewModel<T> : IViewModel
    {
        T OriginalModel { get; }

        void FromModel(T model);
        T SaveToNewModel();
    }

    public interface IApplyToModel<in T>
    {
        void ApplyToModel(T model);
    }

    public interface IViewModel : INotifyPropertyChanged
    {
        void NotifyPropertyChanged(string nameOfProperty);
    }
}