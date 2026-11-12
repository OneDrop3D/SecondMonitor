namespace SecondMonitor.ViewModels.Factory
{
    using System.Collections.Generic;
    using Contracts.NInject;
    using Controllers;
    using ViewModels;

    public interface IViewModelFactory
    {
        ICommandFactory CommandFactory { get; }

        T Create<T>() where T : IViewModel;

        T CreateAndApply<T, TM>(TM model) where T : IViewModel<TM>;

        IEnumerable<T> CreateAll<T>() where T : IViewModel;

        INamedConstructorParameter<IViewModelFactory> Set(string parameterName);

        ITypedConstructorParameter<IViewModelFactory, T> Set<T>();
    }
}