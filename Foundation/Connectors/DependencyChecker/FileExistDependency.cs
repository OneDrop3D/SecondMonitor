namespace SecondMonitor.Foundation.Connectors.DependencyChecker
{
    using System.IO;
    public class FileExistDependency : IDependency
    {
        public FileExistDependency(string fileToCheck, string fileToInstall)
        {
            this.FileToCheck = fileToCheck;
            this.FileToInstall = fileToInstall;
        }

        public string FileToCheck { get; }
        public string FileToInstall { get; }

        public virtual bool ExistsDependency(string basePath)
        {
            return File.Exists(Path.Combine(basePath, this.FileToCheck));
        }

        public string GetBatchCommand(string executablePath)
        {
            string sourcePath = Path.Combine(Directory.GetCurrentDirectory(), this.FileToInstall);
            string dstPath = Path.Combine(executablePath, this.FileToCheck);
            return "copy \"" + sourcePath + "\" \"" + dstPath + "\"";
        }
    }
}