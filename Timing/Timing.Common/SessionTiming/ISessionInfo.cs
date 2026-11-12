namespace SecondMonitor.Timing.Common.SessionTiming
{
    using System;
    using System.Collections.Generic;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using Drivers;
    using Drivers.Lap;
    using ViewModels.Settings.ViewModel;

    public interface ISessionInfo
    {
        Guid SessionGlobalKey { get; }
        int PaceLaps { get; }
        bool RetrieveAlsoInvalidLaps { get; }
        SimulatorDataSet LastSet { get; }
        DriverTiming Player { get; }
        DriverTiming Leader { get; }
        SessionType SessionType { get; }
        bool DisplayBindTimeRelative { get; }
        DisplaySettingsViewModel DisplaySettingsViewModel { get; }

        IReadOnlyCollection<DriverTiming> DriversByPosition { get; }

        bool DisplayGapToPlayerRelative { get; set; }

        double SessionCompletedPercentage { get; }
        BestTimesSetViewModel SessionBestTimesViewModel { get; }

        ILapInfo GetBestLap();

        ILapInfo GetBestLap(string classId);

        ILapInfo GetBestLapExcludePlayer(string classId);

        BestTimesSetViewModel GetBestTimesForClass(string carClassIdm);
    }
}