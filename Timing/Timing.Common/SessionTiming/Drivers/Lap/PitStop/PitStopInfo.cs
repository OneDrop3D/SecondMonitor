namespace SecondMonitor.Timing.Common.SessionTiming.Drivers.Lap.PitStop
{
    using System;

    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.Telemetry;
    using Drivers;
    using Lap;

    using SecondMonitor.DataModel.Extensions;

    public class PitStopInfo
    {
        private readonly TelemetrySnapshot _pitStopStartTelemetrySnapshot;
        private TelemetrySnapshot _pitStopEndTelemetrySnapshot;

        public PitStopInfo(SimulatorDataSet set, DriverTiming driver, ILapInfo entryLap)
        {
            Driver = driver;
            EntryLap = entryLap;
            ExitLap = new StaticLapInfo(driver);
            Phase = PitPhase.Entry;
            PitEntry = set.SessionInfo.SessionTime;
            PitStopDuration = TimeSpan.Zero;
            PitExit = PitEntry;
            PitStopStart = PitEntry;
            PitStopEnd = PitEntry;
            FuelTaken = Volume.FromLiters(0);
            AfterStopFuel = Volume.FromLiters(0);
            VirtualEnergyTaken = Percentage.FromPercentage(0);
            AfterStopVirtualEnergy = Percentage.FromPercentage(0);
            PitStopStallDuration = TimeSpan.Zero;
            NewFrontCompound = string.Empty;
            NewRearCompound = string.Empty;
            IsPitWindowStop = set.SessionInfo.PitWindow.PitWindowState == PitWindowState.InPitWindow;
            if (set.SessionInfo.SessionType == SessionType.Race)
            {
                _pitStopStartTelemetrySnapshot = new TelemetrySnapshot(set);
            }
        }

        public PitPhase Phase { get; private set; }

        public bool Completed => Phase == PitPhase.Completed;

        public bool WasDriveThrough { get; private set; }

        public DriverTiming Driver { get; }

        public ILapInfo EntryLap { get; }

        public ILapInfo ExitLap { get; private set; }

        public TimeSpan PitEntry { get; }

        public TimeSpan PitExit { get; private set; }

        public TimeSpan PitStopStart { get; private set; }

        public TimeSpan PitStopEnd { get; private set; }

        public TimeSpan PitStopDuration { get; private set; }

        public TimeSpan PitStopStallDuration { get; private set; }

        public string NewFrontCompound { get; private set; }

        public string NewRearCompound { get; private set; }

        public Volume FuelTaken { get; private set; }
        
        public Volume AfterStopFuel { get; private set; }

        public bool IsFrontTyresChanged { get; private set; }

        public bool IsRearTyresChanged { get; private set; }

        public bool IsPitWindowStop { get; }

        public bool WasUnderFCY { get; private set; }

        public TimeSpan RaceTimeLost { get; set; } = TimeSpan.Zero;

        public TimeSpan PitStopLaneDuration => PitStopDuration - PitStopStallDuration;

        public Percentage VirtualEnergyTaken { get; private set; }
        
        public Percentage AfterStopVirtualEnergy { get; private set; }

        public string PitInfoFormatted
        {
            get
            {
                switch (Phase)
                {
                    case PitPhase.Entry:
                        return "-->" + PitStopDuration.FormatTimeSpanOnlySecondNoMiliseconds(false);
                    case PitPhase.InPits:
                        return "---" + PitStopDuration.FormatTimeSpanOnlySecondNoMiliseconds(false) + "---";
                    case PitPhase.Exit:
                        return PitStopDuration.FormatTimeSpanOnlySecondNoMiliseconds(false) + " -->";
                    case PitPhase.Completed:
                        return PitStopDuration.FormatTimeSpanOnlySecondNoMiliseconds(false);
                    default:
                        return string.Empty;
                }
            }
        }

        public bool IsValid()
        {
            return EntryLap != null && ExitLap != null && PitStopDuration.TotalSeconds > 5;
        }

        public void Tick(SimulatorDataSet set)
        {
            if (Phase == PitPhase.Completed)
            {
                return;
            }

            if (set.SessionInfo.ActiveFlags.HasFlag(FlagKind.SafetyCar) || set.SessionInfo.ActiveFlags.HasFlag(FlagKind.FullCourseYellow) || set.SessionInfo.ActiveFlags.HasFlag(FlagKind.VirtualSafetyCar))
            {
                WasUnderFCY = true;
            }

            if (Phase == PitPhase.Entry && Driver.DriverInfo.Speed.InKph < 10)
            {
                Phase = PitPhase.InPits;
                PitStopStart = set.SessionInfo.SessionTime;
            }

            if (Phase == PitPhase.InPits && Driver.DriverInfo.Speed.InKph > 20 && (set.SessionInfo.SessionTime - PitStopStart).TotalSeconds > 2)
            {
                if (set.SessionInfo.SessionType == SessionType.Race)
                {
                    _pitStopEndTelemetrySnapshot = new TelemetrySnapshot(set);
                }

                CalculatePitStopProperties();
                Phase = PitPhase.Exit;
                PitStopEnd = set.SessionInfo.SessionTime;
                PitStopStallDuration = PitStopEnd - PitStopStart;
            }

            if (!Driver.DriverInfo.InPits)
            {
                ExitLap = Driver.CurrentLap;
                WasDriveThrough = Phase == PitPhase.Entry;
                Phase = PitPhase.Completed;
                PitExit = set.SessionInfo.SessionTime;
                PitStopDuration = PitExit.Subtract(PitEntry);
            }

            if (Phase == PitPhase.Entry)
            {
                PitStopStart = set.SessionInfo.SessionTime;
                PitStopEnd = set.SessionInfo.SessionTime;
            }

            if (Phase == PitPhase.InPits)
            {
                PitStopEnd = set.SessionInfo.SessionTime;
                PitStopStallDuration = PitStopEnd - PitStopStart;
            }

            if (Phase != PitPhase.Completed)
            {
                PitExit = set.SessionInfo.SessionTime;
                PitStopDuration = PitExit.Subtract(PitEntry);
            }
        }

        private void CalculatePitStopProperties()
        {
            if (_pitStopStartTelemetrySnapshot == null || _pitStopEndTelemetrySnapshot == null)
            {
                return;
            }

            if (_pitStopEndTelemetrySnapshot.PlayerData.CarInfo.FuelSystemInfo.FuelRemaining > _pitStopStartTelemetrySnapshot.PlayerData.CarInfo.FuelSystemInfo.FuelRemaining)
            {
                AfterStopFuel = _pitStopEndTelemetrySnapshot.PlayerData.CarInfo.FuelSystemInfo.FuelRemaining;
                FuelTaken = Volume.FromLiters(_pitStopEndTelemetrySnapshot.PlayerData.CarInfo.FuelSystemInfo.FuelRemaining.InLiters - _pitStopStartTelemetrySnapshot.PlayerData.CarInfo.FuelSystemInfo.FuelRemaining.InLiters);
            }

            if (_pitStopEndTelemetrySnapshot.PlayerData.CarInfo.FuelSystemInfo.HasVirtualTank && _pitStopEndTelemetrySnapshot.PlayerData.CarInfo.FuelSystemInfo.VirtualTank > _pitStopStartTelemetrySnapshot.PlayerData.CarInfo.FuelSystemInfo.VirtualTank)
            {
                AfterStopVirtualEnergy = _pitStopEndTelemetrySnapshot.PlayerData.CarInfo.FuelSystemInfo.VirtualTank;
                VirtualEnergyTaken = Percentage.FromPercentage(_pitStopEndTelemetrySnapshot.PlayerData.CarInfo.FuelSystemInfo.VirtualTank.InPercentage - _pitStopStartTelemetrySnapshot.PlayerData.CarInfo.FuelSystemInfo.VirtualTank.InPercentage);
            }

            IsFrontTyresChanged = _pitStopEndTelemetrySnapshot.PlayerData.CarInfo.WheelsInfo.FrontLeft.TyreWear.ActualWear < _pitStopStartTelemetrySnapshot.PlayerData.CarInfo.WheelsInfo.FrontLeft.TyreWear.ActualWear
                                  || _pitStopEndTelemetrySnapshot.PlayerData.CarInfo.WheelsInfo.FrontLeft.TyreVisualType != _pitStopStartTelemetrySnapshot.PlayerData.CarInfo.WheelsInfo.FrontLeft.TyreVisualType
                                  || _pitStopEndTelemetrySnapshot.PlayerData.CarInfo.WheelsInfo.FrontLeft.TyreCompoundWithSet != _pitStopStartTelemetrySnapshot.PlayerData.CarInfo.WheelsInfo.FrontLeft.TyreCompoundWithSet;
            IsRearTyresChanged = _pitStopEndTelemetrySnapshot.PlayerData.CarInfo.WheelsInfo.RearLeft.TyreWear.ActualWear < _pitStopStartTelemetrySnapshot.PlayerData.CarInfo.WheelsInfo.RearLeft.TyreWear.ActualWear
                                 || _pitStopEndTelemetrySnapshot.PlayerData.CarInfo.WheelsInfo.RearLeft.TyreVisualType != _pitStopStartTelemetrySnapshot.PlayerData.CarInfo.WheelsInfo.RearLeft.TyreVisualType
                                 || _pitStopEndTelemetrySnapshot.PlayerData.CarInfo.WheelsInfo.RearLeft.TyreCompoundWithSet != _pitStopStartTelemetrySnapshot.PlayerData.CarInfo.WheelsInfo.RearLeft.TyreCompoundWithSet;

            if (IsFrontTyresChanged)
            {
                NewFrontCompound = _pitStopEndTelemetrySnapshot.PlayerData.CarInfo.WheelsInfo.FrontLeft.TyreCompoundWithSet;
            }

            if (IsRearTyresChanged)
            {
                NewRearCompound = _pitStopEndTelemetrySnapshot.PlayerData.CarInfo.WheelsInfo.RearLeft.TyreCompoundWithSet;
            }
        }
    }
}
