namespace SecondMonitor.Connectors.Remote.ViewModels
{
    using System.Windows.Input;
    using SecondMonitor.ViewModels;

    public class AddRemoteSourceViewModel : AbstractViewModel
    {
        private string _hostAddress;
        private bool _hostAddressIsValid;
        private int _port;
        private ICommand _connectCommand;

        public string HostAddress
        {
            get => _hostAddress;
            set => this.SetProperty(ref _hostAddress, value);
        }

        public bool HostAddressIsValid
        {
            get => _hostAddressIsValid;
            set => this.SetProperty(ref _hostAddressIsValid, value);
        }

        public int Port
        {
            get => _port;
            set => this.SetProperty(ref _port, value);
        }

        public ICommand ConnectCommand
        {
            get => _connectCommand;
            set => this.SetProperty(ref _connectCommand, value);
        }
    }
}