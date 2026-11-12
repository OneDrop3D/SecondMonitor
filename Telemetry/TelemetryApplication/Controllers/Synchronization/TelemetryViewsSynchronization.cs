namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.Synchronization
{
    using System;
    using System.Collections.Generic;
    using DataModel.Telemetry;
    using Graphs;
    using TelemetryManagement.DTO;

    public class TelemetryViewsSynchronization : ITelemetryViewsSynchronization
    {
        public event EventHandler<BusyStateEventArgs> BusyStateChanged;
        public event EventHandler<EventArgs> LapLoadingFinished;
        public event EventHandler<TelemetrySessionArgs> NewSessionLoaded;
        public event EventHandler<TelemetrySessionArgs> SessionAdded;
        public event EventHandler<LapsSummaryArgs> LapAddedToSession;
        public event EventHandler<LapsSummaryArgs> LapRemovedFromSession;
        public event EventHandler<LapsTelemetryArgs> LapLoaded;
        public event EventHandler<LapsSummaryArgs> LapUnloaded;
        public event EventHandler<TelemetrySnapshotArgs> SyncTelemetryView;
        public event EventHandler<LapSummaryArgs> ReferenceLapSelected;
        public event EventHandler<SessionModifiedArgs> SessionModified;

        public void NotifyNewSessionLoaded(SessionInfoDto sessionInfoDto)
        {
            TelemetrySessionArgs args = new TelemetrySessionArgs(sessionInfoDto);
            NewSessionLoaded?.Invoke(this, args);
        }

        public void NotifySessionAdded(SessionInfoDto sessionInfoDto)
        {
            TelemetrySessionArgs args = new TelemetrySessionArgs(sessionInfoDto);
            SessionAdded?.Invoke(this, args);
        }

        public void NotifyLapAddedToSession(IReadOnlyCollection<LapSummaryDto> lapSummariesDto)
        {
            var args = new LapsSummaryArgs(lapSummariesDto);
            LapAddedToSession?.Invoke(this, args);
        }

        public void NotifyLapLoaded(IReadOnlyCollection<LapTelemetryDto> lapTelemetriesDto)
        {
            var lapTelemetryArgs = new LapsTelemetryArgs(lapTelemetriesDto);
            LapLoaded?.Invoke(this, lapTelemetryArgs);
        }

        public void NotifyLapUnloaded(IReadOnlyCollection<LapSummaryDto> lapSummariesDto)
        {
            var args = new LapsSummaryArgs(lapSummariesDto);
            LapUnloaded?.Invoke(this, args);
        }

        public void NotifyLapAddedToSession(LapSummaryDto lapSummaryDto)
        {
            NotifyLapAddedToSession(new List<LapSummaryDto>() { lapSummaryDto });
        }

        public void NotifyLapRemoveFromSession(LapSummaryDto lapSummaryDto)
        {
            LapRemovedFromSession?.Invoke(this, new LapsSummaryArgs(new[] { lapSummaryDto }));
        }

        public void NotifyLapLoaded(LapTelemetryDto lapTelemetriesDto)
        {
            NotifyLapLoaded(new List<LapTelemetryDto>() { lapTelemetriesDto });
        }

        public void NotifyLapUnloaded(LapSummaryDto lapSummaryDto)
        {
            NotifyLapUnloaded(new List<LapSummaryDto>() { lapSummaryDto });
        }

        public void NotifySynchronizeToSnapshot(TimedTelemetrySnapshot telemetrySnapshot, LapSummaryDto lapSummary)
        {
            TelemetrySnapshotArgs args = new TelemetrySnapshotArgs(telemetrySnapshot, lapSummary);
            SyncTelemetryView?.Invoke(this, args);
        }

        public void NotifyReferenceLapSelected(LapSummaryDto referenceLap)
        {
            LapSummaryArgs args = new LapSummaryArgs(referenceLap);
            ReferenceLapSelected?.Invoke(this, args);
        }

        public void NotifyBusyStateChanged(bool isBusy)
        {
            BusyStateChanged?.Invoke(this, new BusyStateEventArgs(isBusy));
        }

        public void NotifyLapLoadingFinished()
        {
            LapLoadingFinished?.Invoke(this, new EventArgs());
        }

        public void NotifySessionModified(SessionInfoDto sessionInfoDto)
        {
            SessionModified?.Invoke(this, new SessionModifiedArgs(sessionInfoDto));
        }
    }
}