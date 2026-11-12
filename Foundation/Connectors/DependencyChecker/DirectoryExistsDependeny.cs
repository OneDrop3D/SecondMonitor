namespace SecondMonitor.Foundation.Connectors.DependencyChecker
{
    using System.IO;

    using NLog;

    public class DirectoryExistsDependency : IDependency
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        public DirectoryExistsDependency(string directoryToCheck)
        {
            this.DirectoryToCheck = directoryToCheck;
        }

        public string DirectoryToCheck { get; }

        public virtual bool ExistsDependency(string basePath)
        {
            Logger.Info($"Checking Directory: {basePath}");
            return Directory.Exists(Path.Combine(basePath, this.DirectoryToCheck));
        }

        public string GetBatchCommand(string executablePath)
        {
            string dstPath = Path.Combine(executablePath, this.DirectoryToCheck);
            return "md \"" + dstPath + "\"";
        }
    }
}