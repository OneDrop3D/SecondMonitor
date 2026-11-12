namespace SecondMonitor.ViewModels.Repository
{
    using System;
    using System.IO;
    using System.Xml.Serialization;

    using NLog;

    public abstract class AbstractXmlRepository<T> : IAbstractXmlRepository<T> where T : class, new()
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly XmlSerializer _xmlSerializer;

        protected AbstractXmlRepository()
        {
            _xmlSerializer = new XmlSerializer(typeof(T));
        }

        protected object LockObject { get; } = new object();

        protected abstract string RepositoryDirectory { get; }
        protected abstract string FileName { get; }

        protected static void BackupOld(string fileName)
        {
            try
            {
                for (int i = 20; i >= 0; i--)
                {
                    string originalFile = $"{fileName}.{i}";
                    string backupFile = $"{fileName}.{i + 1}";
                    if (File.Exists(originalFile))
                    {
                        File.Copy(originalFile, backupFile, true);
                    }
                }

                if (File.Exists(fileName))
                {
                    File.Copy(fileName, $"{fileName}.0", true);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unable to create backup of old files");
            }
        }

        protected void CheckDirectory()
        {
            if (!Directory.Exists(RepositoryDirectory))
            {
                Directory.CreateDirectory(RepositoryDirectory);
            }
        }

        public virtual T LoadOrCreateNew()
        {
            string fileName = Path.Combine(RepositoryDirectory, FileName);
            lock (LockObject)
            {
                CheckDirectory();
                if (TryLoadFile(fileName, out T deserializedObject))
                {
                    return deserializedObject;
                }

                if (TryLoadBackup(fileName, out deserializedObject))
                {
                    return deserializedObject;
                }

                return new T();
            }
        }

        protected bool TryLoadFile(string fileName, out T deserializedObject)
        {
            lock (LockObject)
            {
                if (!File.Exists(fileName))
                {
                    deserializedObject = null;
                    return false;
                }

                try
                {
                    using FileStream file = File.Open(fileName, FileMode.Open, FileAccess.Read);
                    if (_xmlSerializer.Deserialize(file) is T deserialized)
                    {
                        deserializedObject = deserialized;
                        return true;
                    }

                    file.Close();
                    Logger.Info($"Error while loading file {fileName}");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Error while loading file {fileName}");
                }

                deserializedObject = null;
                return false;
            }
        }

        protected bool TryLoadBackup(string fileName, out T deserializedObject)
        {
            for (int i = 1; i <= 20; i++)
            {
                string backupFile = $"{fileName}.{i}";
                Logger.Info($"Trying to load backup {backupFile}.");
                if (!File.Exists(backupFile))
                {
                    Logger.Info($"Backup not found.");
                    deserializedObject = null;
                    return false;
                }

                if (TryLoadFile(backupFile, out deserializedObject))
                {
                    Logger.Info($"Backup loaded.");
                    return true;
                }
            }

            deserializedObject = null;
            return false;
        }

        public void Save(T toSave)
        {
            string fileName = Path.Combine(RepositoryDirectory, FileName);
            lock (LockObject)
            {
                CheckDirectory();
                BackupOld(fileName);
                using (FileStream file = File.Exists(fileName) ? File.Open(fileName, FileMode.Truncate) : File.Create(fileName))
                {
                    _xmlSerializer.Serialize(file, toSave);
                    file.Close();
                }
            }
        }
    }
}
