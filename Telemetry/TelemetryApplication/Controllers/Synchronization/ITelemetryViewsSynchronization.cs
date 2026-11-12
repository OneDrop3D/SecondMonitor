namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.Synchronization
{
    using System;
    using System.Collections.Generic;
    using DataModel.Telemetry;
    using Graphs;
    using TelemetryManagement.DTO;

    public interface ITelemetryViewsSynchronization
    {
        event EventHandler<BusyStateEventArgs> BusyStateChanged;
        event EventHandler<EventArgs> LapLoadingFinished;
        event EventHandler<TelemetrySessionArgs> NewSessionLoaded;
        event EventHandler<TelemetrySessionArgs> SessionAdded;
        event EventHandler<LapsSummaryArgs> LapAddedToSession;
        event EventHandler<LapsSummaryArgs> LapRemovedFromSession;
        event EventHandler<LapsTelemetryArgs> LapLoaded;
        event EventHandler<LapsSummaryArgs> LapUnloaded;
        event EventHandler<TelemetrySnapshotArgs> SyncTelemetryView;
        event EventHandler<LapSummaryArgs> ReferenceLapSelected;
        event EventHandler<SessionModifiedArgs> SessionModified;

        void NotifyNewSessionLoaded(SessionInfoDto sessionInfoDto);
        void NotifySessionAdded(SessionInfoDto sessionInfoDto);
        void NotifyLapAddedToSession(IReadOnlyCollection<LapSummaryDto> lapSummariesDto);
        void NotifyLapLoaded(IReadOnlyCollection<LapTelemetryDto> lapTelemetriesDto);
        void NotifyLapUnloaded(IReadOnlyCollection<LapSummaryDto> lapSummariesDto);

        void NotifyLapAddedToSession(LapSummaryDto lapSummaryDto);
        void NotifyLapRemoveFromSession(LapSummaryDto lapSummaryDto);
        void NotifyLapLoaded(LapTelemetryDto lapTelemetriesDto);
        void NotifyLapUnloaded(LapSummaryDto lapSummaryDto);

        void NotifySynchronizeToSnapshot(TimedTelemetrySnapshot telemetrySnapshot, LapSummaryDto lapSummary);
        void NotifyReferenceLapSelected(LapSummaryDto referenceLap);
        void NotifyBusyStateChanged(bool isBusy);
        void NotifyLapLoadingFinished();
        void NotifySessionModified(SessionInfoDto sessionInfoDto);
    }
}