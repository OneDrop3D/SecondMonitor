namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.TelemetryLoad
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using DataFillers;

    using DataModel.Telemetry;
    using NLog;
    using Repository;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels.Settings;
    using Synchronization;
    using TelemetryManagement.DTO;
    using TelemetryManagement.Repository;

    public class TelemetryLoadController : ITelemetryLoadController
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly List<SessionInfoDto> _loadedSessions;
        private readonly List<string> _knownLaps;
        private readonly ITelemetryViewsSynchronization _telemetryViewsSynchronization;
        private readonly IDataPointSelectionSynchronization _dataPointSelectionSynchronization;
        private readonly IMissingTelemetryFiller[] _missingTelemetryFillers;
        private readonly ITelemetryRepository _telemetryRepository;
        private readonly ConcurrentDictionary<string, LapTelemetryDto> _cachedTelemetries;
        private int _activeLapJobs;

        private SessionInfoDto _mainSession;
        private Task _loopTask;
        private CancellationTokenSource _loopTaskSource;

        public TelemetryLoadController(ITelemetryRepositoryFactory telemetryRepositoryFactory, ISettingsProvider settingsProvider,
            ITelemetryViewsSynchronization telemetryViewsSynchronization, IEnumerable<IMissingTelemetryFiller> missingTelemetryFillers, IDataPointSelectionSynchronization dataPointSelectionSynchronization)
        {
            _cachedTelemetries = new ConcurrentDictionary<string, LapTelemetryDto>();
            _loadedSessions = new List<SessionInfoDto>();
            _knownLaps = new List<string>();
            _telemetryViewsSynchronization = telemetryViewsSynchronization;
            _dataPointSelectionSynchronization = dataPointSelectionSynchronization;
            _missingTelemetryFillers = missingTelemetryFillers.ToArray();
            _telemetryRepository = telemetryRepositoryFactory.Create(settingsProvider);
        }

        public async Task<IReadOnlyCollection<SessionInfoDto>> GetAllRecentSessionInfoAsync()
        {
            try
            {
                return await Task.Run(() => _telemetryRepository.GetAllRecentSessions());
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while loading recent sessions");
            }

            return Enumerable.Empty<SessionInfoDto>().ToList().AsReadOnly();
        }

        public async Task<IReadOnlyCollection<SessionInfoDto>> GetAllArchivedSessionInfoAsync()
        {
            try
            {
                return await Task.Run(() => _telemetryRepository.GetAllArchivedSessions());
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while loading archived sessions");
            }

            return new List<SessionInfoDto>();
        }

        public async Task RefreshLoadedSessions()
        {
            IReadOnlyCollection<SessionInfoDto> sessions = null;
            await Task.Run(() => sessions = _telemetryRepository.LoadPreviouslyLoadedSessions(_loadedSessions).ToList());

            if (sessions == null)
            {
                return;
            }

            List<LapSummaryDto> unknownLap = sessions.SelectMany(x => x.LapsSummary).Where(y => !_knownLaps.Contains(y.Id)).ToList();
            foreach (LapSummaryDto lapSummaryDto in unknownLap)
            {
                FillCustomDisplayName(lapSummaryDto);
                _knownLaps.Add(lapSummaryDto.Id);
            }

            if (unknownLap.Count > 0)
            {
                _telemetryViewsSynchronization.NotifyLapAddedToSession(unknownLap);
            }
        }

        public async Task<SessionInfoDto> LoadRecentSessionAsync(string sessionIdentifier)
        {
            try
            {
                SessionInfoDto sessionInfoDto = await Task.Run(() => _telemetryRepository.OpenRecentSession(sessionIdentifier));
                return await LoadRecentSessionAsync(sessionInfoDto);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while loading session");
            }

            return null;
        }

        public async Task<SessionInfoDto> LoadRecentSessionAsync(SessionInfoDto sessionInfoDto)
        {
            await CloseAllOpenedSessions();
            _cachedTelemetries.Clear();
            if (!_loadedSessions.Contains(sessionInfoDto))
            {
                _loadedSessions.Add(sessionInfoDto);
                sessionInfoDto.LapsSummary.ForEach(x => _knownLaps.Add(x.Id));
            }

            sessionInfoDto.LapsSummary.ForEach(FillCustomDisplayName);
            _telemetryViewsSynchronization.NotifyNewSessionLoaded(sessionInfoDto);
            sessionInfoDto.SessionTransientData.IsModified = false;
            _telemetryViewsSynchronization.NotifySessionModified(sessionInfoDto);
            _mainSession = sessionInfoDto;
            return sessionInfoDto;
        }

        public Task<SessionInfoDto> AddRecentSessionAsync(SessionInfoDto sessionInfoDto)
        {
            if (_loadedSessions.All(x => x.Id != sessionInfoDto.Id))
            {
                _loadedSessions.Add(sessionInfoDto);
                sessionInfoDto.LapsSummary.ForEach(x => _knownLaps.Add(x.Id));
            }

            sessionInfoDto.LapsSummary.ForEach(FillCustomDisplayName);
            AddLapsToMainSession(sessionInfoDto);
            sessionInfoDto.SessionTransientData.IsModified = true;
            _telemetryViewsSynchronization.NotifySessionAdded(sessionInfoDto);
            _telemetryViewsSynchronization.NotifySessionModified(_mainSession);
            return Task.FromResult(sessionInfoDto);
        }

        public async Task<SessionInfoDto> LoadLastSessionAsync()
        {
            try
            {
                string sessionIdent = _telemetryRepository.GetLastRecentSessionIdentifier();
                if (string.IsNullOrEmpty(sessionIdent))
                {
                    return null;
                }

                return await LoadRecentSessionAsync(sessionIdent);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while loading last session");
            }

            return null;
        }

        public async Task<LapTelemetryDto> LoadLap(LapSummaryDto lapSummaryDto)
        {
            try
            {
                AddToActiveLapJob();
                LapTelemetryDto lapTelemetryDto = null;
                await Task.Run(() => lapTelemetryDto = _cachedTelemetries.GetOrAdd(lapSummaryDto.Id, LoadLapTelemetryDto, lapSummaryDto));
                _telemetryViewsSynchronization.NotifyLapLoaded(lapTelemetryDto);
                return lapTelemetryDto;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while loading lap telemetry");
                return null;
            }
            finally
            {
                RemoveFromActiveLapJob();
            }
        }

        private LapTelemetryDto LoadLapTelemetryDto(string lapId, LapSummaryDto lapSummaryDto)
        {
            LapTelemetryDto lapTelemetryDto = _telemetryRepository.LoadLapTelemetryDtoFromAnySession(lapSummaryDto);
            _missingTelemetryFillers.ForEach(x => x.SetSimulator(lapSummaryDto.Simulator));
            lapTelemetryDto.Accept(_missingTelemetryFillers.OfType<ITimedTelemetrySnapshotVisitor>().ToArray());
            FillCustomDisplayName(lapTelemetryDto.LapSummary);
            return lapTelemetryDto;
        }

        public async Task<LapTelemetryDto> LoadLap(FileInfo file, string customDisplayName)
        {
            try
            {
                AddToActiveLapJob();
                if (!_cachedTelemetries.TryGetValue(file.FullName, out LapTelemetryDto lapTelemetryDto))
                {
                    lapTelemetryDto = await Task.Run(() => _telemetryRepository.LoadLapTelemetryDto(file));
                    lapTelemetryDto.LapSummary.CustomDisplayName = customDisplayName;
                    _cachedTelemetries[lapTelemetryDto.LapSummary.Id] = lapTelemetryDto;
                    _mainSession.SessionTransientData.AddLap(lapTelemetryDto.LapSummary, file.FullName);
                    _mainSession.SessionTransientData.IsModified = true;
                    _telemetryViewsSynchronization.NotifySessionModified(_mainSession);
                }

                _telemetryViewsSynchronization.NotifyLapAddedToSession(lapTelemetryDto.LapSummary);
                return lapTelemetryDto;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while loading lap telemetry");
                return null;
            }
            finally
            {
                RemoveFromActiveLapJob();
            }
        }

        public async Task<IEnumerable<LapTelemetryDto>> LoadLaps(IEnumerable<LapSummaryDto> lapSummaries)
        {
            var lapTelemetryDtos = new ConcurrentBag<LapTelemetryDto>();
            try
            {
                AddToActiveLapJob();
                await Task.Run(() =>
                    Parallel.ForEach(lapSummaries, lapSummary =>
                    {
                        LapTelemetryDto lapTelemetry = _cachedTelemetries.GetOrAdd(lapSummary.Id, LoadLapTelemetryDto, lapSummary);
                        lapTelemetryDtos.Add(lapTelemetry);
                    }));

                List<LapTelemetryDto> filteredLapTelemetryDtos = lapTelemetryDtos.DistinctBy(x => x.LapSummary.Id).ToList();

                _telemetryViewsSynchronization.NotifyLapLoaded(filteredLapTelemetryDtos);
                return filteredLapTelemetryDtos;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while loading lap telemetry");
                return null;
            }
            finally
            {
                RemoveFromActiveLapJob();
            }
        }

        public Task UnloadLap(LapSummaryDto lapSummaryDto)
        {
            _telemetryViewsSynchronization.NotifyBusyStateChanged(true);
            UnloadLapInternal(lapSummaryDto);
            _telemetryViewsSynchronization.NotifyLapLoadingFinished();
            _telemetryViewsSynchronization.NotifyBusyStateChanged(false);
            return Task.CompletedTask;
        }

        public Task UnloadLaps(IEnumerable<LapSummaryDto> lapSummaryDtos)
        {
            _telemetryViewsSynchronization.NotifyBusyStateChanged(true);
            lapSummaryDtos.ForEach(UnloadLapInternal);
            _telemetryViewsSynchronization.NotifyLapLoadingFinished();
            _telemetryViewsSynchronization.NotifyBusyStateChanged(false);
            return Task.CompletedTask;
        }

        private void UnloadLapInternal(LapSummaryDto lapSummaryDto)
        {
            _mainSession?.SessionTransientData.ModifiedLaps.Remove(lapSummaryDto);
            _telemetryViewsSynchronization.NotifyLapUnloaded(lapSummaryDto);
            _cachedTelemetries.TryRemove(lapSummaryDto.Id, out LapTelemetryDto value);
        }

        public async Task ArchiveSession(SessionInfoDto sessionInfoDto)
        {
            _telemetryViewsSynchronization.NotifyBusyStateChanged(true);
            await _telemetryRepository.ArchiveSessions(sessionInfoDto);
            if (_mainSession.Id == sessionInfoDto.Id)
            {
                await SaveMainSession();
            }

            _telemetryViewsSynchronization.NotifyBusyStateChanged(false);
        }

        public async Task OpenSessionFolder(SessionInfoDto sessionInfoDto)
        {
            await _telemetryRepository.OpenSessionFolder(sessionInfoDto);
        }

        public async Task SaveMainSession()
        {
            _telemetryViewsSynchronization.NotifyBusyStateChanged(true);
            await Task.Run(SaveMainSessionInternal);
            _telemetryViewsSynchronization.NotifyBusyStateChanged(false);
        }

        public void MarkLapAsModified(LapSummaryDto lapSummaryDto)
        {
            if (_mainSession == null)
            {
                return;
            }

            _mainSession.SessionTransientData.ModifiedLaps.Add(lapSummaryDto);
            _mainSession.SessionTransientData.IsModified = true;
            _telemetryViewsSynchronization.NotifySessionModified(_mainSession);
        }

        private void SaveMainSessionInternal()
        {
            string mainSessionDirectory = Path.GetDirectoryName(_mainSession?.SessionTransientData.SessionSummaryFilePath);

            if (mainSessionDirectory == null)
            {
                return;
            }

            Logger.Info($"Saving Session {_mainSession.Id}");

            foreach ((LapSummaryDto lapSummary, string fullPathName) in _mainSession.SessionTransientData.AddedLaps)
            {
                string fileName = Path.GetFileName(fullPathName);
                string newFilePath = Path.Combine(mainSessionDirectory, fileName);
                if (File.Exists(newFilePath))
                {
                    Logger.Info($"File Already Exists ${newFilePath}");
                    fileName = Guid.NewGuid().ToString();
                    newFilePath = Path.Combine(mainSessionDirectory, fileName);
                    Logger.Info($"Renmaing to {newFilePath}");
                }

                lapSummary.FileName = fileName;
                lapSummary.SessionIdentifier = _mainSession.Id;
                _mainSession.LapsSummary.Add(lapSummary);

                Logger.Info($"Copying {fullPathName} to {newFilePath}");

                if (!File.Exists(fullPathName))
                {
                    Logger.Warn($"File, {fullPathName}, not found, skipping.");
                    continue;
                }

                File.Copy(fullPathName, newFilePath);
            }

            _mainSession.SessionTransientData.ModifiedLaps.ToList().ForEach(SaveModifiedLapInternal);

            foreach (string filePath in _mainSession.SessionTransientData.RemovedLaps.Select(removedLap => Path.Combine(mainSessionDirectory, removedLap.FileName)))
            {
                _telemetryRepository.DeleteSessionLap(filePath);
            }

            _telemetryRepository.SaveRecentSessionInformationDirectly(_mainSession, _mainSession.SessionTransientData.SessionSummaryFilePath);
            _mainSession.SessionTransientData.PurgeAfterSave();
            _mainSession.SessionTransientData.IsModified = false;
            _telemetryViewsSynchronization.NotifySessionModified(_mainSession);
        }

        public void DeleteSession(SessionInfoDto sessionInfoDto)
        {
            _telemetryRepository.DeleteSession(sessionInfoDto);
        }

        public async Task RemoveLap(LapSummaryDto lapSummary)
        {
            if (_cachedTelemetries.TryGetValue(lapSummary.Id, out LapTelemetryDto lapTelemetryDto))
            {
                await UnloadLap(lapSummary);
            }

            _telemetryViewsSynchronization.NotifyLapRemoveFromSession(lapSummary);
            _mainSession.SessionTransientData.AddRemovedLap(lapSummary);
            _mainSession.LapsSummary.RemoveAll(x => x.Id == lapSummary.Id);
            _mainSession.SessionTransientData.IsModified = true;
            _telemetryViewsSynchronization.NotifySessionModified(_mainSession);
        }

        public async Task ExportTelemetry(SessionInfoDto sessionInfoDto, string exportFileName)
        {
            _telemetryViewsSynchronization.NotifyBusyStateChanged(true);
            try
            {
                if (File.Exists(exportFileName))
                {
                    File.Delete(exportFileName);
                }

                string sessionPath = Path.GetDirectoryName(sessionInfoDto.SessionTransientData.SessionSummaryFilePath);
                await Task.Run(() => ZipFile.CreateFromDirectory(sessionPath, exportFileName));
            }
            finally
            {
                _telemetryViewsSynchronization.NotifyBusyStateChanged(false);
            }
        }

        public Task<SessionInfoDto> ImportTelemetry(string fileName)
        {
            try
            {
                _telemetryViewsSynchronization.NotifyBusyStateChanged(true);
                return _telemetryRepository.ImportTelemetry(fileName);
            }
            finally
            {
                _telemetryViewsSynchronization.NotifyBusyStateChanged(false);
            }
        }

        public Task SaveModifiedLap(LapSummaryDto modifiedLap)
        {
            return Task.Run(() => SaveModifiedLapInternal(modifiedLap));
        }

        public void SaveModifiedLapInternal(LapSummaryDto modifiedLap)
        {
            if (_mainSession == null)
            {
                Logger.Warn("No Session Opened, Cannot save modified Lap");
                return;
            }

            string mainSessionDirectory = Path.GetDirectoryName(_mainSession.SessionTransientData.SessionSummaryFilePath);
            string filePath = Path.Combine(mainSessionDirectory ?? string.Empty, modifiedLap.FileName);

            if (!_cachedTelemetries.TryGetValue(modifiedLap.Id, out LapTelemetryDto lapTelemetryDto))
            {
                return;
            }

            _telemetryRepository.DeleteSessionLap(filePath);
            _telemetryRepository.Save(lapTelemetryDto, filePath, true);
            _mainSession.SessionTransientData.ModifiedLaps.Remove(modifiedLap);
        }

        private void AddToActiveLapJob()
        {
            _activeLapJobs++;
            if (_activeLapJobs == 1)
            {
                _telemetryViewsSynchronization.NotifyBusyStateChanged(true);
            }
        }

        private async Task CloseAllOpenedSessions()
        {
            while (_activeLapJobs > 0)
            {
                await Task.Delay(100);
            }

            _dataPointSelectionSynchronization.DeselectAllPoints();
            foreach (LapTelemetryDto value in _cachedTelemetries.Values)
            {
                await UnloadLap(value.LapSummary);
            }

            //TODO: Improve open telemetry sessions handling
            /*_loadedSessions.ForEach( x => _telemetryRepository.CloseSession(x.Id));*/
            _loadedSessions.Clear();
            _knownLaps.Clear();
        }

        private void RemoveFromActiveLapJob()
        {
            _activeLapJobs--;
            if (_activeLapJobs != 0)
            {
                return;
            }

            try
            {
                _telemetryViewsSynchronization.NotifyLapLoadingFinished();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                _telemetryViewsSynchronization.NotifyBusyStateChanged(false);
            }
        }

        private void FillCustomDisplayName(LapSummaryDto lapSummaryDto)
        {
            int sessionIndex = _loadedSessions.IndexOf(_loadedSessions.FirstOrDefault(x => x.Id == lapSummaryDto.SessionIdentifier));
            if (string.IsNullOrEmpty(lapSummaryDto.CustomDisplayName))
            {
                lapSummaryDto.CustomDisplayName = sessionIndex > 0 ? $"{sessionIndex}/{lapSummaryDto.LapNumber}" : lapSummaryDto.LapNumber.ToString();
            }
        }

        public Task StartControllerAsync()
        {
            if (_loopTask != null)
            {
                return Task.CompletedTask;
            }

            _loopTaskSource = new CancellationTokenSource();
            _loopTask = Task.CompletedTask; //RefreshLoop(_loopTaskSource.Token);
            return Task.CompletedTask;
        }

        public async Task StopControllerAsync()
        {
            if (_loopTask == null)
            {
                return;
            }

            try
            {
                _loopTaskSource.Cancel();
                await _loopTask;
            }
            catch (TaskCanceledException ex)
            {
                Logger.Info(ex, "Task Canceled");
            }
            finally
            {
                _cachedTelemetries.Clear();
            }
        }

        private void AddLapsToMainSession(SessionInfoDto sessionInfoDto)
        {
            if (_mainSession == null)
            {
                return;
            }

            string directoryPath = Path.GetDirectoryName(sessionInfoDto.SessionTransientData.SessionSummaryFilePath);
            if (string.IsNullOrEmpty(directoryPath))
            {
                return;
            }

            foreach (LapSummaryDto lapSummaryDto in sessionInfoDto.LapsSummary)
            {
                string fileName = Path.Combine(directoryPath, lapSummaryDto.FileName) + TelemetryRepository.FileSuffixCompressed;
                if (!File.Exists(fileName))
                {
                    continue;
                }

                _mainSession.SessionTransientData.AddLap(lapSummaryDto, fileName);
            }
        }
    }
}