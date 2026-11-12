namespace SecondMonitor.ViewModels.ListItem
{
    public class CheckedListItem<T> : AbstractViewModel
    {
        private bool _isChecked;
        private string _caption;
        private T _value;

        public CheckedListItem(bool isChecked, string caption, T value)
        {
            _isChecked = isChecked;
            _caption = caption;
            _value = value;
        }

        public bool IsChecked
        {
            get => _isChecked;
            set => SetProperty(ref _isChecked, value);
        }

        public string Caption
        {
            get => _caption;
            set => SetProperty(ref _caption, value);
        }

        public T Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
}