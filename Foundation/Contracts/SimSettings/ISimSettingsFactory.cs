namespace SecondMonitor.Contracts.SimSettings
{
    public interface ISimSettingsFactory
    {
        ISimSettings Create(string simulatorName);
    }
}