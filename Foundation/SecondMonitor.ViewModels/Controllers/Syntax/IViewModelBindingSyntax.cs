namespace SecondMonitor.ViewModels.Controllers.Syntax
{
    using System;
    using System.Linq.Expressions;
    using System.Windows.Input;
    using Contracts.Commands;
    using Ninject.Syntax;

    public interface IViewModelBindingSyntax<TViewModel> : IFluentSyntax where TViewModel : IViewModel
    {
        ICommandBindingSyntax<TViewModel> Command(Expression<Func<TViewModel, ICommand>> commandExpression);

        ICommandBindingSyntax<TViewModel> Command(Expression<Func<TViewModel, RelayCommand>> commandExpression);
        
        ICommandBindingSyntax<TViewModel> Command(Expression<Func<TViewModel, AsyncCommand>> commandExpression);
    }
}