namespace SecondMonitor.ViewModels.Settings.ViewModel.Overrides
{
    using System;

    public class ActivationStateEventArgs : EventArgs
    {
        public ActivationStateEventArgs(bool isActive)
        {
            IsActive = isActive;
        }

        public bool IsActive { get; }
    }
}