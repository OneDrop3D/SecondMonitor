namespace SecondMonitor.ViewModels.CarStatus.FuelStatus
{
    public class FuelDeltaModel
    {
        public FuelDeltaModel(string name, string formattedValue)
        {
            Name = name;
            FormattedValue = formattedValue;
        }

        public string Name { get; }
        
        public string FormattedValue { get; }
    }
}
