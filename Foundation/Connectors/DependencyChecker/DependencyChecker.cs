namespace SecondMonitor.Foundation.Connectors.DependencyChecker
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    public class DependencyChecker
    {
        public DependencyChecker(IReadOnlyCollection<IDependency> dependencies, Func<bool> shouldInstallDependency)
        {
            this.Dependencies = dependencies;
            this.ShouldInstallDependency = shouldInstallDependency;
        }

        public Func<bool> ShouldInstallDependency { get; set; }

        public bool Checked { get; private set; }

        public IReadOnlyCollection<IDependency> Dependencies { get; }

        public void CheckAndInstallDependencies(string basePath)
        {
            Action installAction = this.CheckAndReturnInstallDependenciesAction(basePath);

            installAction?.Invoke();
        }

        public Action CheckAndReturnInstallDependenciesAction(string basePath)
        {
            IReadOnlyCollection<IDependency> missingDependencies =
                this.Dependencies.Where(x => !x.ExistsDependency(basePath)).ToList();
            this.Checked = true;

            if (!missingDependencies.Any() || !this.ShouldInstallDependency())
            {
                return null;
            }

            string filePath = this.CreateBatchFile(missingDependencies, basePath);
            return () => StartWithElevated(filePath);
        }

        private static void StartWithElevated(string filePath)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            startInfo.FileName = filePath;
            startInfo.Verb = "runas";
            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = Directory.GetCurrentDirectory();
            process.StartInfo = startInfo;
            process.Start();
        }

        private string CreateBatchFile(IReadOnlyCollection<IDependency> dependencies, string basePath)
        {
            string fileName = Path.Combine(Path.GetTempPath(), "installSecondMonitorDependencies.cmd");
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            StringBuilder cmdFileContent = new StringBuilder();
            foreach (IDependency dependency in dependencies)
            {
                cmdFileContent.Append(dependency.GetBatchCommand(basePath) + "\n");
            }

            File.WriteAllText(fileName, cmdFileContent.ToString());
            return fileName;
        }
    }
}