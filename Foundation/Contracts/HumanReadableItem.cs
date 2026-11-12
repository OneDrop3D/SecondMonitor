namespace SecondMonitor.Contracts
{
    public class HumanReadableItem<T>
    {
        public HumanReadableItem(T value, string humanReadableValue)
        {
            Value = value;
            HumanReadableValue = humanReadableValue;
        }

        public T Value { get; }

        public string HumanReadableValue { get; }

        public override string ToString()
        {
            return HumanReadableValue;
        }
    }
}
