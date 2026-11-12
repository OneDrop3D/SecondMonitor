namespace SecondMonitor.R3EConnector
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using DataModel;
    using DataModel.Snapshot;
    using Foundation.Connectors;
    using NLog;
    using Rating;

    public class R3EConnector : AbstractGameConnector
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly string[] R3EExecutables = { "RRRE", "RRRE64" };
        private static readonly string SharedMemoryName = "$R3E";

        private readonly RatingProvider _ratingProvider;
        private readonly Stopwatch _sessionTimeWatch;
        private readonly R3EDatabase _r3EDatabase;

        private MemoryMappedFile _sharedMemory;
        private bool _inSession;
        private int _lastSessionType;
        private int _lastSessionPhase;
        private DateTime _lastPlayerMultipleTimesLogged;

        private TimeSpan _lastSessionTime = TimeSpan.Zero;

        public R3EConnector() : base(R3EExecutables)
        {
            _sessionTimeWatch = Stopwatch.StartNew();
            _ratingProvider = new RatingProvider();
            _r3EDatabase = new R3EDatabase();
            _r3EDatabase.Load();
            DataConverter = new R3EDataConverter(this, _ratingProvider, _r3EDatabase);
            TickTime = 16;
        }

        private R3EDataConverter DataConverter { get; set; }

        public override bool IsConnected => _sharedMemory != null;

        internal TimeSpan SessionTime => _sessionTimeWatch.Elapsed;

        protected override string ConnectorName => SimulatorsNameMap.R3ESimName;

        protected override void ResetConnector()
        {
            _inSession = false;
            _lastSessionType = -2;
            _sessionTimeWatch.Restart();
            _lastSessionTime = TimeSpan.Zero;
            _lastPlayerMultipleTimesLogged = DateTime.Now;
        }

        protected override void OnConnection()
        {
            ResetConnector();
            _sharedMemory = MemoryMappedFile.OpenExisting(SharedMemoryName);
            try
            {
                if (Process?.MainModule?.FileName == null)
                {
                    return;
                }

                string directory = Path.GetDirectoryName(Process.MainModule.FileName) ?? string.Empty;
                string[] directories = directory.Split(Path.DirectorySeparatorChar);
                if (directories[directories.Length - 1] == "x64")
                {
                    directories[directories.Length - 1] = string.Empty;
                }

                directory = directories.Skip(1).Aggregate(Path.Combine);
                string fileName = Path.Combine(directory, @"GameData\General\r3e-data.json");
                fileName = Path.Combine(Path.GetPathRoot(Process.MainModule.FileName), fileName);
                _r3EDatabase.Load(fileName);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Unable to load r3e database");
            }
        }

        protected override async Task DaemonMethod(CancellationToken cancellationToken)
        {
            await _ratingProvider.InitializeRating();

            while (!ShouldDisconnect)
            {
                await Task.Delay(TickTime, cancellationToken).ConfigureAwait(false);
                R3ESharedData r3RData = Load();

                SimulatorDataSet data = DataConverter.FromR3EData(r3RData);
                if (CheckSessionStarted(r3RData, data))
                {
                    _sessionTimeWatch.Restart();

                    _lastSessionTime = SessionTime;
                    RaiseSessionStartedEvent(data);
                }

                if (r3RData.GamePaused != 1 && r3RData.GameInReplay != 1 && !_sessionTimeWatch.IsRunning)
                {
                    _sessionTimeWatch.Start();
                }
                else if (_sessionTimeWatch.IsRunning && (r3RData.GamePaused == 1 || r3RData.GameInReplay == 1))
                {
                    _sessionTimeWatch.Stop();
                }

                _lastSessionTime = data.SessionInfo.SessionTime;
                //R3E sometimes reports the player twice, happens at race finish
                if (data.DriversInfo.Count(x => x.DriverSessionId == data.PlayerInfo?.DriverSessionId) > 1)
                {
                    if ((DateTime.Now - _lastPlayerMultipleTimesLogged).TotalSeconds > 60)
                    {
                        Logger.Info("Player is present more than once in data");
                        _lastPlayerMultipleTimesLogged = DateTime.Now;
                    }

                    continue;
                }

                RaiseDataLoadedEvent(data);

                if (r3RData.ControlType == -1 && !IsProcessRunning())
                {
                    ShouldDisconnect = true;
                }
            }

            _sharedMemory = null;
            _sessionTimeWatch.Stop();
            RaiseDisconnectedEvent();
        }

        private R3ESharedData Load()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Not connected");
            }

            MemoryMappedViewStream view = _sharedMemory.CreateViewStream();
            BinaryReader stream = new BinaryReader(view);
            byte[] buffer = stream.ReadBytes(Marshal.SizeOf(typeof(R3ESharedData)));
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            R3ESharedData data = (R3ESharedData)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(R3ESharedData));
            handle.Free();
            return data;
        }

        private bool CheckSessionStarted(R3ESharedData r3RData, SimulatorDataSet data)
        {
            if (r3RData.GameInReplay == 1)
            {
                return false;
            }

            if (r3RData.SessionType != _lastSessionType)
            {
                Logger.Info("New Session because of session type change");
                _lastSessionType = r3RData.SessionType;
                return true;
            }

            if (r3RData.SessionPhase != -1 && !_inSession)
            {
                Logger.Info("New session because of session phase change to valid");
                _inSession = true;
                return true;
            }

            if (_inSession && r3RData.SessionPhase == -1)
            {
                Logger.Info("New session because of session phase change to invalid");
                _inSession = false;
            }

            if (_lastSessionPhase >= 5 && r3RData.SessionPhase < 5)
            {
                Logger.Info("New session because of session phase restart");
                _lastSessionPhase = r3RData.SessionPhase;
                return true;
            }

            if (Math.Abs((data.SessionInfo.SessionTime - _lastSessionTime).TotalSeconds) > 10)
            {
                Logger.Trace("More than 10 seconds between data packets");
            }

            if ((data.SessionInfo.SessionTime - _lastSessionTime).TotalSeconds < -10)
            {
                Logger.Info($"New session because of session time reset OldSession time: {_lastSessionTime.TotalSeconds}, new sessionTime {data.SessionInfo.SessionTime.TotalSeconds}, gamesimulationtime: {r3RData.Player.GameSimulationTime}");
                return true;
            }

            _lastSessionPhase = r3RData.SessionPhase;
            return false;
        }
    }
}