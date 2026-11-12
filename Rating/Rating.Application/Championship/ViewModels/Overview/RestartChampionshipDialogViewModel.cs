namespace SecondMonitor.Rating.Application.Championship.ViewModels.Overview
{
    using System;
    using System.Windows;
    using System.Windows.Input;
    using Contracts.Commands;
    using SecondMonitor.ViewModels;

    public class RestartChampionshipDialogViewModel : AbstractViewModel, IDialogViewModel
    {
        private Window _window;

        public RestartChampionshipDialogViewModel()
        {
            OkCommand = new RelayCommand(OkPressed);
            CancelCommand = new RelayCommand(CancelPressed);
            ShuffleCommand = new RelayCommand(ShufflePressed);
        }

        public enum DialogResultKind
        {
            Cancel,
            Ok,
            Shuffle,
        }

        public ICommand OkCommand { get; }

        public ICommand CancelCommand { get; }

        public bool? DialogResult { get; set; }

        public ICommand ShuffleCommand { get; }

        public DialogResultKind ResetChampionshipDialogResult { get; private set; }

        public void RegisterWindow(Window window)
        {
            _window = window;
            _window.Closed += WindowOnClosed;
        }

        public void UnRegisterWindow()
        {
            _window.Closed -= WindowOnClosed;
        }

        private void WindowOnClosed(object sender, EventArgs e)
        {
            UnRegisterWindow();
        }

        private void CloseWindow()
        {
            _window?.Close();
        }

        private void CancelPressed()
        {
            CloseWindow();
        }

        private void OkPressed()
        {
            ResetChampionshipDialogResult = DialogResultKind.Ok;
            DialogResult = true;
            CloseWindow();
        }

        private void ShufflePressed()
        {
            ResetChampionshipDialogResult = DialogResultKind.Shuffle;
            CloseWindow();
        }
    }
}