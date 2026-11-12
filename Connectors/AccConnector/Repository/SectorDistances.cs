namespace SecondMonitor.AccConnector.Repository
{
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;

    internal class SectorDistances
    {
        private const int MaxSamples = 10;
        private double? _averageSectorOneDistance;
        private double? _averageSectorTwoDistance;

        public SectorDistances()
        {
            SectorOneSamples = new List<double>(MaxSamples);
            SectorTwoSamples = new List<double>(MaxSamples);
        }

        public List<double> SectorOneSamples { get; set; }
        public List<double> SectorTwoSamples { get; set; }

        public double? SectorOneDistance
        {
            get
            {
                double? distance;
                if (SectorOneSamples.Count == 0)
                {
                    distance = null;
                }
                else if (_averageSectorOneDistance.HasValue)
                {
                    distance = _averageSectorOneDistance;
                }
                else
                {
                    distance = SectorOneSamples.Average();
                    _averageSectorOneDistance = distance;
                }

                return distance;
            }
        }

        public double? SectorTwoDistance
        {
            get
            {
                double? distance;
                if (SectorTwoSamples.Count == 0)
                {
                    distance = null;
                }
                else if (_averageSectorTwoDistance.HasValue)
                {
                    distance = _averageSectorTwoDistance;
                }
                else
                {
                    distance = SectorTwoSamples.Average();
                    _averageSectorTwoDistance = distance;
                }

                return distance;
            }
        }

        [JsonIgnore]
        public bool HasValidDistances => SectorOneSamples.Count > 0 && SectorTwoSamples.Count > 0;

        public bool TryAddSector1Sample(double sectorDistance)
        {
            return TryAddSectorSample(sectorDistance, SectorOneSamples, ref _averageSectorOneDistance);
        }

        public bool TryAddSector2Sample(double sectorDistance)
        {
            return TryAddSectorSample(sectorDistance, SectorTwoSamples, ref _averageSectorTwoDistance);
        }

        private bool TryAddSectorSample(double sectorDistance, List<double> samples, ref double? averageSectorDistance)
        {
            if (samples.Count >= MaxSamples)
            {
                return false;
            }

            samples.Add(sectorDistance);
            averageSectorDistance = null;

            return true;
        }
    }
}