namespace SecondMonitor.ViewModels
{
    public abstract class AbstractViewModel<T> : AbstractViewModel, IViewModel<T>
    {
        public T OriginalModel { get; private set; }

        protected abstract void ApplyModel(T model);

        public void FromModel(T model)
        {
            OriginalModel = model;
            ApplyModel(model);
            IsModified = false;
        }

        public abstract T SaveToNewModel();
    }
}