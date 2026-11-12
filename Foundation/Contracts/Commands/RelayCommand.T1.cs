namespace SecondMonitor.Contracts.Commands
{
    using System;
    using System.Windows.Input;

    public class RelayCommand<TParam> : ICommand
    {
        private readonly Action<TParam> _relayAction;

        public RelayCommand(Action<TParam> action)
        {
            _relayAction = action;
        }

        // Interface implementation
#pragma warning disable CS0067
        public event EventHandler CanExecuteChanged;
#pragma warning disable CS0067

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            if (parameter is TParam concertedParam)
            {
                _relayAction(concertedParam);
            }
        }
    }
}