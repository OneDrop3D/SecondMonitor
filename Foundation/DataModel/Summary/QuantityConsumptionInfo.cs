namespace SecondMonitor.DataModel.Summary
{
    using System;

    using NLog;

    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;

    public abstract class QuantityConsumptionInfo<T, TPerDistance, TImpl> : IQuantityConsumptionInfo<T, TPerDistance> where T : IQuantity
    where TImpl : IQuantityConsumptionInfo<T, TPerDistance>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private bool _logInvalidConsumption;

        protected QuantityConsumptionInfo()
        {
            _logInvalidConsumption = true;
            ElapsedTime = TimeSpan.Zero;
            TraveledDistance = Distance.ZeroDistance;
        }

        protected QuantityConsumptionInfo(T consumedQuantity, TimeSpan elapsedTime, double traveledDistance)
            : this(consumedQuantity, elapsedTime, Distance.FromMeters(traveledDistance))
        {
        }

        protected QuantityConsumptionInfo(T consumedQuantity, TimeSpan elapsedTime, Distance traveledDistance)
        {
            ConsumedQuantity = consumedQuantity;
            ElapsedTime = elapsedTime;
            TraveledDistance = traveledDistance;
        }

        public abstract TPerDistance Consumption { get; }

        public T ConsumedQuantity { get; protected set; }

        public TimeSpan ElapsedTime { get;  protected set; }

        public Distance TraveledDistance { get;  protected set; }

        public T GetAveragePerMinute()
        {
            return CreateNew(ConsumedQuantity.RawValue / ElapsedTime.TotalMinutes);
        }

        public T GetAveragePerDistance(Distance distance) => GetAveragePerDistance(distance.InMeters);

        public T GetAveragePerDistance(double distance)
        {
            double distanceCoef = TraveledDistance.InMeters / distance;
            return CreateNew(ConsumedQuantity.RawValue / distanceCoef);
        }

        public virtual bool IsConsumptionValid(SimulatorDataSet dataSet)
        {
            if (TraveledDistance.InMeters < 0)
            {
                if (_logInvalidConsumption)
                {
                    Logger.Info("Fuel Consumption - Discarded Consumption - negative distance traveled");
                    _logInvalidConsumption = false;
                }

                return false;
            }

            if (TraveledDistance > dataSet.SessionInfo.TrackInfo.LayoutLength * 0.8)
            {
                if (_logInvalidConsumption)
                {
                    Logger.Info("Fuel Consumption - Discarded Consumption - moved over 80% of track");
                    _logInvalidConsumption = false;
                }

                return false;
            }

            if (TraveledDistance.InMeters > 500)
            {
                if (_logInvalidConsumption)
                {
                    Logger.Info("Fuel Consumption - Discarded Consumption - moved over 500m");
                    _logInvalidConsumption = false;
                }

                return false;
            }

            if (ElapsedTime <= TimeSpan.Zero)
            {
                return false;
            }

            if (ElapsedTime > TimeSpan.FromSeconds(20))
            {
                return false;
            }

            _logInvalidConsumption = true;
            return true;
        }

        public abstract TImpl AddConsumption(TImpl previousConsumption);
        
        protected abstract T CreateNew(double rawValue);
    }
}
