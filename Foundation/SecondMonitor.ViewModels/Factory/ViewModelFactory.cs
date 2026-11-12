namespace SecondMonitor.ViewModels.Factory
{
    using System.Collections.Generic;
    using Contracts.NInject;
    using Controllers;
    using Ninject;
    using Ninject.Parameters;
    using Ninject.Syntax;
    using ViewModels;

    public class ViewModelFactory : GenericFactory<IViewModelFactory>, IViewModelFactory
    {
        public ViewModelFactory(IResolutionRoot resolutionRoot, params IParameter[] constructorParameters) : base(resolutionRoot, constructorParameters)
        {
            CommandFactory = resolutionRoot.Get<ICommandFactory>();
        }

        public ViewModelFactory(IResolutionRoot resolutionRoot) : base(resolutionRoot)
        {
            CommandFactory = resolutionRoot.Get<ICommandFactory>();
        }

        public ICommandFactory CommandFactory { get; }

        public new T Create<T>() where T : IViewModel
        {
            return base.Create<T>();
        }

        public T CreateAndApply<T, TM>(TM model) where T : IViewModel<TM>
        {
            T viewModel = base.Create<T>();
            viewModel.FromModel(model);
            return viewModel;
        }

        public new IEnumerable<T> CreateAll<T>() where T : IViewModel
        {
            return base.CreateAll<T>();
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