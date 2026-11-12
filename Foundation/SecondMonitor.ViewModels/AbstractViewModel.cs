namespace SecondMonitor.ViewModels
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public abstract class AbstractViewModel : IViewModel
    {
        private bool _isModified;
        public event PropertyChangedEventHandler PropertyChanged;

        public virtual bool IsModified
        {
            get => _isModified;
            set => SetProperty(ref _isModified, value);
        }

        public virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void SetProperty<T>(ref T backingField, T value, [CallerMemberName] string propertyName = null) => SetProperty(ref backingField, value, true, propertyName);

        protected void SetProperty<T>(ref T backingField, T value, bool setIsModified, [CallerMemberName] string propertyName = null)
        {
            if (backingField != null && backingField.Equals(value))
            {
                return;
            }

            backingField = value;
            if (setIsModified && propertyName != nameof(IsModified))
            {
                IsModified = true;
            }

            NotifyPropertyChanged(propertyName);
        }

        protected void SetProperty<T>(ref T backingField, T value, Action<T, T> onChanged, [CallerMemberName] string propertyName = null)
        {
            T oldValue = backingField;
            if (backingField != null && backingField.Equals(value))
            {
                return;
            }

            backingField = value;
            onChanged(oldValue, backingField);
            if (propertyName != nameof(IsModified))
            {
                IsModified = true;
            }

            NotifyPropertyChanged(propertyName);
        }
    }
}