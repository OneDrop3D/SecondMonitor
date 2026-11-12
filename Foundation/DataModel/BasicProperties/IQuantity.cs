namespace SecondMonitor.DataModel.BasicProperties
{
    public interface IQuantity
    {
        bool IsZero { get; }

        double RawValue { get; }

        bool Equals(IQuantity other, IQuantity tolerance);
    }
}