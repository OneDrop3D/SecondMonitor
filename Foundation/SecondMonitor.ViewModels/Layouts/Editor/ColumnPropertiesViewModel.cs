namespace SecondMonitor.ViewModels.Layouts.Editor
{
    using System.Windows.Input;

    public class ColumnPropertiesViewModel : LengthDefinitionSettingViewModel
    {
        private bool _isMoveLeftEnabled;
        private bool _isMoveRightEnabled;

        public bool IsMoveLeftEnabled
        {
            get => _isMoveLeftEnabled;
            set => SetProperty(ref _isMoveLeftEnabled, value);
        }

        public bool IsMoveRightEnabled
        {
            get => _isMoveRightEnabled;
            set => SetProperty(ref _isMoveRightEnabled, value);
        }

        public ICommand MoveLeftCommand { get; set; }

        public ICommand MoveRightCommand { get; set; }

        public ICommand RemoveColumnCommand { get; set; }
    }
}