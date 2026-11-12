namespace SecondMonitor.Telemetry.TelemetryApplication.Checks.Evaluators.TyreTemperatures
{
    using System;
    using System.Linq;
    using DataModel.Snapshot.Systems;
    using Statistics;

    public class TyreTemperaturesStatistics
    {
        public TyreTemperaturesStatistics(WheelInfo wheel)
        {
            InitializeStatistics(wheel);
        }

        public QuantityWithLimitsStatistics LeftPortionStatistics { get; private set; }
        public QuantityWithLimitsStatistics CenterPortionStatistics { get; private set; }
        public QuantityWithLimitsStatistics RightPortionStatistics { get; private set; }
        public QuantityWithLimitsStatistics CorePortionStatistics { get; private set; }
        public QuantityWithLimitsStatistics OuterInnerDiffStatistics { get; private set; }

        private void InitializeStatistics(WheelInfo wheel)
        {
            LeftPortionStatistics = new QuantityWithLimitsStatistics(wheel.LeftTyreTemp.IdealQuantity.InCelsius - (wheel.LeftTyreTemp.IdealQuantityWindow.InCelsius * 1.5),
                wheel.LeftTyreTemp.IdealQuantity.InCelsius + (wheel.LeftTyreTemp.IdealQuantityWindow.InCelsius * 1.5), wheel.LeftTyreTemp.IdealQuantity.InCelsius);

            RightPortionStatistics = new QuantityWithLimitsStatistics(wheel.RightTyreTemp.IdealQuantity.InCelsius - (wheel.RightTyreTemp.IdealQuantityWindow.InCelsius * 1.5),
                wheel.RightTyreTemp.IdealQuantity.InCelsius + (wheel.RightTyreTemp.IdealQuantityWindow.InCelsius * 1.5), wheel.RightTyreTemp.IdealQuantity.InCelsius);

            CenterPortionStatistics = new QuantityWithLimitsStatistics(wheel.CenterTyreTemp.IdealQuantity.InCelsius - (wheel.CenterTyreTemp.IdealQuantityWindow.InCelsius * 1.5),
                wheel.CenterTyreTemp.IdealQuantity.InCelsius + (wheel.CenterTyreTemp.IdealQuantityWindow.InCelsius * 1.5), wheel.CenterTyreTemp.IdealQuantity.InCelsius);

            CorePortionStatistics = new QuantityWithLimitsStatistics(wheel.TyreCoreTemperature.IdealQuantity.InCelsius - (wheel.TyreCoreTemperature.IdealQuantityWindow.InCelsius * 1.5),
                wheel.TyreCoreTemperature.IdealQuantity.InCelsius + (wheel.TyreCoreTemperature.IdealQuantityWindow.InCelsius * 1.5), wheel.TyreCoreTemperature.IdealQuantity.InCelsius);

            OuterInnerDiffStatistics = new QuantityWithLimitsStatistics(0, 12, 8);
        }

        public void ProcessDataPoint(WheelInfo wheel)
        {
            LeftPortionStatistics.ProcessDataPoint(wheel.LeftTyreTemp.ActualQuantity.InCelsius);
            CenterPortionStatistics.ProcessDataPoint(wheel.CenterTyreTemp.ActualQuantity.InCelsius);
            RightPortionStatistics.ProcessDataPoint(wheel.RightTyreTemp.ActualQuantity.InCelsius);
            CorePortionStatistics.ProcessDataPoint(wheel.TyreCoreTemperature.ActualQuantity.InCelsius);

            OuterInnerDiffStatistics.ProcessDataPoint(Math.Abs(wheel.LeftTyreTemp.ActualQuantity.InCelsius - wheel.RightTyreTemp.ActualQuantity.InCelsius));
        }

        public QuantityWithLimitsStatistics GetHottestPart()
        {
            return new[] { LeftPortionStatistics, RightPortionStatistics, CenterPortionStatistics, CorePortionStatistics }
                .OrderByDescending(x => x.PointsOverLimit).First();
        }

        public QuantityWithLimitsStatistics GetColdestPart()
        {
            return new[] { LeftPortionStatistics, RightPortionStatistics, CenterPortionStatistics, CorePortionStatistics }
                .OrderBy(x => x.PointsOverLimit).First();
        }
    }
}