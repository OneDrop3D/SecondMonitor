namespace SecondMonitor.Timing.Application.AutoDelete.Deleters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using NLog;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels.Settings.ViewModel;

    public abstract class AutoDeleterBase : IAutoDeleter
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        protected AutoDeleteSettingsViewModel AutoDeleteSettingsViewModel { get; private set; }

        protected abstract string GetDefaultDirectory();

        protected abstract AutoDeleteSettingsViewModel GetAutoDeleteSettings(SimsAutoDeleteSettingsViewModel simsAutoDeleteSettingsViewModel);

        public void CheckSettingsAndSetDefaults(SimsAutoDeleteSettingsViewModel autoDeleteSettingsViewModel)
        {
            AutoDeleteSettingsViewModel = GetAutoDeleteSettings(autoDeleteSettingsViewModel);
            if (string.IsNullOrWhiteSpace(AutoDeleteSettingsViewModel.Directory) || !Directory.Exists(AutoDeleteSettingsViewModel.Directory))
            {
                AutoDeleteSettingsViewModel.Directory = DirectoryUtil.PopulateSpecialDirPlaceholders(GetDefaultDirectory());
            }
        }

        protected abstract List<FileInfo> GetFilesToCheck(string directory);

        public void PerformAutoDelete()
        {
            if (string.IsNullOrWhiteSpace(AutoDeleteSettingsViewModel.Directory) || !Directory.Exists(AutoDeleteSettingsViewModel.Directory))
            {
                return;
            }

            List<FileInfo> files = GetFilesToCheck(AutoDeleteSettingsViewModel.Directory).OrderByDescending(x => x.CreationTimeUtc).ToList();
            List<FileInfo> filesToDelete; 
            if (AutoDeleteSettingsViewModel.IsDeleteByDaysEnabled)
            {
                filesToDelete = files.Where(x => (DateTime.UtcNow - x.CreationTimeUtc).TotalDays > AutoDeleteSettingsViewModel.MaxDays).ToList();
                filesToDelete.ForEach(x =>
                {
                    files.Remove(x);
                    DeleteFile(x);
                });
            }

            if (!AutoDeleteSettingsViewModel.IsDeleteBySizeEnabled)
            {
                return;
            }

            long sizeSoFar = 0;
            int i = 0;
            long maxSize = AutoDeleteSettingsViewModel.MaxMegabytes * 1024L * 1024L;
            for (i = 0; i < files.Count && sizeSoFar < maxSize; i++)
            {
                sizeSoFar += files[i].Length;
            }

            filesToDelete = files.Skip(i).ToList();
            filesToDelete.ForEach(DeleteFile);
        }

        private void DeleteFile(FileInfo fileInfo)
        {
            logger.Info($"AutoDeleter Removing File {fileInfo.FullName}, Size: {fileInfo.Length}, LastWriteTimeUtc: {fileInfo.LastWriteTimeUtc}");
            File.Delete(fileInfo.FullName);
        }
    }
}
