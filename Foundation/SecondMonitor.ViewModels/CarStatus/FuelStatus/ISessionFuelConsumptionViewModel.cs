namespace SecondMonitor.ViewModels.CarStatus.FuelStatus
{
    using DataModel.BasicProperties;
    using DataModel.Summary;
    using DataModel.Summary.FuelConsumption;

    public interface ISessionFuelConsumptionViewModel : IViewModel<SessionFuelConsumptionDto>
    {
         string TrackName { get; set; }
         Distance LapDistance { get; set; }
         string SessionType { get; set; }
         FuelConsumptionInfo FuelConsumption { get; set; }
         Volume AvgPerMinute { get; set; }
         Volume AvgPerLap { get; set; }
         bool IsChecked { get; set; }
         bool IsCheckedVisible { get; }
    }
}