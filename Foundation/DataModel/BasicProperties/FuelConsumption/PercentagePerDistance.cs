namespace SecondMonitor.DataModel.BasicProperties.FuelConsumption
{
    using System;

    using Newtonsoft.Json;

    public class PercentagePerDistance : IQuantity
    {
        public PercentagePerDistance(Percentage consumed, Distance distance)
        {
            Percentage = Percentage.FromRatio(consumed.InRatio);
            Distance = Distance.FromMeters(distance.InMeters);
        }

        [JsonIgnore]
        public Percentage Percentage { get; }

        [JsonIgnore]
        public Distance Distance { get; }

        [JsonIgnore]
        public bool IsZero => Distance.IsZero;

        [JsonIgnore]
        public double RawValue => InPercentagePer100Km.InRatio;

        public Percentage InPercentagePer100Km
        {
            get
            {
                double normalizedDistance = Distance.InKilometers / 100;
                return Percentage.FromRatio(Percentage.InRatio / normalizedDistance);
            }
        }

        public bool Equals(IQuantity other, IQuantity tolerance)
        {
            if (other.GetType() != GetType())
            {
                return false;
            }

            return Math.Abs(RawValue - other.RawValue) < tolerance.RawValue;
        }
    }
}