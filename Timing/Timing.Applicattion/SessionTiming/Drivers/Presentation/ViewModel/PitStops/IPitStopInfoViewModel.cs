namespace SecondMonitor.Timing.Application.SessionTiming.Drivers.Presentation.ViewModel.PitStops
{
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.Timing.Common.SessionTiming.Drivers;
    using SecondMonitor.ViewModels;

    public interface IPitStopInfoViewModel : IViewModel<DriverTiming>
    {
        bool IsPlayer { get; }
        bool HasValidInformation { get; }

        bool CanBeUsed(SimulatorDataSet dataSet);

        void UpdatePitInformation(DriverTiming driverTiming, SimulatorDataSet dataSet);
    }
}