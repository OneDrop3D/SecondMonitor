namespace SecondMonitor.Contracts.Statistics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class StandardDeviationCalculator
    {
        public static StandardDeviationResult<double> ComputeDeviation(ICollection<double> values)
        {
            double mean = values.Sum() / values.Count;

            var squaresQuery = values.Select(x => (x - mean) * (x - mean));
            double sumOfSquares = squaresQuery.Sum();

            double stdDeviation = Math.Sqrt(sumOfSquares / values.Count());

            return new StandardDeviationResult<double>(mean, stdDeviation);
        }

        public static double Median(List<double> numbers)
        {
            if (numbers.Count == 0)
            {
                return 0;
            }

            numbers = numbers.OrderBy(n => n).ToList();

            var halfIndex = numbers.Count() / 2;

            if (numbers.Count() % 2 == 0)
            {
                return (numbers[halfIndex] + numbers[halfIndex - 1]) / 2.0;
            }

            return numbers[halfIndex];
        }
    }
}
