namespace SecondMonitor.ViewModels.Factory
{
    using System.Collections.Generic;
    using System.Linq;
    using Contracts.NInject;
    using Controllers;
    using Ninject.Parameters;
    using Ninject.Syntax;

    public class OverriddenViewModelFactory : GenericFactory<IViewModelFactory>, IViewModelFactory
    {
        private readonly IViewModelFactory _decorate;

        public OverriddenViewModelFactory(IResolutionRoot resolutionRoot, IViewModelFactory decorate,  params IParameter[] constructorParameters) : base(resolutionRoot, constructorParameters)
        {
            _decorate = decorate;
        }

        public ICommandFactory CommandFactory => _decorate.CommandFactory;
        public new T Create<T>() where T : IViewModel
        {
            T newViewModel = base.Create<T>();
            if (_decorate is IDecoratedViewModelFactory decoratedViewModelFactory)
            {
                decoratedViewModelFactory.OnViewModelCreated(newViewModel);
            }

            return newViewModel;
        }

        public T CreateAndApply<T, TM>(TM model) where T : IViewModel<TM>
        {
            T newViewmodel = Create<T>();
            newViewmodel.FromModel(model);
            return newViewmodel;
        }

        public new IEnumerable<T> CreateAll<T>() where T : IViewModel
        {
            List<T> newViewModels = base.CreateAll<T>().ToList();
            if (_decorate is IDecoratedViewModelFactory decoratedViewModelFactory)
            {
                newViewModels.ForEach(decoratedViewModelFactory.OnViewModelCreated);
            }

            return newViewModels;
        }

        public override INamedConstructorParameter<IViewModelFactory> Set(string parameterName)
        {
            return new NamedConstructorParameter<IViewModelFactory, OverriddenViewModelFactory>(ResolutionRoot, parameterName, ConstructorParameters);
        }

        public override ITypedConstructorParameter<IViewModelFactory, T> Set<T>()
        {
            return new TypedConstructorParameter<IViewModelFactory, OverriddenViewModelFactory, T>(ResolutionRoot, ConstructorParameters);
        }
    }
}