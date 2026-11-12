namespace SecondMonitor.DataModel.BasicProperties.FuelConsumption
{
    using System;
    using System.ComponentModel;
    using Newtonsoft.Json;

    public class FuelPerDistance : IQuantity
    {
        public FuelPerDistance(Volume consumedFuel, Distance distance)
        {
            ConsumedFuel = Volume.FromLiters(consumedFuel.InLiters);
            Distance = Distance.FromMeters(distance.InMeters);
        }

        [JsonIgnore]
        public Volume ConsumedFuel { get; }

        [JsonIgnore]
        public Distance Distance { get; }

        [JsonIgnore]
        public bool IsZero => Distance.IsZero;

        [JsonIgnore]
        public double RawValue => InVolumePer100Km.InLiters;

        public Volume InVolumePer100Km
        {
            get
            {
                double normalizedDistance = Distance.InKilometers / 100;
                return Volume.FromLiters(ConsumedFuel.InLiters / normalizedDistance);
            }
        }

        public Distance InDistancePerGallon => Distance.FromMeters(Distance.InMeters / ConsumedFuel.InUsGallons);

        public static string GetUnitsSymbol(FuelPerDistanceUnits fuelPerDistanceUnits)
        {
            return fuelPerDistanceUnits switch
            {
                FuelPerDistanceUnits.LitersPerHundredKm => "l/100km",
                FuelPerDistanceUnits.MilesPerGallon => "mpg",
                _ => throw new ArgumentException($"Unknown units {fuelPerDistanceUnits}")
            };
        }

        public bool Equals(IQuantity other, IQuantity tolerance)
        {
            if (other.GetType() != GetType())
            {
                return false;
            }

            return Math.Abs(RawValue - other.RawValue) < tolerance.RawValue;
        }

        public double GetConsumption(FuelPerDistanceUnits fuelPerDistanceUnits)
        {
            return fuelPerDistanceUnits switch
            {
                FuelPerDistanceUnits.LitersPerHundredKm => InVolumePer100Km.InLiters,
                FuelPerDistanceUnits.MilesPerGallon => InDistancePerGallon.InMiles,
                _ => throw new InvalidEnumArgumentException($"Unknown fuel consumption unit: {fuelPerDistanceUnits}")
            };
        }
    }
}