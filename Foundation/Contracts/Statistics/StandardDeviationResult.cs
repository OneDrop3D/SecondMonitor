namespace SecondMonitor.Contracts.Statistics
{
    public class StandardDeviationResult<T>
    {
        public StandardDeviationResult(T meanValue, T standardDeviation)
        {
            MeanValue = meanValue;
            StandardDeviation = standardDeviation;
        }

        public T MeanValue { get; }

        public T StandardDeviation { get; }
    }
}