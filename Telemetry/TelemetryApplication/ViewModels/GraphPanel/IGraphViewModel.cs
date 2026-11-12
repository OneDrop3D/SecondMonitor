namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel
{
    using System;
    using System.Collections.Generic;
    using Controllers.Synchronization;
    using Controllers.Synchronization.Graphs;
    using DataModel.BasicProperties.Units;
    using DataModel.Telemetry;
    using OxyPlot;
    using SecondMonitor.ViewModels;
    using Settings.DTO;
    using TelemetryManagement.DTO;
    using TelemetryManagement.Settings;

    public interface IGraphViewModel : IViewModel, IDisposable
    {
        string Title { get; }
        bool HasValidData { get; set; }
        PlotModel PlotModel { get; }
        XAxisKind XAxisKind { get; set; }
        ILapColorSynchronization LapColorSynchronization { get; set; }
        IGraphViewSynchronization GraphViewSynchronization { get; set; }

        bool IsCarSettingsDependant { get; }

        UnitsCollection UnitsCollection { get; set; }

        void AddLapTelemetry(LapTelemetryDto lapTelemetryDto, CarPropertiesDto carPropertiesDto);
        void AddLapTelemetries(IEnumerable<(LapTelemetryDto LapTelemetryDto, CarPropertiesDto CarPropertiesDto)> lapSummaryDtos);
        void RemoveLapTelemetry(LapSummaryDto lapSummaryDto);
        void UpdateXSelection(LapSummaryDto lapSummaryDto, TimedTelemetrySnapshot timedTelemetrySnapshot);
        void RemoveAllLapsTelemetry();
    }
}