namespace SecondMonitor.Contracts.WheelInformation
{
    using DataModel.Snapshot;

    public interface IIdealWheelQuantitiesFiller
    {
        void FillWheelIdealQuantities(SimulatorDataSet dataSet);
    }
}