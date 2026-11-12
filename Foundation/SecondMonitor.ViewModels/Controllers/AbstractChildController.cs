namespace SecondMonitor.ViewModels.Controllers
{
    using Factory;

    public abstract class AbstractChildController<TParent> : AbstractController, IChildController<TParent> where TParent : IController
    {
        protected AbstractChildController(IViewModelFactory viewModelFactory) : base(viewModelFactory)
        {
        }

        public TParent ParentController { get; set; }
    }
}