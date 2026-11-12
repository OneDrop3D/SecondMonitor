namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.SnapshotSection
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using DataModel.BasicProperties;
    using Replay;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.CarStatus;
    using TelemetryManagement.DTO;

    public interface ISnapshotSectionViewModel : IViewModel, INotifyPropertyChanged
    {
        ReadOnlyCollection<LapSummaryDto> AvailableLaps { get; }
        LapSummaryDto SelectedLap { get; set; }
        IReplayViewModel ReplayViewModel { get; }
        IPedalSectionViewModel PedalSectionViewModel { get; }
        CarWheelsViewModel CarWheelsViewModel { get; }
        TemperatureUnits TemperatureUnits { get; set; }
        PressureUnits PressureUnits { get; set; }

        void AddAvailableLaps(IEnumerable<LapSummaryDto> lapSummaryDtos);
        void RemoveAvailableLaps(IEnumerable<LapSummaryDto> lapSummaryDtos);
        void ClearAvailableLaps();
    }
}