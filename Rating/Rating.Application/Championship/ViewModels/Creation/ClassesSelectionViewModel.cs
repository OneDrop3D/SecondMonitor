namespace SecondMonitor.Rating.Application.Championship.ViewModels.Creation
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;
    using Contracts.Commands;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.ListItem;

    public class ClassesSelectionViewModel : AbstractViewModel<IEnumerable<string>>
    {
        private List<CheckedListItem<string>> _classes;

        public ClassesSelectionViewModel()
        {
            SelectAllCommand = new RelayCommand(SelectAll);
            UnselectAllCommand = new RelayCommand(UnselectAll);
        }

        public List<CheckedListItem<string>> Classes
        {
            get => _classes;
            set => SetProperty(ref _classes, value);
        }

        public ICommand SelectAllCommand
        {
            get;
        }

        public ICommand UnselectAllCommand
        {
            get;
        }

        protected override void ApplyModel(IEnumerable<string> model)
        {
            Classes = model.OrderBy(x => x).Select(x => new CheckedListItem<string>(true, x, x)).ToList();
        }

        public override IEnumerable<string> SaveToNewModel()
        {
            return _classes.Where(x => x.IsChecked).Select(x => x.Value);
        }

        private void UnselectAll()
        {
            _classes.ForEach(x => x.IsChecked = false);
        }

        private void SelectAll()
        {
            _classes.ForEach(x => x.IsChecked = true);
        }
    }
}