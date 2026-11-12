namespace SecondMonitor.ViewModels.PitBoard.Controller
{
    using System;
    using Controllers;

    public interface IPitBoardController : IController
    {
        void RequestToShowPitBoard(IViewModel viewModel, int priority);

        void RequestToShowPitBoard(IViewModel viewModel, int priority, Func<bool> keepVisibleFunc);

        void RequestToShowPitBoard(IViewModel viewModel, int priority, TimeSpan displayTime);

        void HidePitBoard(IViewModel viewModel);
    }
}