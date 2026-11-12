namespace SecondMonitor.ViewModels.Controllers.Syntax
{
    using System;
    using System.Threading.Tasks;
    using Contracts.Fluent;
    using Factory;

    public interface ICommandBindingSyntax<TViewModel> : IFluentSyntax where TViewModel : IViewModel
    {
        IViewModelBindingSyntax<TViewModel> To(Action action);

        IViewModelBindingSyntax<TViewModel> To<TParam>(Action<TParam> action);

        IViewModelBindingSyntax<TViewModel> ToAsync(Func<Task> action);

        void ExecuteCommandBinding(IViewModel viewModel, IViewModelFactory viewModelFactory);
    }
}