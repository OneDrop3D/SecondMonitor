namespace SecondMonitor.ViewModels.TrackInfo
{
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.ViewModels.Settings.Model;

    public interface ITemperatureChangeTracker
    {
        TemperatureChangeKind ChangeKind { get; }

        OptimalQuantity<Temperature> TemperatureChange { get; }

        void Update(Temperature trackedTemperature, SimulatorDataSet dataSet);

        void Reset();
    }
}
