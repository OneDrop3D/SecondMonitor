namespace SecondMonitor.ViewModels.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Ninject;
    using Ninject.Parameters;
    using Ninject.Syntax;

    public class ChildControllerFactory : IChildControllerFactory
    {
        private readonly IResolutionRoot _resolutionRoot;

        public ChildControllerFactory(IResolutionRoot resolutionRoot)
        {
            _resolutionRoot = resolutionRoot;
        }

        public T Create<T, TParent>(TParent parentInstance) where T : IChildController<TParent>
            where TParent : IController
        {
            return Create<T, TParent>(parentInstance, Array.Empty<IParameter>());
        }

        public T Create<T, TParent>(TParent parentInstance, params IParameter[] constructorArguments) where T : IChildController<TParent> where TParent : IController
        {
            var childController = _resolutionRoot.Get<T>(constructorArguments);
            childController.ParentController = parentInstance;
            return childController;
        }

        public List<T> CreateAll<T, TParent>(TParent parentInstance) where T : IChildController<TParent> where TParent : IController
        {
            var childControllers = _resolutionRoot.GetAll<T>().ToList();
            childControllers.ForEach(x => x.ParentController = parentInstance);
            return childControllers;
        }
    }
}