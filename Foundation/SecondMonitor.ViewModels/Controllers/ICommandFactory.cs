namespace SecondMonitor.ViewModels.Controllers
{
    using System;
    using System.Threading.Tasks;
    using System.Windows.Input;

    public interface ICommandFactory
    {
        ICommand Create(Action operation);

        ICommand Create<TParam>(Action<TParam> operation);

        ICommand Create(Func<Task> operation);
    }
}