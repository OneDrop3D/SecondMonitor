namespace SecondMonitor.AccConnector.Repository
{
    using System;
    using System.IO;
    using Newtonsoft.Json;

    public class AccSettingsRepository
    {
        private static readonly DirectoryInfo ConfigPath =
            new DirectoryInfo(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SecondMonitor",
                "Connectors",
                "ACC"));

        internal SectorDistances LoadOrCreateDefault(string trackName)
        {
            if (!ConfigPath.Exists)
            {
                ConfigPath.Create();
            }

            var fileInfo = new FileInfo(Path.Combine(ConfigPath.FullName, $"{trackName}.json"));

            SectorDistances sectorDistances;

            if (!fileInfo.Exists)
            {
                sectorDistances = new SectorDistances();
            }
            else
            {
                sectorDistances = JsonConvert.DeserializeObject<SectorDistances>(File.ReadAllText(fileInfo.FullName));
            }

            return sectorDistances;
        }

        internal void Save(string trackName, SectorDistances sectorDistances)
        {
            var fileInfo = new FileInfo(Path.Combine(ConfigPath.FullName, $"{trackName}.json"));

            var serializeObject = JsonConvert.SerializeObject(sectorDistances);
            File.WriteAllText(fileInfo.FullName, serializeObject);
        }
    }
}