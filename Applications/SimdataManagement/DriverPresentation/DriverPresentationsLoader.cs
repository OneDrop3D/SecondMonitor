namespace SecondMonitor.SimdataManagement.DriverPresentation
{
    using System;
    using System.IO;
    using System.Xml;
    using System.Xml.Serialization;
    using DataModel.DriversPresentation;
    using NLog;
    using ViewModels.Settings;

    public class DriverPresentationsLoader
    {
        private const string FileName = "DriverPresentations.xml";
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly string _storedDirectoryPath;

        private readonly XmlSerializer _xmlSerializer;

        public DriverPresentationsLoader(ISettingsProvider settingsProvider)
        {
            _storedDirectoryPath = settingsProvider.DriversPresentationFolder;
            _xmlSerializer = new XmlSerializer(typeof(DriverPresentationsDto));
        }

        public bool TryLoad(out DriverPresentationsDto driverPresentationsDto)
        {
            string fullPathName = Path.Combine(_storedDirectoryPath, FileName);
            try
            {
                using (TextReader file = File.OpenText(fullPathName))
                {
                    XmlReader reader = XmlReader.Create(file, new XmlReaderSettings() { CheckCharacters = false });
                    driverPresentationsDto = (DriverPresentationsDto)_xmlSerializer.Deserialize(reader);
                }

                return driverPresentationsDto != null;
            }
            catch (Exception)
            {
                if (File.Exists(fullPathName))
                {
                    File.Delete(fullPathName);
                }

                driverPresentationsDto = null;
                return false;
            }
        }

        public void Save(DriverPresentationsDto driverPresentationsDto)
        {
            try
            {
                string fullPathName = Path.Combine(_storedDirectoryPath, FileName);
                Directory.CreateDirectory(_storedDirectoryPath);
                using (FileStream file = File.Exists(fullPathName) ? File.Open(fullPathName, FileMode.Truncate) : File.Create(fullPathName))
                {
                    _xmlSerializer.Serialize(file, driverPresentationsDto);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while saving driver presentation");
            }
        }
    }
}