namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using System.Windows.Forms;
    using System.Windows.Input;

    using SecondMonitor.Contracts.Commands;
    using SecondMonitor.ViewModels.Settings.Model;

    public class AutoDeleteSettingsViewModel : AbstractViewModel<AutoDeleteSettings>
    {
        private string _directory;
        private bool _isDeleteBySizeEnabled;
        private bool _isDeleteByDaysEnabled;
        private int _maxMegabytes;
        private int _maxDays;

        public AutoDeleteSettingsViewModel(string title)
        {
            Title = title;
            OpenSelectFolderCommand = new RelayCommand(SelectDirectory);
        }

        public string Title { get; }

        public ICommand OpenSelectFolderCommand { get; }

        public string Directory
        {
            get => _directory;
            set => SetProperty(ref _directory, value);
        }

        public bool IsDeleteBySizeEnabled
        {
            get => _isDeleteBySizeEnabled;
            set => SetProperty(ref _isDeleteBySizeEnabled, value);
        }

        public bool IsDeleteByDaysEnabled
        {
            get => _isDeleteByDaysEnabled;
            set => SetProperty(ref _isDeleteByDaysEnabled, value);
        }

        public int MaxMegabytes
        {
            get => _maxMegabytes;
            set => SetProperty(ref _maxMegabytes, value);
        }

        public int MaxDays
        {
            get => _maxDays;
            set => SetProperty(ref _maxDays, value);
        }

        protected override void ApplyModel(AutoDeleteSettings model)
        {
            IsDeleteByDaysEnabled = model.IsDeleteByDaysEnabled;
            IsDeleteBySizeEnabled = model.IsDeleteBySizeEnabled;
            MaxMegabytes = model.MaxMegabytes;
            MaxDays = model.MaxDays;
        }

        public override AutoDeleteSettings SaveToNewModel() => new(IsDeleteBySizeEnabled, IsDeleteByDaysEnabled, MaxMegabytes, MaxDays);

        private void SelectDirectory()
        {
            using (FolderBrowserDialog fbd = new())
            {
                fbd.ShowNewFolderButton = true;
                fbd.SelectedPath = Directory;
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    Directory = fbd.SelectedPath;
                }
            }
        }
    }
}
