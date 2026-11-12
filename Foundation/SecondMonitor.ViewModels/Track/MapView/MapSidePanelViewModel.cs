namespace SecondMonitor.ViewModels.Track.MapView
{
    using System.Windows.Input;
    using SecondMonitor.ViewModels;

    public class MapSidePanelViewModel : AbstractViewModel, IMapSidePanelViewModel
    {
        private ICommand _deleteMapCommand;
        private ICommand _rotateMapLeftCommand;
        private ICommand _rotateMapRightCommand;
        private ICommand _popupMapCommand;

        public ICommand PopUpCommand
        {
            get => _popupMapCommand;
            set => SetProperty(ref _popupMapCommand, value);
        }

        public ICommand DeleteMapCommand
        {
            get => _deleteMapCommand;
            set => SetProperty(ref _deleteMapCommand, value);
        }

        public ICommand RotateMapLeftCommand
        {
            get => _rotateMapLeftCommand;
            set => SetProperty(ref _rotateMapLeftCommand, value);
        }

        public ICommand RotateMapRightCommand
        {
            get => _rotateMapRightCommand;
            set => SetProperty(ref _rotateMapRightCommand, value);
        }
    }
}