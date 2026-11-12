namespace SecondMonitor.ViewModels.TrackInfo
{
    using System.Collections.Generic;
    using System.Diagnostics;

    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.ViewModels.Settings.Model;

    public class Last5MinutesTemperatureTracker : ITemperatureChangeTracker
    {
        private const int TemperaturesDeltaTimeMs = 5 * 60 * 1000;
        private const int DeltaSnapshotPeriod = 10 * 1000;
        private const int MaxSnapshots = (TemperaturesDeltaTimeMs / DeltaSnapshotPeriod) + 1;

        private readonly Queue<Temperature> _storedTemperatures = new Queue<Temperature>();
        private Stopwatch _storeTemperatureStopwatch;

        public TemperatureChangeKind ChangeKind => TemperatureChangeKind.Last5Minutes;

        public OptimalQuantity<Temperature> TemperatureChange { get; private set; } = new()
        {
            ActualQuantity = Temperature.FromCelsius(0),
            IdealQuantity = Temperature.FromCelsius(0),
            IdealQuantityWindow = Temperature.FromCelsius(1)
        };

        public void Update(Temperature trackedTemperature, SimulatorDataSet dataSet)
        {
            if (dataSet.SessionInfo.SessionPhase != SessionPhase.Green || _storeTemperatureStopwatch.ElapsedMilliseconds < DeltaSnapshotPeriod)
            {
                return;
            }

            _storeTemperatureStopwatch.Restart();

            _storedTemperatures.Enqueue(trackedTemperature);

            if (_storedTemperatures.Count > MaxSnapshots)
            {
                _storedTemperatures.Dequeue();
            }

            TemperatureChange = new OptimalQuantity<Temperature>
            {
                ActualQuantity = trackedTemperature - _storedTemperatures.Peek(),
                IdealQuantity = Temperature.FromCelsius(0),
                IdealQuantityWindow = Temperature.FromCelsius(1)
            };
        }

        public void Reset()
        {
            _storeTemperatureStopwatch = Stopwatch.StartNew();
            _storedTemperatures.Clear();
            TemperatureChange = new OptimalQuantity<Temperature>()
            {
                ActualQuantity = Temperature.FromCelsius(0),
                IdealQuantity = Temperature.FromCelsius(0),
                IdealQuantityWindow = Temperature.FromCelsius(1)
            };
        }
    }
}
