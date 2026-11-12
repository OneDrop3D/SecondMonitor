namespace SecondMonitor.Timing.Application.AutoDelete.Deleters
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using SecondMonitor.ViewModels.Settings.ViewModel;

    public class LmuReplayDeleter : AutoDeleterBase, IAutoDeleter
    {
        protected override string GetDefaultDirectory() => "c:\\Program Files (x86)\\Steam\\steamapps\\common\\Le Mans Ultimate\\UserData\\Replays\\";

        protected override AutoDeleteSettingsViewModel GetAutoDeleteSettings(SimsAutoDeleteSettingsViewModel simsAutoDeleteSettingsViewModel)
        {
            return simsAutoDeleteSettingsViewModel.LmUReplayAutoDeleteSettings;
        }

        protected override List<FileInfo> GetFilesToCheck(string directory)
        {
            return Directory.GetFiles(directory, "*.Vcr", SearchOption.TopDirectoryOnly).Select(x => new FileInfo(x)).ToList();
        }
    }
}
