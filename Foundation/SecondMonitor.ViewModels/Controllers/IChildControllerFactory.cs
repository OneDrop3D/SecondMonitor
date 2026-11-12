namespace SecondMonitor.ViewModels.Controllers
{
    using System.Collections.Generic;

    using Ninject.Parameters;

    public interface IChildControllerFactory
    {
        T Create<T, TParent>(TParent parentInstance) where T : IChildController<TParent> where TParent : IController;

        T Create<T, TParent>(TParent parentInstance, params IParameter[] constructorArguments) where T : IChildController<TParent> where TParent : IController;

        List<T> CreateAll<T, TParent>(TParent parentInstance) where T : IChildController<TParent> where TParent : IController;
    }
}