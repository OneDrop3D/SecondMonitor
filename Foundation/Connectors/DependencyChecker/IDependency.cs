namespace SecondMonitor.Foundation.Connectors.DependencyChecker
{
    public interface IDependency
    {
        bool ExistsDependency(string executablePath);

        string GetBatchCommand(string executablePath);
    }
}