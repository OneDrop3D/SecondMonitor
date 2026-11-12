namespace SecondMonitor.Timing.Application.AutoDelete.Deleters
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels.Settings.ViewModel;

    public class AccSaveGameDeleter : AutoDeleterBase, IAutoDeleter
    {
        protected override string GetDefaultDirectory() => Path.Combine(DirectoryUtil.MyDocumentsPlaceHolder, "Assetto Corsa Competizione\\Savegames\\");

        protected override AutoDeleteSettingsViewModel GetAutoDeleteSettings(SimsAutoDeleteSettingsViewModel simsAutoDeleteSettingsViewModel) => simsAutoDeleteSettingsViewModel.AccSaveGameAutoDeleteSettings;

        protected override List<FileInfo> GetFilesToCheck(string directory)
        {
            return Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly).Select(x => new FileInfo(x)).ToList();
        }
    }
}
