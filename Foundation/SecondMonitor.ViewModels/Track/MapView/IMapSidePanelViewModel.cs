namespace SecondMonitor.ViewModels.Track.MapView
{
    using System.Windows.Input;

    public interface IMapSidePanelViewModel : IViewModel
    {
        ICommand PopUpCommand { get; set; }
        ICommand DeleteMapCommand { get; set; }
        ICommand RotateMapLeftCommand { get; set; }
        ICommand RotateMapRightCommand { get; set; }
    }
}