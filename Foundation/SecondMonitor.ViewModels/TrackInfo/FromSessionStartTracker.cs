namespace SecondMonitor.ViewModels.TrackInfo
{
    using System;

    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.ViewModels.Settings.Model;

    public class FromSessionStartTracker : ITemperatureChangeTracker
    {
        private Temperature _referenceTemperature;
        private DateTime _referenceTemperatureSetTime;
        public TemperatureChangeKind ChangeKind => TemperatureChangeKind.FromSessionStart;
        public OptimalQuantity<Temperature> TemperatureChange { get; private set; } = new()
        {
            ActualQuantity = Temperature.FromCelsius(0),
            IdealQuantity = Temperature.FromCelsius(0),
            IdealQuantityWindow = Temperature.FromCelsius(0.75)
        };

        public void Update(Temperature trackedTemperature, SimulatorDataSet dataSet)
        {
            if (dataSet.SessionInfo.SessionPhase != SessionPhase.Green)
            {
                return;
            }

            if (_referenceTemperature == null)
            {
                _referenceTemperature = trackedTemperature;
                _referenceTemperatureSetTime = DateTime.Now;
            }
            else if ((DateTime.Now - _referenceTemperatureSetTime).TotalSeconds < 30)
            {
                _referenceTemperature = trackedTemperature;
            }

            TemperatureChange = new OptimalQuantity<Temperature>
            {
                ActualQuantity = trackedTemperature - _referenceTemperature,
                IdealQuantity = Temperature.FromCelsius(0),
                IdealQuantityWindow = Temperature.FromCelsius(0.75)
            };
        }

        public void Reset()
        {
            _referenceTemperature = null;
            TemperatureChange = new OptimalQuantity<Temperature>
            {
                ActualQuantity = Temperature.FromCelsius(0),
                IdealQuantity = Temperature.FromCelsius(0),
                IdealQuantityWindow = Temperature.FromCelsius(0.75)
            };
        }
    }
}
