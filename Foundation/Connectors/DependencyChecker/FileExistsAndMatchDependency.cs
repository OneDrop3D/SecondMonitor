namespace SecondMonitor.Foundation.Connectors.DependencyChecker
{
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;

    using NLog;

    public class FileExistsAndMatchDependency : FileExistDependency
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        public FileExistsAndMatchDependency(string fileToCheck, string fileToInstall)
            : base(fileToCheck, fileToInstall)
        {
        }

        public override bool ExistsDependency(string basePath)
        {
            bool fileExists = base.ExistsDependency(basePath);
            if (!fileExists)
            {
                return false;
            }

            string fullPathToFileToCheck = Path.Combine(basePath, this.FileToCheck);
            string fullPathToFileToInstall = Path.Combine(Directory.GetCurrentDirectory(), this.FileToInstall);

            Logger.Info($"Checking for file existence File to Check: '{fullPathToFileToCheck}', File to Install '{fullPathToFileToInstall}'");

            using (MD5 fileToCheckMd5 = MD5.Create())
            using (MD5 fileToInstallMd5 = MD5.Create())
            using (FileStream fileToCheckStream = File.OpenRead(fullPathToFileToCheck))
            using (FileStream fileToInstallStream = File.OpenRead(fullPathToFileToInstall))
            {
                byte[] fileToCheckHash = fileToCheckMd5.ComputeHash(fileToCheckStream);
                byte[] fileToInstallHash = fileToInstallMd5.ComputeHash(fileToInstallStream);
                Logger.Info($"File Exists, File to Check Hash: '{string.Concat(fileToCheckHash.Select(x => x.ToString("X2")))}', File to Install Hash: '{string.Concat(fileToInstallHash.Select(x => x.ToString("X2")))}' ");
                return fileToCheckHash.SequenceEqual(fileToInstallHash);
            }
        }
    }
}