namespace SecondMonitor.DataModel.Summary
{
    using System;
    using BasicProperties;
    using BasicProperties.FuelConsumption;

    using Snapshot;

    public class FuelConsumptionInfo : QuantityConsumptionInfo<Volume, FuelPerDistance, FuelConsumptionInfo>
    {
        public FuelConsumptionInfo()
        {
            ConsumedQuantity = Volume.FromLiters(0);
        }

        public FuelConsumptionInfo(Volume consumedFuel, TimeSpan elapsedTime, double traveledDistance)
        : this(consumedFuel, elapsedTime, Distance.FromMeters(traveledDistance))
        {
        }

        public FuelConsumptionInfo(Volume consumedFuel, TimeSpan elapsedTime, Distance traveledDistance) : base(consumedFuel, elapsedTime, traveledDistance)
        {
        }

        public Volume ConsumedFuel => ConsumedQuantity;

        public override FuelPerDistance Consumption => new(ConsumedFuel, Distance.FromMeters(TraveledDistance.InMeters));

        public override FuelConsumptionInfo AddConsumption(FuelConsumptionInfo previousConsumption)
        {
            TimeSpan newElapsedTime = ElapsedTime + previousConsumption.ElapsedTime;
            return new FuelConsumptionInfo(ConsumedFuel + previousConsumption.ConsumedFuel, newElapsedTime, TraveledDistance.InMeters + previousConsumption.TraveledDistance.InMeters);
        }

        public override bool IsConsumptionValid(SimulatorDataSet dataSet)
        {
            if (!base.IsConsumptionValid(dataSet))
            {
                return false;
            }

            // Can be negative, for electric car regen
            if (ConsumedFuel.InLiters < -3)
            {
                return false;
            }

            // Consumed 0.5l while the car is stationary - doesn't add up. Most likely the car placed i.e at Assetto Corsa time trial start
            if (dataSet.PlayerInfo.Speed.InKph < 1 && ConsumedFuel.InLiters > 0.5)
            {
                return false;
            }

            return true;
        }

        public static FuelConsumptionInfo CreateConsumption(FuelStatusSnapshot fromSnapshot, FuelStatusSnapshot toSnapshot)
        {
            return new FuelConsumptionInfo(fromSnapshot.FuelLevel - toSnapshot.FuelLevel, toSnapshot.SessionTime - fromSnapshot.SessionTime, toSnapshot.TotalDistance - fromSnapshot.TotalDistance);
        }

        protected override Volume CreateNew(double rawValue)
        {
            return Volume.FromLiters(rawValue);
        }
    }
}