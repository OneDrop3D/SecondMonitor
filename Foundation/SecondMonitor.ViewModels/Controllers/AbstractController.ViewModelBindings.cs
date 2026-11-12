namespace SecondMonitor.ViewModels.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Windows.Input;
    using Contracts.Commands;
    using Factory;
    using Syntax;

    public partial class AbstractController
    {
        private sealed class ViewModelBindings<TViewModel> : IViewModelBindings, IViewModelBindingSyntax<TViewModel> where TViewModel : IViewModel
        {
            private readonly List<ICommandBindingSyntax<TViewModel>> _commandBindings;
            internal ViewModelBindings()
            {
                _commandBindings = new List<ICommandBindingSyntax<TViewModel>>();
                ViewModelType = typeof(TViewModel);
            }

            public Type ViewModelType { get; }

            public ICommandBindingSyntax<TViewModel> Command(Expression<Func<TViewModel, RelayCommand>> commandExpression)
            {
                ICommandBindingSyntax<TViewModel> newBinding = new CommandBindingSyntax<TViewModel, RelayCommand>(commandExpression, this);
                _commandBindings.Add(newBinding);
                return newBinding;
            }

            public ICommandBindingSyntax<TViewModel> Command(Expression<Func<TViewModel, AsyncCommand>> commandExpression)
            {
                ICommandBindingSyntax<TViewModel> newBinding = new CommandBindingSyntax<TViewModel, AsyncCommand>(commandExpression, this);
                _commandBindings.Add(newBinding);
                return newBinding;
            }

            public ICommandBindingSyntax<TViewModel> Command(Expression<Func<TViewModel, ICommand>> commandExpression)
            {
                ICommandBindingSyntax<TViewModel> newBinding = new CommandBindingSyntax<TViewModel, ICommand>(commandExpression, this);
                _commandBindings.Add(newBinding);
                return newBinding;
            }

            public void BindCommands<T>(T viewModel, IViewModelFactory viewModelFactory) where T : IViewModel
            {
                _commandBindings.ForEach(x => x.ExecuteCommandBinding(viewModel, viewModelFactory));
            }
        }
    }
}