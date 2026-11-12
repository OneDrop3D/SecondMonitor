namespace SecondMonitor.ViewModels.CarStatus.FuelStatus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.ViewModels.Formatters;

    public class ConsumptionMonitors
    {
        private readonly IConsumptionMonitorFactory _consumptionMonitorsFactory;
        private readonly IQuantityFormatter _quantityFormatter;
        private IList<IConsumptionMonitor> _consumptionMonitors;
        private IConsumptionMonitor _lowestEnduranceMonitor;

        public ConsumptionMonitors(IConsumptionMonitorFactory consumptionMonitorsFactory, IQuantityFormatter quantityFormatter)
        {
            _consumptionMonitorsFactory = consumptionMonitorsFactory;
            _quantityFormatter = quantityFormatter;
            FuelConsumptionMonitor = new FuelConsumptionMonitor();
            _consumptionMonitors = new List<IConsumptionMonitor>()
            {
                FuelConsumptionMonitor,
            };
        }

        public FuelConsumptionMonitor FuelConsumptionMonitor { get; private set; }

        public void Initialize(SimulatorDataSet dataSet)
        {
            _lowestEnduranceMonitor = null;
            _consumptionMonitors = _consumptionMonitorsFactory.Create(dataSet).ToList();
            FuelConsumptionMonitor = _consumptionMonitors.OfType<FuelConsumptionMonitor>().SingleOrDefault();
        }

        public void UpdateQuantityConsumption(SimulatorDataSet simulatorDataSet)
        {
            _consumptionMonitors.ForEach(x => x.UpdateQuantityConsumption(simulatorDataSet));
        }

        public void Reset()
        {
            _consumptionMonitors.ForEach(x => x.Reset());
        }

        public bool TryGet<T>(out T consumptionMonitor)
        {
            consumptionMonitor = _consumptionMonitors.OfType<T>().SingleOrDefault();
            return consumptionMonitor != null;
        }

        public string CreateActPerLapInfo()
        {
            return GetQuantitiesFormatted(x => x.ActPerLap);
        }

        public string CreateActPerMinuteInfo()
        {
            return GetQuantitiesFormatted(x => x.ActPerMinute);
        }

        public string CreateTotalPerLapInfo()
        {
            return GetQuantitiesFormatted(x => x.TotalPerLap);
        }

        public string CreateTotalPerMinuteInfo()
        {
            return GetQuantitiesFormatted(x => x.TotalPerMinute);
        }

        private string GetQuantitiesFormatted(Func<IConsumptionMonitor, IQuantity> quantitySelector)
        {
            return string.Join(", ", _consumptionMonitors.Select(x => FormatQuantity(quantitySelector(x))));
        }

        private string FormatQuantity(IQuantity quantity)
        {
            return _quantityFormatter.Format(quantity);
        }

        public string GetAllRemainingFuelLabel(SimulatorDataSet simulatorDataSet)
        {
            if (_consumptionMonitors.Count == 1)
            {
                return FormatQuantity(FuelConsumptionMonitor.GetMonitoredQuantity(simulatorDataSet));
            }

            string[] quantities = _consumptionMonitors.Select(x => new { ConsumptionMonitor = x, Quantity = x.GetMonitoredQuantity(simulatorDataSet) })
                .Select(x => x.ConsumptionMonitor == _lowestEnduranceMonitor ? FormatQuantity(x.Quantity) + "*" : FormatQuantity(x.Quantity)).ToArray();
            return string.Join(" / ", quantities);
        }

        public double? GetLowestLapsLeft(SimulatorDataSet dataSet)
        {
            var lowestConsumptionMonitor = _consumptionMonitors.Select(x => new { ConsumptionMonitor = x, LapsLeft = x.GetLapsLeft(dataSet) })
                .Where(x => x.LapsLeft.HasValue)
                .OrderBy(x => x.LapsLeft.Value)
                .FirstOrDefault();

            if (lowestConsumptionMonitor == null)
            {
                return null;
            }

            _lowestEnduranceMonitor = lowestConsumptionMonitor.ConsumptionMonitor;
            return lowestConsumptionMonitor.LapsLeft;
        }

        public TimeSpan? GetLowestTimeLeft(SimulatorDataSet dataSet)
        {
            return _consumptionMonitors.Select(x => x.GetTimeLeft(dataSet))
                .Where(x => x.HasValue)
                .OrderBy(x => x.Value)
                .FirstOrDefault();
        }

        public IEnumerable<FuelDeltaModel> GetFuelDeltaLabel(SimulatorDataSet dataSet, double laps)
        {
            return _consumptionMonitors.Select(x => new FuelDeltaModel("Δ" + x.Name + ": ", FormatQuantity(x.GetQuantityIn(laps, dataSet))));
        }

        public string GenerateRemark()
        {
            if (_consumptionMonitors.Count == 2 && TryGet(out VirtualEnergyConsumptionMonitor virtualEnergyConsumptionMonitor))
            {
                double ratio = (FuelConsumptionMonitor.TotalQuantityConsumptionInfo.ConsumedFuel.InLiters /
                                virtualEnergyConsumptionMonitor.TotalQuantityConsumptionInfo.ConsumedQuantity.InPercentage);
                return double.IsNaN(ratio) || double.IsInfinity(ratio) ? string.Empty : $"Ratio: {ratio.ToStringScalableDecimals()}";
            }

            return FuelConsumptionMonitor?.TotalQuantityConsumptionInfo != null && !double.IsNaN(FuelConsumptionMonitor.TotalQuantityConsumptionInfo.Consumption.RawValue)
                ? _quantityFormatter.Format(FuelConsumptionMonitor.TotalQuantityConsumptionInfo.Consumption) : string.Empty;
        }
    }
}
