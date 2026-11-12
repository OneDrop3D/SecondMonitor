namespace SecondMonitor.DataModel.BasicProperties
{
    public interface IOptimalQuantity
    {
        double ActualQuantityRawValue { get; }

        double IdealQuantityRawValue { get; }

        double IdealQuantityWindowRawValue { get; }
    }
}
