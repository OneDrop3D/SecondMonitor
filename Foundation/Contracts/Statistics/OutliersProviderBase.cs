namespace SecondMonitor.Contracts.Statistics
{
    using System.Collections.Generic;
    using System.Linq;

    public abstract class OutliersProviderBase<T>
    {
        protected abstract double GetValue(T element);

        public IList<T> GetOutliers(IList<T> elements)
        {
            if (elements.Count <= 3)
            {
                return new List<T>();
            }

            List<(T Element, double Value)> elementsWithValueOrdered = elements.Select(x => (Element: x, Value: GetValue(x))).OrderBy(x => x.Value).ToList();
            int medianIndex = elements.Count / 2;
            List<(T Element, double Value)> q1List = elementsWithValueOrdered.Take(medianIndex).ToList();
            List<(T Element, double Value)> q3List = elementsWithValueOrdered.Skip(medianIndex + 1).ToList();

            (T Element, double Value) q1 = q1List[q1List.Count / 2];
            (T Element, double Value) q3 = q3List[q3List.Count / 2];

            double iqr = q3.Value - q1.Value;
            double upperFence = q3.Value + (iqr * 1.5);
            double lowerFence = q1.Value - (iqr * 1.5);

            return elementsWithValueOrdered.Where(x => x.Value < lowerFence || x.Value > upperFence).Select(x => x.Element).ToList();
        }
        
        public IList<T> GetNonOutliers(IList<T> elements)
        {
            if (elements.Count <= 3)
            {
                return new List<T>();
            }

            List<(T Element, double Value)> elementsWithValueOrdered = elements.Select(x => (Element: x, Value: GetValue(x))).OrderBy(x => x.Value).ToList();
            int medianIndex = elements.Count / 2;
            List<(T Element, double Value)> q1List = elementsWithValueOrdered.Take(medianIndex).ToList();
            List<(T Element, double Value)> q3List = elementsWithValueOrdered.Skip(medianIndex + 1).ToList();

            (T Element, double Value) q1 = q1List[q1List.Count / 2];
            (T Element, double Value) q3 = q3List[q3List.Count / 2];

            double iqr = q3.Value - q1.Value;
            double upperFence = q3.Value + (iqr * 1.5);
            double lowerFence = q1.Value - (iqr * 1.5);

            return elementsWithValueOrdered.Where(x => x.Value >= lowerFence && x.Value <= upperFence).Select(x => x.Element).ToList();
        }
    }
}
