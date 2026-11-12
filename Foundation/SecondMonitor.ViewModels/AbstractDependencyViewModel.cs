namespace SecondMonitor.ViewModels
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows;

    public abstract class AbstractDependencyViewModel : DependencyObject, IViewModel
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void SetProperty<T>(DependencyProperty property, T value, [CallerMemberName] string propertyName = null)
        {
            T oldValue = (T)GetValue(property);
            if (oldValue != null && oldValue.Equals(value))
            {
                return;
            }

            SetValue(property, value);
        }

        public virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}