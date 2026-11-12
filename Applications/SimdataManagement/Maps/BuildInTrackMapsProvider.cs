namespace SecondMonitor.SimdataManagement
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Serialization;

    using SecondMonitor.DataModel.TrackMap;

    public class BuildInTrackMapsProvider
    {
        private const string BuildInTrackFileName = "SecondMonitor.SimdataManagement.TrackMaps.zip";
        private readonly XmlSerializer _xmlSerializer;
        private readonly Lazy<List<TrackMapDto>> _trackMapsLazy;

        public BuildInTrackMapsProvider()
        {
            _xmlSerializer = new XmlSerializer(typeof(TrackMapDto));
            _trackMapsLazy = new Lazy<List<TrackMapDto>>(GetTrackMaps);
        }

        private List<TrackMapDto> GetTrackMaps()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using Stream stream = assembly.GetManifestResourceStream(BuildInTrackFileName);

            if (stream == null)
            {
                throw new ArgumentException($"Unable to find Resource File {BuildInTrackFileName}");
            }

            List<TrackMapDto> trackMapsDtos = new List<TrackMapDto>();
            using (ZipArchive zip = new ZipArchive(stream))
            {
                foreach (ZipArchiveEntry zipArchiveEntry in zip.Entries.Where(x => x.Name.EndsWith(MapsLoader.MapFileSuffix)))
                {
                    using Stream s = zipArchiveEntry.Open();
                    TrackMapDto trackMapDto = (TrackMapDto)_xmlSerializer.Deserialize(s);
                    trackMapsDtos.Add(trackMapDto);
                }
            }

            return trackMapsDtos;
        }

        public bool TryGetMap(string simulator, string trackName, out TrackMapDto trackMapDto)
        {
            List<TrackMapDto> trackMaps = _trackMapsLazy.Value;
            trackMapDto = trackMaps.Find(x => x.SimulatorSource == simulator && x.TrackFullName == trackName);
            return trackMapDto != null;
        }
    }
}
