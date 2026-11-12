namespace SecondMonitor.ViewModels.CarStatus.FuelStatus
{
    public class FuelDeltaViewModel : AbstractViewModel<FuelDeltaModel>
    {
        private string _name;
        private string _formattedValue;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string FormattedValue
        {
            get => _formattedValue;
            set => SetProperty(ref _formattedValue, value);
        }

        protected override void ApplyModel(FuelDeltaModel model)
        {
            Name = model.Name;
            FormattedValue = model.FormattedValue;
        }

        public override FuelDeltaModel SaveToNewModel()
        {
            return OriginalModel;
        }
    }
}
