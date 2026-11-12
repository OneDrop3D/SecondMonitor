namespace SecondMonitor.SimdataManagement.SimSettings.IdealPressure
{
    using DataModel.Snapshot.Systems;

    public interface IIdealPressureByTemperatureCalculator
    {
        void FillIdealPressure(WheelInfo wheelInfo);
    }
}