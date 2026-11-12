namespace SecondMonitor.Timing.Application.ReportCreation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common.SessionTiming.Drivers;
    using Common.SessionTiming.Drivers.Lap;
    using Common.SessionTiming.Drivers.Lap.PitStop;
    using DataModel.BasicProperties;
    using DataModel.Snapshot.Drivers;
    using DataModel.Summary;
    using NLog;

    using SecondMonitor.DataModel.Extensions;

    using SessionTiming.ViewModel;
    using ViewModels.FuelConsumption;

    public class SessionSummaryExporter
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly FuelConsumptionController _fuelConsumptionController;

        public SessionSummaryExporter(FuelConsumptionController fuelConsumptionController)
        {
            _fuelConsumptionController = fuelConsumptionController;
        }

        private static void AddDrivers(SessionSummary summary, SessionTimingController timing, bool overridePlayerAsFinished)
        {
            TimeSpan inactiveAllowance = TimeSpan.FromSeconds(30);
            summary.Drivers.AddRange(timing.Drivers.Where(x => x.Value.IsActive || x.Value.InactiveTime < inactiveAllowance).
                Select(d => ConvertToSummaryDriver(d.Value, timing.SessionType, timing.Player, overridePlayerAsFinished)));
            FillRaceGapsInfo(summary.Drivers, summary);
            FillRatingRelativeToPlayer(summary.Drivers, summary);
            AddPitStopInfo(summary, timing.Player);
        }

        private static void AddPitStopInfo(SessionSummary summary, DriverTiming player)
        {
            if (player == null || summary.SessionType != SessionType.Race)
            {
                return;
            }

            summary.PitStops = player.PitStops.Select(CreatePitStopSummary).ToList();
        }

        private static PitStopSummary CreatePitStopSummary(PitStopInfo pitStopInfo)
        {
            return new PitStopSummary()
            {
                EntryLapNumber = pitStopInfo.EntryLap?.LapNumber ?? 0,
                ExitLapNumber = pitStopInfo.ExitLap?.LapNumber ?? 0,
                NewRearCompound = pitStopInfo.NewRearCompound,
                NewFrontCompound = pitStopInfo.NewFrontCompound,
                FuelTaken = pitStopInfo.FuelTaken,
                AfterStopFuel = pitStopInfo.AfterStopFuel,
                VirtualEnergyTaken = pitStopInfo.VirtualEnergyTaken,
                AfterStopVirtualEnergy = pitStopInfo.AfterStopVirtualEnergy,
                IsFrontTyresChanged = pitStopInfo.IsFrontTyresChanged,
                IsRearTyresChanged = pitStopInfo.IsRearTyresChanged,
                OverallDuration = pitStopInfo.PitStopDuration,
                StallDuration = pitStopInfo.PitStopStallDuration,
                WasDriverThrough = pitStopInfo.WasDriveThrough,
                TotalRaceTimeLost = pitStopInfo.RaceTimeLost,
            };
        }

        private static Driver ConvertToSummaryDriver(DriverTiming driverTiming, SessionType sessionType, DriverTiming playerTiming, bool overridePlayerAsFinished)
        {
            Driver driverSummary = new Driver()
            {
                DriverLongName = driverTiming.DriverLongName,
                CarName = driverTiming.CarName,
                Finished = sessionType != SessionType.Race || driverTiming.IsActive,
                FinishingPosition = driverTiming.Position > 0 ? driverTiming.Position : int.MaxValue,
                FinishingPositionInClass = driverTiming.PositionInClass > 0 ? driverTiming.PositionInClass : int.MaxValue,
                InitialPosition = driverTiming.Laps.FirstOrDefault()?.LapTelemetryInfo?.LapStarSnapshot?.PlayerData.Position ?? 0,
                InitialPositionInClass = driverTiming.Laps.FirstOrDefault()?.LapTelemetryInfo?.LapStarSnapshot?.PlayerData.PositionInClass ?? 0,
                TopSpeed = driverTiming.TopSpeed,
                IsPlayer = driverTiming == playerTiming,
                FinishStatus = driverTiming.DriverInfo.FinishStatus,
                ClassName = driverTiming.CarClassName,
                ClassId = driverTiming.CarClassId,
                TotalDistance = driverTiming.TotalDistanceTraveled,
                GapToPlayerByTiming = driverTiming.GapToPlayerAbsolute,
                DriverId = driverTiming.DriverId,
                Rating = driverTiming.Rating,
                CarNumber = driverTiming.DriverInfo.CarRaceNumber,
                TeamName = driverTiming.DriverInfo.TeamName,
            };

            driverSummary.PitStopSummaries.AddRange(driverTiming.PitStops.Select(x => CreatePitStopSummary(x)));
            ILapInfo[] lapsWithEndPosition = driverTiming.Laps.Where(x => x.LapEndPosition > 0).ToArray();

            driverSummary.AveragePosition = lapsWithEndPosition.Length > 0 ? lapsWithEndPosition.Average(x => x.LapEndPosition) : int.MaxValue;
            if (driverSummary.IsPlayer && overridePlayerAsFinished)
            {
                driverSummary.FinishStatus = DriverFinishStatus.Finished;
            }

            int lapNumber = 1;
            bool allLaps = true;
            Logger.Info($"Exporting Session Summary driver {driverSummary.DriverId}, Total Laps : {driverTiming.Laps.Count}, Average Position {driverSummary.AveragePosition:F2}");
            driverSummary.Laps.AddRange(driverTiming.Laps.Where(l => l.LapTelemetryInfo != null && l.Completed && (allLaps || l.Valid)).Select(l => ConvertToSummaryLap(driverSummary, l, lapNumber++, sessionType)));
            driverSummary.TotalLaps = driverSummary.Laps.Count;
            Logger.Info($"Laps Exported: {driverSummary.TotalLaps}");
            FillGapInfo(driverSummary, driverTiming, playerTiming, sessionType);
            return driverSummary;
        }

        private static void FillGapInfo(Driver driverToFill, DriverTiming driverToFillTiming, DriverTiming playerTiming, SessionType sessionType)
        {
            if (playerTiming == null)
            {
                return;
            }

            if (sessionType == SessionType.Race)
            {
                driverToFill.LapsDifferenceToPlayer = -(int)((driverToFillTiming.TotalDistanceTraveled - playerTiming.TotalDistanceTraveled) / driverToFillTiming.Session.LastSet.SessionInfo.TrackInfo.LayoutLength.InMeters);
            }
            else if (playerTiming.BestLap != null && driverToFillTiming.BestLap != null)
            {
                driverToFill.GapToPlayerRelative = driverToFillTiming.BestLap.LapTime - playerTiming.BestLap.LapTime;
            }
        }

        private static void FillRaceGapsInfo(List<Driver> driversToFill, SessionSummary sessionSummary)
        {
            var player = driversToFill.FirstOrDefault(x => x.IsPlayer);
            if (player == null || sessionSummary.SessionType != SessionType.Race)
            {
                return;
            }

            driversToFill.Where(x => !x.IsPlayer && x.FinishStatus != DriverFinishStatus.Dnf).ForEach(x => FillRaceGapsInfo(x, player));
        }

        private static void FillRatingRelativeToPlayer(List<Driver> driversToFill, SessionSummary sessionSummary)
        {
            var player = driversToFill.FirstOrDefault(x => x.IsPlayer);
            if (player == null)
            {
                return;
            }

            driversToFill.ForEach(x => FillRatingRelativeToPlayer(x, player));
        }

        private static void FillRaceGapsInfo(Driver driverToFill, Driver player)
        {
            /*int lapsCount = Math.Min(driverToFill.Laps.Count, player.Laps.Count);
            double totalTimeDriver = driverToFill.Laps.Take(lapsCount).Sum(x => x.LapTime.TotalSeconds);
            double totalTimePlayer = player.Laps.Take(lapsCount).Sum(x => x.LapTime.TotalSeconds);
            driverToFill.GapToPlayerRelative = TimeSpan.FromSeconds(totalTimeDriver - totalTimePlayer);*/
            driverToFill.GapToPlayerRelative = driverToFill.GapToPlayerByTiming;
        }

        private static void FillRatingRelativeToPlayer(Driver driverToFill, Driver player)
        {
            if (driverToFill.Rating > 0 && player.Rating > 0)
            {
                driverToFill.RatingRelativeToPlayer = driverToFill.Rating - player.Rating;
            }
        }

        private static Lap ConvertToSummaryLap(Driver summaryDriver,  ILapInfo lapInfo, int lapNumber, SessionType sessionType)
        {
            Lap summaryLap = new(summaryDriver, lapInfo.Valid)
                                 {
                                     IsPitLap = lapInfo.IsPitLap,
                                     IsPitEntryLap = lapInfo.IsPitEntryLap,
                                     IsPitExitLap = lapInfo.IsPitExitLap,
                                     LapNumber = lapNumber,
                                     LapTime = lapInfo.LapTime,
                                     Sector1 = lapInfo.Sector1?.Duration ?? TimeSpan.Zero,
                                     Sector2 = lapInfo.Sector2?.Duration ?? TimeSpan.Zero,
                                     Sector3 = lapInfo.Sector3?.Duration ?? TimeSpan.Zero,
                                     LapEndSnapshot = lapInfo.LapTelemetryInfo.LapEndSnapshot,
                                     LapStartSnapshot = lapInfo.LapTelemetryInfo.LapStarSnapshot,
                                     SessionType = sessionType
                                 };
            return summaryLap;
        }

        public SessionSummary CreateSessionSummary(SessionTimingController timing, bool overridePlayerAsFinished)
        {
            SessionSummary summary = new SessionSummary();
            FillSessionInfo(summary, timing);
            AddDrivers(summary, timing, overridePlayerAsFinished);
            return summary;
        }

        private void FillSessionInfo(SessionSummary summary, SessionTimingController timing)
        {
            var fuelConsumptionToUse = _fuelConsumptionController.CreateCurrentSessionFuelConsumptionDto() ?? _fuelConsumptionController.LastFinishedConsumptionDto;
            summary.SessionType = timing.SessionType;
            summary.TrackInfo = timing.LastSet.SessionInfo.TrackInfo;
            summary.Simulator = timing.LastSet.Source;
            summary.SessionLength = TimeSpan.FromSeconds(timing.TotalSessionLength);
            summary.SessionLengthType = timing.LastSet.SessionInfo.SessionLengthType;
            summary.TotalNumberOfLaps = timing.LastSet.SessionInfo.TotalNumberOfLaps;
            summary.SessionRunDuration = timing.SessionTime;
            summary.WasGreen = timing.WasGreen;
            summary.IsMultiClass = timing.IsMultiClass;
            summary.FuelConsumptionInformation = fuelConsumptionToUse;
            summary.SessionGuid = timing.GlobalKey;
        }
    }
}