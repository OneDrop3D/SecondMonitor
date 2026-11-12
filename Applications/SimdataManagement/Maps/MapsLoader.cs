namespace SecondMonitor.SimdataManagement
{
    using System;
    using System.IO;
    using System.Xml;
    using System.Xml.Serialization;
    using Contracts.TrackMap;
    using DataModel.TrackMap;
    using NLog;

    public class MapsLoader : IMapsLoader
    {
        public const string MapFileSuffix = ".xml";
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly XmlSerializer _xmlSerializer;
        private readonly BuildInTrackMapsProvider _buildInTrackMapsProvider;

        public MapsLoader(string mapsPath, BuildInTrackMapsProvider buildInTrackMapsProvider)
        {
            _buildInTrackMapsProvider = buildInTrackMapsProvider;
            MapsPath = mapsPath;
            _xmlSerializer = new XmlSerializer(typeof(TrackMapDto));
        }

        public string MapsPath { get; }

        public bool TryLoadMap(string simulator, string trackName, out TrackMapDto trackMapDto)
        {
            trackName = SanitizeTrackName(trackName);
            string fullPathName = Path.Combine(Path.Combine(MapsPath, simulator), trackName + MapFileSuffix);
            string fullPathNameAlternate = fullPathName.Replace("-", "_");
            Logger.Info($"Trying to Load Map for Simulator {simulator}, track: {trackName}, path: {fullPathName} ");
            if (!File.Exists(fullPathName) && File.Exists(fullPathNameAlternate))
            {
                Logger.Info($"Migrating map file to a new name format oldName {fullPathName}, new name {fullPathNameAlternate}");
                File.Move(fullPathNameAlternate, fullPathName);
            }

            if (!File.Exists(fullPathName))
            {
                if (TryGetBuildInMap(simulator, trackName, out trackMapDto))
                {
                    Logger.Info($"Found build in track : Simulator: {simulator}, Track: {trackName} ");
                    return true;
                }

                Logger.Info($"Map File not found : Simulator: {simulator}, Track: {trackName} ");
                trackMapDto = null;
                return false;
            }

            try
            {
                using (TextReader file = File.OpenText(fullPathName))
                {
                    XmlReader reader = XmlReader.Create(file, new XmlReaderSettings() { CheckCharacters = false });
                    trackMapDto = (TrackMapDto)_xmlSerializer.Deserialize(reader);
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error Loading Map");
                if (File.Exists(fullPathName))
                {
                    File.Delete(fullPathName);
                }

                trackMapDto = null;
                return false;
            }
        }

        public void SaveMap(string simulator, string trackName, TrackMapDto trackMapDto)
        {
            string directory = Path.Combine(MapsPath, simulator);
            Directory.CreateDirectory(directory);
            trackName = SanitizeTrackName(trackName);
            string fullPathName = Path.Combine(directory, trackName + MapFileSuffix);
            Logger.Info($"Trying to Save Map for Simulator {simulator}, track: {trackName}, path: {fullPathName} ");
            using (FileStream file = File.Exists(fullPathName) ? File.Open(fullPathName, FileMode.Truncate) : File.Create(fullPathName))
            {
                _xmlSerializer.Serialize(file, trackMapDto);
            }
        }

        public void RemoveMap(string simulator, string trackName)
        {
            string directory = Path.Combine(MapsPath, simulator);
            trackName = SanitizeTrackName(trackName);
            string fullPathName = Path.Combine(directory, trackName + MapFileSuffix);
            Logger.Info($"Trying to Remove Map for Simulator {simulator}, track: {trackName}, path: {fullPathName} ");
            if (File.Exists(fullPathName))
            {
                File.Delete(fullPathName);
            }
        }

        private bool TryGetBuildInMap(string simulator, string trackName, out TrackMapDto trackMapDto)
        {
            if (!_buildInTrackMapsProvider.TryGetMap(simulator, trackName, out trackMapDto))
            {
                return false;
            }

            SaveMap(simulator, trackName, trackMapDto);
            return true;
        }

        private string SanitizeTrackName(string trackName)
        {
            return trackName.Replace("/", string.Empty);
        }
    }
}