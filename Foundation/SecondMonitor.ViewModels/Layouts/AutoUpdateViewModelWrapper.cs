namespace SecondMonitor.ViewModels.Layouts
{
    using System;
    using System.ComponentModel;

    public class AutoUpdateViewModelWrapper : AbstractViewModel
    {
        private readonly Func<IViewModel> _viewModelRetrieval;
        private IViewModel _wrappedViewModel;

        public AutoUpdateViewModelWrapper(INotifyPropertyChanged source, Func<IViewModel> viewModelRetrieval)
        {
            _viewModelRetrieval = viewModelRetrieval;

            source.PropertyChanged += SourceOnPropertyChanged;
            WrappedViewModel = viewModelRetrieval();
        }

        public IViewModel WrappedViewModel
        {
            get => _wrappedViewModel;
            set => SetProperty(ref _wrappedViewModel, value);
        }

        private void SourceOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            WrappedViewModel = _viewModelRetrieval();
        }
    }
}