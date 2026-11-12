namespace SecondMonitor.DataModel.Summary
{
    using System;

    using SecondMonitor.DataModel.BasicProperties;

    public interface IQuantityConsumptionInfo<out T, out TPerDistance> where T : IQuantity
    {
        public abstract TPerDistance Consumption { get; }

        public T ConsumedQuantity { get; }

        public TimeSpan ElapsedTime { get;  }

        public Distance TraveledDistance { get; }
    }
}
