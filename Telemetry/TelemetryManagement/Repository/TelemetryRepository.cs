namespace SecondMonitor.Telemetry.TelemetryManagement.Repository
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.Serialization;

    using DTO;
    using Migrations;

    using Newtonsoft.Json;

    using NLog;
    using ProtoBuf;

    using SecondMonitor.DataModel.Extensions;

    public class TelemetryRepository : ITelemetryRepository
    {
        public const string FileSuffix = ".pLap";
        public const string FileSuffixCompressed = ".zLap";

        private readonly object _repositoryLock;
        public const string SessionInfoFile = "_Session.xml";
        private const string FileOldSuffix = ".Lap";
        private const string RecentDir = "Recent";
        private const string ArchiveDir = "Archive";
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly string _repositoryDirectory;
        private readonly int _maxStoredSessions;
        private readonly List<ITelemetryMigration> _telemetryMigrations;
        private readonly ConcurrentDictionary<string, (string Directory, bool IsRecent)> _sessionIdToDirectoryDictionary;
        private readonly XmlSerializer _sessionInfoSerializer;
        private readonly XmlSerializer _lapTelemetrySerializer;

        public TelemetryRepository(string repositoryDirectory, int maxStoredSessions, List<ITelemetryMigration> telemetryMigrations)
        {
            _repositoryLock = new object();
            _repositoryDirectory = repositoryDirectory;
            _maxStoredSessions = maxStoredSessions;
            _telemetryMigrations = telemetryMigrations;
            _sessionInfoSerializer = new XmlSerializer(typeof(SessionInfoDto));
            _lapTelemetrySerializer = new XmlSerializer(typeof(LapTelemetryDto));
            _sessionIdToDirectoryDictionary = new ConcurrentDictionary<string, (string Directory, bool IsRecent)>();
        }

        private static string GetFileNameWithExtension(string fullFileName)
        {
            if (File.Exists(fullFileName))
            {
                return fullFileName;
            }

            string compressedFileName = fullFileName + FileSuffixCompressed;
            if (File.Exists(compressedFileName))
            {
                return compressedFileName;
            }

            string unCompressedFileName = fullFileName + FileSuffix;
            if (File.Exists(unCompressedFileName))
            {
                return unCompressedFileName;
            }

            return fullFileName + FileOldSuffix;
        }

        public IReadOnlyCollection<SessionInfoDto> GetAllRecentSessions()
        {
            string directory = Path.Combine(Path.Combine(_repositoryDirectory, RecentDir));
            return GetAllSessionsFromDirectory(new DirectoryInfo(directory), true);
        }

        public IReadOnlyCollection<SessionInfoDto> GetAllArchivedSessions()
        {
            string directory = Path.Combine(Path.Combine(_repositoryDirectory, ArchiveDir));
            return !Directory.Exists(directory) ? new List<SessionInfoDto>() : GetAllSessionsFromDirectory(new DirectoryInfo(directory), false);
        }

        public IEnumerable<SessionInfoDto> LoadPreviouslyLoadedSessions(List<SessionInfoDto> sessions)
        {
            foreach (SessionInfoDto previouslyOpenedSession in sessions)
            {
                string sessionSummaryFilePath = previouslyOpenedSession.SessionTransientData.SessionSummaryFilePath;
                if (!File.Exists(sessionSummaryFilePath) || File.GetLastWriteTime(sessionSummaryFilePath) == previouslyOpenedSession.SessionTransientData.SessionSummaryFileLastModified)
                {
                    continue;
                }

                if (_sessionIdToDirectoryDictionary.TryGetValue(previouslyOpenedSession.Id, out (string Directory, bool IsRecent) entry))
                {
                    yield return OpenSession(entry.Directory, entry.IsRecent);
                }
            }
        }

        public void SaveRecentSessionInformation(SessionInfoDto sessionInfoDto, string sessionIdentifier)
        {
            string directory = Path.Combine(Path.Combine(_repositoryDirectory, RecentDir), sessionIdentifier);
            string fileName = Path.Combine(directory, SessionInfoFile);
            Logger.Info($"Saving session info to file: {fileName}");
            lock (_repositoryLock)
            {
                Directory.CreateDirectory(directory);
                Save(sessionInfoDto, fileName);
            }
        }

        public void SaveRecentSessionInformationDirectly(SessionInfoDto sessionInfoDto, string fullFilePath)
        {
            Save(sessionInfoDto, fullFilePath);
        }

        public string GetLastRecentSessionIdentifier()
        {
            string directory = Path.Combine(_repositoryDirectory, RecentDir);
            if (!Directory.Exists(directory))
            {
                return string.Empty;
            }

            Directory.CreateDirectory(directory);
            DirectoryInfo info = new DirectoryInfo(directory);
            DirectoryInfo[] dis = info.GetDirectories().OrderBy(x => x.CreationTime).ToArray();
            return dis.Last().Name;
        }

        public async Task ArchiveSessions(SessionInfoDto sessionInfoDto)
        {
            if (!_sessionIdToDirectoryDictionary.TryGetValue(sessionInfoDto.Id, out (string Directory, bool IsRecent) entry))
            {
                throw new InvalidOperationException($"Session {sessionInfoDto.Id} is not opened. Cannot archive");
            }

            if (!entry.IsRecent)
            {
                throw new InvalidOperationException($"Session {sessionInfoDto.Id} is not a recent session, cannot archive");
            }

            sessionInfoDto.Id = Guid.NewGuid().ToString();
            sessionInfoDto.LapsSummary.ForEach(x => x.SessionIdentifier = sessionInfoDto.Id);

            string archiveDir = Path.Combine(Path.Combine(_repositoryDirectory, ArchiveDir), sessionInfoDto.Id);

            if (Directory.Exists(archiveDir))
            {
                Directory.Delete(archiveDir, true);
            }

            Directory.CreateDirectory(archiveDir);

            DirectoryInfo startDirectory = new DirectoryInfo(entry.Directory);

            //Creates all of the directories and sub-directories
            foreach (FileInfo file in startDirectory.EnumerateFiles())
            {
                using FileStream sourceStream = file.OpenRead();
                using (FileStream destinationStream = File.Create(Path.Combine(archiveDir, file.Name)))
                {
                    await sourceStream.CopyToAsync(destinationStream);
                }
            }

            sessionInfoDto.SessionTransientData.SessionSummaryFilePath = Path.Combine(archiveDir, SessionInfoFile);
        }

        public async Task OpenSessionFolder(SessionInfoDto sessionInfoDto)
        {
            if (!_sessionIdToDirectoryDictionary.TryGetValue(sessionInfoDto.Id, out (string Directory, bool IsRecent) entry))
            {
                throw new InvalidOperationException($"Session {sessionInfoDto.Id} is not opened. Cannot open folder");
            }

            await Task.Run(() => { Process.Start(new ProcessStartInfo(entry.Directory) { UseShellExecute = true }); });
        }

        public void DeleteSession(SessionInfoDto sessionInfoDto)
        {
            if (!_sessionIdToDirectoryDictionary.TryGetValue(sessionInfoDto.Id, out (string Directory, bool IsRecent) entry))
            {
                throw new InvalidOperationException($"Session {sessionInfoDto.Id} is not opened.");
            }

            CloseSession(sessionInfoDto.Id);
            Logger.Info($"Removing {entry.Directory} by request");
            Directory.Delete(entry.Directory, true);
        }

        public async Task<SessionInfoDto> ImportTelemetry(string fileName)
        {
            Guid newGuid = Guid.NewGuid();
            string importDirectoryName = newGuid.ToString();
            string importDirectoryFullPath = Path.Combine(Path.Combine(Path.Combine(_repositoryDirectory, ArchiveDir)), importDirectoryName);
            await Task.Run(() => ZipFile.ExtractToDirectory(fileName, importDirectoryFullPath));

            SessionInfoDto importedSession = OpenSession(importDirectoryFullPath, false);
            _sessionIdToDirectoryDictionary.TryRemove(importedSession.Id, out (string Directory, bool IsRecent) outValue);
            importedSession.Id = importDirectoryName;
            importedSession.LapsSummary.ForEach(x => x.SessionIdentifier = importDirectoryName);
            SaveRecentSessionInformationDirectly(importedSession, importedSession.SessionTransientData.SessionSummaryFilePath);
            _sessionIdToDirectoryDictionary.TryAdd(importedSession.Id, (importDirectoryFullPath, false));
            return importedSession;
        }

        public bool DeleteSessionLap(string sessionIdentifier, string fileName)
        {
            string directory = Path.Combine(Path.Combine(_repositoryDirectory, RecentDir), sessionIdentifier);
            string fullFilePath = Path.Combine(directory, fileName);
            return DeleteSessionLap(fullFilePath);
        }

        public bool DeleteSessionLap(string fileNameWithoutExtension)
        {
            string fullFilePath = GetFileNameWithExtension(fileNameWithoutExtension);
            Logger.Info($"Deleting File: {fullFilePath}");
            if (!File.Exists(fullFilePath))
            {
                return false;
            }

            File.Delete(fullFilePath);
            return true;
        }

        public string SaveRecentSessionLap(LapTelemetryDto lapTelemetry, string sessionIdentifier, string fileName, bool compress)
        {
            string directory = Path.Combine(Path.Combine(_repositoryDirectory, RecentDir), sessionIdentifier);
            string fullFilePath = Path.Combine(directory, fileName);
            if (compress)
            {
                fullFilePath += FileSuffixCompressed;
            }
            else
            {
                fullFilePath += FileSuffix;
            }

            Logger.Info($"Saving lap info {lapTelemetry.LapSummary.LapNumber} to file: {fullFilePath}");
            Directory.CreateDirectory(directory);
            Save(lapTelemetry, fullFilePath, compress);
            return fullFilePath;
        }

        public string SaveRecentSessionLapAsXml(LapTelemetryDto lapTelemetry, string sessionIdentifier, string fileName, bool compress)
        {
            string directory = Path.Combine(Path.Combine(_repositoryDirectory, RecentDir), sessionIdentifier);
            string fullFilePath = Path.Combine(directory, fileName);
            fullFilePath += compress ? ".xml.zip" : ".xml";

            Logger.Info($"Saving xml lap info {lapTelemetry.LapSummary.LapNumber} to file: {fullFilePath}");
            Directory.CreateDirectory(directory);
            SaveAsXml(lapTelemetry, fullFilePath, compress);
            return fullFilePath;
        }
        
        public string SaveRecentSessionLapAsJson(LapTelemetryDto lapTelemetry, string sessionIdentifier, string fileName, bool compress)
        {
            string directory = Path.Combine(Path.Combine(_repositoryDirectory, RecentDir), sessionIdentifier);
            string fullFilePath = Path.Combine(directory, fileName);
            fullFilePath += compress ? ".json.zip" : ".json";

            Logger.Info($"Saving json lap info {lapTelemetry.LapSummary.LapNumber} to file: {fullFilePath}");
            Directory.CreateDirectory(directory);
            SaveAsJson(lapTelemetry, fullFilePath, compress);
            return fullFilePath;
        }

        public SessionInfoDto OpenRecentSession(string sessionIdentifier)
        {
            string directory = Path.Combine(Path.Combine(_repositoryDirectory, RecentDir), sessionIdentifier);
            return OpenSession(directory, true);
        }

        public void CloseSession(string sessionIdentifier)
        {
            _sessionIdToDirectoryDictionary.TryRemove(sessionIdentifier, out (string Directory, bool IsRecent) entry);
        }

        public LapTelemetryDto LoadLapTelemetryDtoFromAnySession(LapSummaryDto lapSummaryDto)
        {
            if (!_sessionIdToDirectoryDictionary.TryGetValue(lapSummaryDto.SessionIdentifier, out (string Directory, bool IsRecent) entry))
            {
                throw new InvalidOperationException($"Session {lapSummaryDto.SessionIdentifier} is not opened. Unable to load lap {lapSummaryDto.Id}");
            }

            string fileName = Path.Combine(entry.Directory, lapSummaryDto.FileName);
            var loadedLapTelemetry = LoadLapTelemetryDto(new FileInfo(fileName));

            //Lazy Ids update in case the laps originated in different session
            loadedLapTelemetry.LapSummary.Id = lapSummaryDto.Id;
            loadedLapTelemetry.LapSummary.SessionIdentifier = lapSummaryDto.SessionIdentifier;
            return loadedLapTelemetry;
        }

        public LapTelemetryDto LoadLapTelemetryDto(FileInfo file)
        {
            string fullFileName = GetFileNameWithExtension(file.FullName);
            Logger.Info($"Loading from file: {fullFileName}");

            LapTelemetryDto dto;
            if (fullFileName.EndsWith(FileSuffixCompressed))
            {
                using var fileProtoBuf = File.OpenRead(fullFileName);
                using (var gzipStream = new GZipStream(fileProtoBuf, CompressionMode.Decompress))
                {
                    dto = Serializer.Deserialize<LapTelemetryDto>(gzipStream);
                }
            }
            else
            {
                using (var fileProtoBuf = File.OpenRead(fullFileName))
                {
                    dto = Serializer.Deserialize<LapTelemetryDto>(fileProtoBuf);
                }
            }

            MigrateLapTelemetry(dto);
            return dto;
        }

        private void MigrateLapTelemetry(LapTelemetryDto lapTelemetryDto)
        {
            lapTelemetryDto.DataPoints.Where(x => string.IsNullOrWhiteSpace(x.PlayerData.DriverSessionId)).ForEach(x => x.PlayerData.DriverSessionId = x.PlayerData.DriverShortName);
            _telemetryMigrations.Where(x => x.TargetVersion >= lapTelemetryDto.Version).OrderBy(x => x.TargetVersion).ForEach(x => x.MigrateUp(lapTelemetryDto));
        }

        private SessionInfoDto OpenSession(string sessionDirectory, bool isRecent)
        {
            string fileName = Path.Combine(sessionDirectory, SessionInfoFile);
            Logger.Info($"Loading Session info: {fileName}");
            SessionInfoDto sessionInfoDto;
            DateTime fileLastModifiedDateTime = File.GetLastWriteTime(fileName);

            using (FileStream file = File.Open(fileName, FileMode.Open))
            {
               sessionInfoDto = (SessionInfoDto)_sessionInfoSerializer.Deserialize(file);
            }

            sessionInfoDto.SessionTransientData.SessionSummaryFilePath = fileName;
            sessionInfoDto.SessionTransientData.SessionSummaryFileLastModified = fileLastModifiedDateTime;

            if (_sessionIdToDirectoryDictionary.TryAdd(sessionInfoDto.Id, (sessionDirectory, isRecent)))
            {
                return sessionInfoDto;
            }

            _sessionIdToDirectoryDictionary.TryRemove(sessionInfoDto.Id, out (string Directory, bool IsRecent) outValue);
            _sessionIdToDirectoryDictionary.TryAdd(sessionInfoDto.Id, (sessionDirectory, isRecent));

            return sessionInfoDto;
        }

        private void Save(SessionInfoDto sessionInfoDto, string path)
        {
            lock (_repositoryLock)
            {
                using (FileStream file = File.Exists(path) ? File.Open(path, FileMode.Truncate) : File.Create(path))
                {
                    _sessionInfoSerializer.Serialize(file, sessionInfoDto);
                }
            }

            RemoveObsoleteSessionsSafe();
        }

        public void Save(LapTelemetryDto lapTelemetryDto, string path, bool compress)
        {
            lock (_repositoryLock)
            {
                if (compress)
                {
                    using (Stream fileProtoBuf = File.Create(path))
                    {
                        using (var zipStream = new GZipStream(fileProtoBuf, CompressionLevel.Optimal))
                        {
                            Serializer.Serialize(zipStream, lapTelemetryDto);
                            zipStream.Close();
                        }

                        fileProtoBuf.Close();
                    }
                }
                else
                {
                    using (Stream fileProtoBuf = File.Create(path))
                    {
                        Serializer.Serialize(fileProtoBuf, lapTelemetryDto);
                    }
                }
            }
        }

        public void SaveAsJson(LapTelemetryDto lapTelemetryDto, string path, bool compress)
        {
            lock (_repositoryLock)
            {
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None,
                    Formatting = Formatting.Indented
                };

                if (compress)
                {
                    using FileStream outFile = new FileStream(path, FileMode.CreateNew);
                    using GZipStream gzipStream = new GZipStream(outFile, CompressionMode.Compress);
                    {
                        using StreamWriter writer = new StreamWriter(gzipStream);
                        {
                            writer.Write(JsonConvert.SerializeObject(lapTelemetryDto, settings));
                            writer.Flush();
                            gzipStream.Flush();
                        }
                    }
                }
                else
                {
                    File.WriteAllText(path, JsonConvert.SerializeObject(lapTelemetryDto, settings));
                }
            }
        }

        public void SaveAsXml(LapTelemetryDto lapTelemetryDto, string path, bool compress)
        {
            lock (_repositoryLock)
            {
                if (compress)
                {
                    using FileStream writer = new FileStream(path, FileMode.CreateNew);
                    using (var zipStream = new GZipStream(writer, CompressionLevel.Optimal))
                    {
                        _lapTelemetrySerializer.Serialize(zipStream, lapTelemetryDto);
                        zipStream.Close();
                    }
                }
                else
                {
                    using (StreamWriter writer = new StreamWriter(path))
                    {
                        _lapTelemetrySerializer.Serialize(writer, lapTelemetryDto);
                    }
                }
            }
        }

        private void RemoveObsoleteSessionsSafe()
        {
            DirectoryInfo info = new DirectoryInfo(Path.Combine(_repositoryDirectory, RecentDir));
            DirectoryInfo[] dis = info.GetDirectories().OrderBy(x => x.CreationTime).ToArray();
            if (dis.Length <= _maxStoredSessions)
            {
                return;
            }

            int toDelete = dis.Length - _maxStoredSessions;
            for (int i = 0; i < toDelete; i++)
            {
                Logger.Info($"Removing {dis[i].Name} because it is obsolete");

                try
                {
                    Directory.Delete(dis[i].FullName, true);
                }
                catch (IOException ex)
                {
                    Logger.Warn(ex, "Unable to delete obsolte session");
                }
            }
        }

        private IReadOnlyCollection<SessionInfoDto> GetAllSessionsFromDirectory(DirectoryInfo directory, bool recent)
        {
            DirectoryInfo[] dis = directory.GetDirectories().OrderBy(x => x.CreationTime).ToArray();

            if (dis.Length == 0)
            {
                return Enumerable.Empty<SessionInfoDto>().ToList().AsReadOnly();
            }

            List<SessionInfoDto> sessions = new List<SessionInfoDto>();
            foreach (var x in dis)
            {
                try
                {
                    sessions.Add(OpenSession(x.FullName, recent));
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Unable to load session {x.FullName}");
                }
            }

            return sessions.AsReadOnly();
        }
    }
}
