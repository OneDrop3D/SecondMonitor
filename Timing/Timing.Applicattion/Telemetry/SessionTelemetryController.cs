namespace SecondMonitor.Timing.Application.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Common.SessionTiming;
    using Common.SessionTiming.Drivers.Lap;
    using Contracts.TaskQueue;
    using DataModel.BasicProperties;
    using DataModel.Snapshot.Drivers;
    using DataModel.Telemetry;
    using NLog;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.Telemetry.TelemetryManagement.DTO;
    using SecondMonitor.Telemetry.TelemetryManagement.Repository;
    using ViewModels.Settings;

    public class SessionTelemetryController : ISessionTelemetryController
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<string, string> _personalBestMap;
        private readonly ITelemetryRepository _telemetryRepository;
        private readonly ILapEventProvider _lapEventProvider;
        private readonly ISettingsProvider _settingsProvider;
        private readonly TaskQueue _taskQueue;
        private SessionInfoDto _sessionInfoDto;
        private ILapInfo _overallBest;
        private string _overallBestFileName;

        public SessionTelemetryController(string trackName, SessionType sessionType, 
            ITelemetryRepository telemetryRepository, ILapEventProvider lapEventProvider, ISettingsProvider settingsProvider)
        {
            _personalBestMap = new Dictionary<string, string>();
            _telemetryRepository = telemetryRepository;
            _lapEventProvider = lapEventProvider;
            _settingsProvider = settingsProvider;
            SessionIdentifier = $"{DateTime.Now:yy-MM-dd-HH-mm}-{trackName}-{sessionType}-{Guid.NewGuid()}";
            _taskQueue = new TaskQueue(true);
        }

        public string SessionIdentifier { get; }

        public void TrySaveLapTelemetry(ILapInfo lapInfo)
        {
            _taskQueue.EnqueueTask(() => TrySaveLapTelemetryInternal(lapInfo));
        }

        private async Task<bool> TrySaveLapTelemetryInternal(ILapInfo lapInfo)
        {
            if (lapInfo?.LapTelemetryInfo == null)
            {
                return false;
            }

            if (_sessionInfoDto == null)
            {
                _sessionInfoDto = CreateSessionInfo(lapInfo);
            }

            Logger.Info($"Saving Telemetry for Lap:{lapInfo.LapNumber}");
            if (lapInfo.LapTelemetryInfo.IsTelemetryPurged)
            {
                Logger.Error("Lap Is PURGED! Cannot Save");
                return false;
            }

            await Task.Run(() => TrySaveLapTelemetrySync(lapInfo, out string _));
            return true;
        }

        public Task StartControllerAsync()
        {
            _lapEventProvider.BestLapPersonalChanged += LapEventProviderOnLapPersonalChanged;
            _lapEventProvider.LapCompleted += LapEventProviderOnLapCompleted;
            return Task.CompletedTask;
        }

        public Task StopControllerAsync()
        {
            _lapEventProvider.BestLapPersonalChanged -= LapEventProviderOnLapPersonalChanged;
            _lapEventProvider.LapCompleted -= LapEventProviderOnLapCompleted;
            return Task.CompletedTask;
        }

        private bool TrySaveLapTelemetrySync(ILapInfo lapInfo, out string lapFileName)
        {
            return TrySaveLap(lapInfo, string.Empty, out lapFileName);
        }

        private bool TrySaveLapTelemetrySync(ILapInfo lapInfo, string lapName, out string lapFileName)
        {
            if (_sessionInfoDto == null)
            {
                _sessionInfoDto = CreateSessionInfo(lapInfo);
            }

            return TrySaveLap(lapInfo, lapName, out lapFileName);
        }

        private bool TrySaveLap(ILapInfo lapInfo, string customName, out string lapFileName)
        {
            try
            {
                LapSummaryDto lapSummaryDto = CreateLapSummary(lapInfo, customName);
                Logger.Info($"Saving Telemetry Lap - {lapSummaryDto.FileName}");
                LapTelemetryDto lapTelemetryDto = CreateLapTelemetryDto(lapInfo, lapSummaryDto);

                _sessionInfoDto.LapsSummary.Where(x => x.FileName == lapSummaryDto.FileName).ToList().ForEach(x => _sessionInfoDto.LapsSummary.Remove(x));
                _sessionInfoDto.LapsSummary.Add(lapSummaryDto);

                _telemetryRepository.SaveRecentSessionInformation(_sessionInfoDto, SessionIdentifier);
                lapInfo.LapTelemetryInfo.TelemetryFilePath = _telemetryRepository.SaveRecentSessionLap(lapTelemetryDto, SessionIdentifier, lapSummaryDto.FileName, true);

                if (_settingsProvider.DisplaySettingsViewModel.TelemetrySettingsViewModel.IsSaveAlsoAsXMlEnabled)
                {
                    _telemetryRepository.SaveRecentSessionLapAsXml(lapTelemetryDto, SessionIdentifier, lapSummaryDto.FileName, false);
                }

                if (_settingsProvider.DisplaySettingsViewModel.TelemetrySettingsViewModel.IsSaveAlsoAsJsonEnabled)
                {
                    _telemetryRepository.SaveRecentSessionLapAsJson(lapTelemetryDto, SessionIdentifier, lapSummaryDto.FileName, false);
                }

                Logger.Info($"Completed Saving Telemetry Lap - {lapSummaryDto.FileName}");
                lapFileName = lapSummaryDto.FileName;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Uanble to Save Telemetry");
                lapFileName = string.Empty;
                return false;
            }
        }

        private LapTelemetryDto CreateLapTelemetryDto(ILapInfo lapInfo, LapSummaryDto lapSummaryDto)
        {
            var lapTelemetry = lapInfo.LapTelemetryInfo.TimedTelemetrySnapshots.Snapshots.ToList();

            TimedTelemetrySnapshot fistSnapshotsByDistance = lapTelemetry.First(x => x.PlayerData.LapDistance < _sessionInfoDto.LayoutLength * 0.5);

            LapTelemetryDto lapTelemetryDto = new LapTelemetryDto(LapTelemetryDto.CurrentVersion)
            {
                LapSummary = lapSummaryDto,
                DataPoints = lapTelemetry.Skip(lapTelemetry.ToList().IndexOf(fistSnapshotsByDistance)).ToList()
            };
            return lapTelemetryDto;
        }

        private LapSummaryDto CreateLapSummary(ILapInfo lapInfo, string lapName)
        {
            LapSummaryDto lapSummaryDto = new LapSummaryDto()
            {
                Id = lapInfo.LapGuid.ToString(),
                LapNumber = lapInfo.LapNumber,
                LapTimeSeconds = lapInfo.LapTime.TotalSeconds,
                Sector1Time = lapInfo.Sector1?.Duration ?? TimeSpan.Zero,
                Sector2Time = lapInfo.Sector2?.Duration ?? TimeSpan.Zero,
                Sector3Time = lapInfo.Sector3?.Duration ?? TimeSpan.Zero,
                SessionIdentifier = SessionIdentifier,
                Simulator = _sessionInfoDto.Simulator,
                TrackName = _sessionInfoDto.TrackName,
                LayoutName = _sessionInfoDto.LayoutName,
                LayoutLength = _sessionInfoDto.LayoutLength,
                Stint = lapInfo.StintNumber,
                IsPlayer = lapInfo.Driver.IsPlayer,
                DriverName = lapInfo.Driver.DriverShortName,
                IsValid = lapInfo.Valid,
            };

            if (!string.IsNullOrEmpty(lapName))
            {
                lapSummaryDto.CustomDisplayName = lapName;
            }

            lapSummaryDto.FileName = string.Join("_", $"{lapSummaryDto.DriverName}-{lapSummaryDto.LapNumber}-{lapSummaryDto.CustomDisplayName}-{lapSummaryDto.Id}".Split(Path.GetInvalidFileNameChars()));
            return lapSummaryDto;
        }

        private SessionInfoDto CreateSessionInfo(ILapInfo lapInfo)
        {
            DriverInfo driverToUse = lapInfo.Driver.Session.Player?.DriverInfo ?? lapInfo.Driver.DriverInfo;
            SessionInfoDto sessionInfoDto = new SessionInfoDto()
            {
                CarName = driverToUse.CarDisplayName,
                Id = SessionIdentifier,
                TrackName = lapInfo.Driver.Session.LastSet.SessionInfo.TrackInfo.TrackName,
                LayoutName = lapInfo.Driver.Session.LastSet.SessionInfo.TrackInfo.TrackLayoutName,
                LayoutLength = lapInfo.Driver.Session.LastSet.SessionInfo.TrackInfo.LayoutLength.InMeters,
                PlayerName = driverToUse.DriverLongName,
                Simulator = lapInfo.Driver.Session.LastSet.Source,
                SessionRunDateTime = DateTime.Now,
                LapsSummary = new List<LapSummaryDto>(),
                SessionType = lapInfo.Driver.Session.SessionType.ToString()
            };
            return sessionInfoDto;
        }

        private void LapEventProviderOnLapPersonalChanged(object sender, BestLapChangedArgs e)
        {
            switch (e.NewLap.Driver.IsPlayer)
            {
                case false when e.NewLap.LapTelemetryInfo?.TimedTelemetrySnapshots?.Snapshots.Any() == true:
                    _taskQueue.EnqueueTask(() => BestLapEventProviderOnBestLapPersonalChangedInternal(e));
                    break;
                case false when _settingsProvider.DisplaySettingsViewModel.TelemetrySettingsViewModel.LogOpponentsBest || _settingsProvider.DisplaySettingsViewModel.TelemetrySettingsViewModel.LogOpponentsAll:
                    Logger.Warn($"Unable to Export Personal Best Lap Driver: '{e.NewLap.Driver.DriverId}', Lap: {e.NewLap.LapNumber}, {(e.NewLap.LapTelemetryInfo?.TimedTelemetrySnapshots == null ? "TimedTelemetrySnapshots is null" : "No Snapshots")}.");
                    break;
            }
        }

        private void LapEventProviderOnLapCompleted(object sender, LapEventArgs e)
        {
            if (!e.Lap.Driver.IsPlayer && _settingsProvider.DisplaySettingsViewModel.TelemetrySettingsViewModel.LogOpponentsAll && e.Lap.LapTelemetryInfo?.TimedTelemetrySnapshots?.Snapshots.Any() == true)
            {
                _taskQueue.EnqueueTask(() => LogOpponentsLapInternal(e.Lap));
            }
        }

        private Task<bool> LogOpponentsLapInternal(ILapInfo lap)
        {
            return Task.Run(() => TrySaveLapTelemetrySync(lap, out _));
        }

        private async Task BestLapEventProviderOnBestLapPersonalChangedInternal(BestLapChangedArgs e)
        {
            ILapInfo bestLap = e.NewLap;
            if (!bestLap.Valid || bestLap.LapTime == TimeSpan.Zero)
            {
                Logger.Error($"Unable to Save Personal best for driver {bestLap.Driver.DriverId} saved. Lap is {bestLap.Valid}, Lap Time is {bestLap.LapTime.FormatToDefault()}");
                return;
            }

            try
            {
                if (!bestLap.Driver.IsPlayer && (_overallBest == null || _overallBest.LapTime == TimeSpan.Zero || _overallBest.LapTime > bestLap.LapTime))
                {
                    _overallBest = bestLap;
                    await TryExportOverallBest();
                }

                await TryExportPersonalBest(bestLap);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unable to Save Personal best for driver {bestLap.Driver.DriverId}.");
            }

            Logger.Trace($"Personal best for driver {bestLap.Driver.DriverId} saved");
        }

        private async Task<bool> TryExportPersonalBest(ILapInfo bestLap)
        {
            if (bestLap.Driver.Session.Player != null && bestLap.Driver.Session.Player.CarClassId != bestLap.Driver.CarClassId && _settingsProvider.DisplaySettingsViewModel.TelemetrySettingsViewModel.LogOnlyPlayerClass)
            {
                return false;
            }

            if (!_settingsProvider.DisplaySettingsViewModel.TelemetrySettingsViewModel.LogBestForEachDriver || !_settingsProvider.DisplaySettingsViewModel.TelemetrySettingsViewModel.IsTelemetryLoggingEnabled)
            {
                return false;
            }

            if (_personalBestMap.TryGetValue(bestLap.Driver.DriverLongName, out string fileName))
            {
                _sessionInfoDto.LapsSummary.RemoveAll(x => x.FileName == fileName);
                _telemetryRepository.DeleteSessionLap(SessionIdentifier, fileName);
            }

            string lapName = $"Best";
            string lapFileName = string.Empty;
            await Task.Run(() => TrySaveLapTelemetrySync(bestLap, lapName, out lapFileName));
            _personalBestMap[bestLap.Driver.DriverLongName] = lapFileName;
            return true;
        }

        private Task<bool> TryExportOverallBest()
        {
            if (_overallBest.Driver.Session.Player != null && _overallBest.Driver.Session.Player.CarClassId != _overallBest.Driver.CarClassId && _settingsProvider.DisplaySettingsViewModel.TelemetrySettingsViewModel.LogOnlyPlayerClass)
            {
                return Task.FromResult(false);
            }

            if (!_settingsProvider.DisplaySettingsViewModel.TelemetrySettingsViewModel.LogOpponentsBest || !_settingsProvider.DisplaySettingsViewModel.TelemetrySettingsViewModel.IsTelemetryLoggingEnabled)
            {
                return Task.FromResult(false);
            }

            if (!string.IsNullOrEmpty(_overallBestFileName))
            {
                _sessionInfoDto.LapsSummary.RemoveAll(x => x.FileName == _overallBestFileName);
                _telemetryRepository.DeleteSessionLap(SessionIdentifier, _overallBestFileName);
            }

            string lapName = $"Best Opponents";
            Task<bool> returnTask = Task.Run(() => TrySaveLapTelemetrySync(_overallBest, lapName, out _overallBestFileName));
            return returnTask;
        }
    }
}