namespace SecondMonitor.ViewModels.Layouts.Factory
{
    using System.Collections.Generic;
    using Editor;

    public class CachedViewModelsFactory : IViewModelByNameFactory
    {
        private readonly Dictionary<string, IViewModel> _viewModelDictionary;

        public CachedViewModelsFactory()
        {
            _viewModelDictionary = new Dictionary<string, IViewModel>();
        }

        public void RegisterViewModel(string viewModelName, IViewModel viewModel)
        {
            _viewModelDictionary[viewModelName] = viewModel;
        }

        public IViewModel Create(string viewModelName)
        {
            return _viewModelDictionary.TryGetValue(viewModelName, out IViewModel viewModel) ? viewModel : new EmptyViewModel();
        }
    }
}