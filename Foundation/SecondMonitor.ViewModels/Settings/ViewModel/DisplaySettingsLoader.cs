namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Migrations;
    using Model;
    using Newtonsoft.Json;
    using NLog;

    public class DisplaySettingsLoader
    {
        private readonly List<ISettingMigration> _settingMigration;

        public DisplaySettingsLoader(List<ISettingMigration> settingMigration)
        {
            _settingMigration = settingMigration.OrderBy(x => x.VersionAfterMigration).ToList();
        }

        public DisplaySettings LoadDisplaySettingsFromFileSafe(string fileName)
        {
            try
            {
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    Formatting = Formatting.Indented
                };
                object deserializedOptions = JsonConvert.DeserializeObject<DisplaySettings>(File.ReadAllText(fileName), settings);
                if (deserializedOptions == null)
                {
                    return new DisplaySettings();
                }

                DisplaySettings displaySettings = (DisplaySettings)deserializedOptions;
                foreach (ISettingMigration migration in _settingMigration.Where(x => x.VersionAfterMigration > displaySettings.Version))
                {
                    displaySettings = migration.ApplyMigration(displaySettings);
                    displaySettings.Version = migration.VersionAfterMigration;
                }

                return displaySettings;
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error(ex, "Error while loading display settingsView - default settingsView created");
                return new DisplaySettings();
            }
        }

        public bool TrySaveDisplaySettings(DisplaySettings displaySettings, string filePath)
        {
            try
            {
                displaySettings.Version = _settingMigration.Last().VersionAfterMigration;
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    Formatting = Formatting.Indented
                };
                File.WriteAllText(filePath, JsonConvert.SerializeObject(displaySettings, settings));
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error(ex, "Error while saving display settingsView.");
                return false;
            }

            return true;
        }
    }
}
