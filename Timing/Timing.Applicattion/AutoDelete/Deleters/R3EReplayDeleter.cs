namespace SecondMonitor.Timing.Application.AutoDelete.Deleters
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels.Settings.ViewModel;

    public class R3EReplayDeleter : AutoDeleterBase, IAutoDeleter
    {
        protected override string GetDefaultDirectory() => Path.Combine(DirectoryUtil.MyGamesPlaceHolder, "SimBin");

        protected override AutoDeleteSettingsViewModel GetAutoDeleteSettings(SimsAutoDeleteSettingsViewModel simsAutoDeleteSettingsViewModel)
        {
            return simsAutoDeleteSettingsViewModel.R3EReplayAutoDeleteSettings;
        }

        protected override List<FileInfo> GetFilesToCheck(string directory)
        {
            string[] replayDirectories = Directory.GetDirectories(directory, "ReplayData", SearchOption.AllDirectories);
            return replayDirectories.SelectMany(x => Directory.GetFiles(x, "*.Vcr", SearchOption.TopDirectoryOnly)).Select(x => new FileInfo(x)).ToList();
        }
    }
}
