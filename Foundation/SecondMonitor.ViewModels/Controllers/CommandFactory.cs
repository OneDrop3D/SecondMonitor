namespace SecondMonitor.ViewModels.Controllers
{
    using System;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Contracts.Commands;

    public class CommandFactory : ICommandFactory
    {
        public ICommand Create(Action operation)
        {
            return new RelayCommand(operation);
        }

        public ICommand Create<TParam>(Action<TParam> operation)
        {
            return new RelayCommand<TParam>(operation);
        }

        public ICommand Create(Func<Task> operation)
        {
            return new AsyncCommand(operation);
        }
    }
}