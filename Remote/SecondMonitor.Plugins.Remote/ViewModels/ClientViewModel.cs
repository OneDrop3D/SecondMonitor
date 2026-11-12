namespace SecondMonitor.Plugins.Remote.ViewModels
{
    using System;

    using LiteNetLib;
    using SecondMonitor.ViewModels;

    public class ClientViewModel : AbstractViewModel<NetPeer>, IClientViewModel
    {
        private string _ipAddress;
        private int _port;

        public string IpAddress
        {
            get => _ipAddress;
            set => this.SetProperty(ref _ipAddress, value);
        }

        public int Port
        {
            get => _port;
            set => this.SetProperty(ref _port, value);
        }

        protected override void ApplyModel(NetPeer model)
        {
            IpAddress = model.Address.ToString();
            Port = model.Port;
        }

        public override NetPeer SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}