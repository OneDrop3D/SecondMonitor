namespace SecondMonitor.DataModel.Summary
{
    using System;

    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.BasicProperties.FuelConsumption;
    using SecondMonitor.DataModel.Snapshot;

    public class VirtualTankConsumptionInfo : QuantityConsumptionInfo<Percentage, PercentagePerDistance, VirtualTankConsumptionInfo>
    {
        public VirtualTankConsumptionInfo()
        {
            ConsumedQuantity = Percentage.FromRatio(0);
        }

        public VirtualTankConsumptionInfo(Percentage consumedFuel, TimeSpan elapsedTime, double traveledDistance)
            : this(consumedFuel, elapsedTime, Distance.FromMeters(traveledDistance))
        {
        }

        public VirtualTankConsumptionInfo(Percentage consumedFuel, TimeSpan elapsedTime, Distance traveledDistance) : base(consumedFuel, elapsedTime, traveledDistance)
        {
        }

        public override PercentagePerDistance Consumption => new PercentagePerDistance(ConsumedQuantity, TraveledDistance);

        public override bool IsConsumptionValid(SimulatorDataSet dataSet)
        {
            if (!base.IsConsumptionValid(dataSet))
            {
                return false;
            }

            if (!dataSet.PlayerInfo.CarInfo.FuelSystemInfo.HasVirtualTank)
            {
                return false;
            }

            // Can be negative, for electric car regen
            if (ConsumedQuantity.RawValue < 0)
            {
                return false;
            }

            // Consumed 0.5l while the car is stationary - doesn't add up. Most likely the car placed i.e at Assetto Corsa time trial start
            if (dataSet.PlayerInfo.Speed.InKph < 1 && ConsumedQuantity.RawValue > 0.05)
            {
                return false;
            }

            return true;
        }

        public override VirtualTankConsumptionInfo AddConsumption(VirtualTankConsumptionInfo previousConsumption)
        {
            TimeSpan newElapsedTime = ElapsedTime + previousConsumption.ElapsedTime;
            return new VirtualTankConsumptionInfo(ConsumedQuantity + previousConsumption.ConsumedQuantity, newElapsedTime, TraveledDistance.InMeters + previousConsumption.TraveledDistance.InMeters);
        }

        public static VirtualTankConsumptionInfo CreateConsumption(VirtualTankStatusSnapshot fromSnapshot, VirtualTankStatusSnapshot toSnapshot)
        {
            return new VirtualTankConsumptionInfo(fromSnapshot.VirtualTank - toSnapshot.VirtualTank, toSnapshot.SessionTime - fromSnapshot.SessionTime, toSnapshot.TotalDistance - fromSnapshot.TotalDistance);
        }

        protected override Percentage CreateNew(double rawValue)
        {
            return Percentage.FromRatio(rawValue);
        }
    }
}
