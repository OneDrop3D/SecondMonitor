namespace SecondMonitor.ViewModels.Layouts.Editor
{
    using System.Windows.Input;

    public class RowPropertiesViewModel : LengthDefinitionSettingViewModel
    {
        private bool _isMoveUpEnabled;
        private bool _isMoveDownEnabled;

        public bool IsMoveUpEnabled
        {
            get => _isMoveUpEnabled;
            set => SetProperty(ref _isMoveUpEnabled, value);
        }

        public bool IsMoveDownEnabled
        {
            get => _isMoveDownEnabled;
            set => SetProperty(ref _isMoveDownEnabled, value);
        }

        public ICommand MoveUpCommand { get; set; }

        public ICommand MoveDownCommand { get; set; }

        public ICommand RemoveRowCommand { get; set; }
    }
}