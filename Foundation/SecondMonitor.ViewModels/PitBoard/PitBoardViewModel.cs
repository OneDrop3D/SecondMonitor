namespace SecondMonitor.ViewModels.PitBoard
{
    using System.Collections.ObjectModel;
    using System.Linq;
    using ViewModels;

    public class PitBoardViewModel : AbstractViewModel
    {
        public PitBoardViewModel()
        {
            PitBoards = new ObservableCollection<IViewModel>();
        }

        public ObservableCollection<IViewModel> PitBoards { get; }

        public bool IsPitBoardVisible => PitBoards.Any();

        public void AddPitBoard(IViewModel viewModel)
        {
            PitBoards.Add(viewModel);
            NotifyPropertyChanged(nameof(IsPitBoardVisible));
        }

        public void RemovePitBoard(IViewModel viewModel)
        {
            PitBoards.Remove(viewModel);
            NotifyPropertyChanged(nameof(IsPitBoardVisible));
        }
    }
}