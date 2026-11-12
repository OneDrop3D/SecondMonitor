namespace SecondMonitor.ViewModels.Controllers
{
    using System.Threading.Tasks;
    using Factory;
    using Syntax;

    public abstract partial class AbstractController : IController
    {
        private readonly BindingViewModelFactoryDecorator _viewModelFactory;
        protected AbstractController(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = new BindingViewModelFactoryDecorator(viewModelFactory);
        }

        protected IViewModelFactory ViewModelFactory => _viewModelFactory;

        public abstract Task StartControllerAsync();
        public abstract Task StopControllerAsync();

        public IViewModelBindingSyntax<TViewModel> Bind<TViewModel>() where TViewModel : IViewModel => _viewModelFactory.Bind<TViewModel>();
    }
}