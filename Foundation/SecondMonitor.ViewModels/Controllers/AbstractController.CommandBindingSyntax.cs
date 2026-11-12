namespace SecondMonitor.ViewModels.Controllers
{
    using System;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Contracts.Reflect;
    using Factory;
    using Syntax;

    public partial class AbstractController
    {
        private class CommandBindingSyntax<TViewModel, TCommand> : ICommandBindingSyntax<TViewModel> where TViewModel : IViewModel
            where TCommand : ICommand
        {
            private readonly Expression<Func<TViewModel, TCommand>> _commandExpression;
            private readonly IViewModelBindings _parentBindings;
            private readonly string _propertyName;
            private Func<TViewModel, ICommandFactory, TCommand> _createCommand;
            private Func<TViewModel, IViewModelFactory, TCommand> _targetAction;

            internal CommandBindingSyntax(Expression<Func<TViewModel, TCommand>> commandExpression, IViewModelBindings parentBindings)
            {
                _commandExpression = commandExpression;
                _parentBindings = parentBindings;
                _propertyName = Reflect<TViewModel>.GetProperty(commandExpression).Name;
            }

            public IViewModelBindingSyntax<TViewModel> To(Action action)
            {
                SetAssignAction(((model, factory) => (TCommand)factory.Create(action)));
                return (IViewModelBindingSyntax<TViewModel>)_parentBindings;
            }

            public IViewModelBindingSyntax<TViewModel> To<TParam>(Action<TParam> action)
            {
                SetAssignAction(((model, factory) => (TCommand)factory.Create(action)));
                return (IViewModelBindingSyntax<TViewModel>)_parentBindings;
            }

            public IViewModelBindingSyntax<TViewModel> ToAsync(Func<Task> action)
            {
                SetAssignAction(((model, factory) => (TCommand)factory.Create(action)));
                return (IViewModelBindingSyntax<TViewModel>)_parentBindings;
            }

            public void ExecuteCommandBinding(IViewModel viewModel, IViewModelFactory viewModelFactory)
            {
                _targetAction((TViewModel)viewModel, viewModelFactory);
            }

            private static Action<TViewModel, TResult> Compile<TResult>(Expression<Func<TViewModel, TResult>> commandGetExpression)
            {
                ParameterExpression newValue = Expression.Parameter(commandGetExpression.Body.Type);
                Expression<Action<TViewModel, TResult>> propertySetExpression = Expression.Lambda<Action<TViewModel, TResult>>(Expression.Assign(commandGetExpression.Body, newValue), commandGetExpression.Parameters[0], newValue);
                return propertySetExpression.Compile();
            }

            private void SetAssignAction(Func<TViewModel, ICommandFactory, TCommand> createCommand)
            {
                _createCommand = createCommand;
                _targetAction = (viewModel, viewModelFactory) =>
                {
                    Action<TViewModel, TCommand> compiledSetAction = Compile(_commandExpression);
                    try
                    {
                        TCommand command = createCommand(viewModel, viewModelFactory.CommandFactory);

                        compiledSetAction(viewModel, command);
                        viewModel.NotifyPropertyChanged(_propertyName);

                        return command;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Could not evaluate command property of view model '{viewModel}' in expression '{_commandExpression}'", ex);
                    }
                };
            }
        }
    }
}