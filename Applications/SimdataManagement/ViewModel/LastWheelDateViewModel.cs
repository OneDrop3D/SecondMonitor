namespace SecondMonitor.SimdataManagement.ViewModel
{
    using System;
    using DataModel.BasicProperties;
    using DataModel.Snapshot.Systems;
    using ViewModels;
    using ViewModels.Settings;

    public class LastWheelDateViewModel : AbstractViewModel<WheelInfo>
    {
        private readonly ISettingsProvider _settingsProvider;
        private string _wheelName;
        private string _distanceUnits;
        private double _suspensionTravel;
        private double _rideHeight;

        public LastWheelDateViewModel(ISettingsProvider settingsProvider)
        {
            _settingsProvider = settingsProvider;
        }

        public string DistanceUnits
        {
            get => _distanceUnits;
            set => SetProperty(ref _distanceUnits, value);
        }

        public double SuspensionTravel
        {
            get => _suspensionTravel;
            set => SetProperty(ref _suspensionTravel, value);
        }

        public string WheelName
        {
            get => _wheelName;
            set => SetProperty(ref _wheelName, value);
        }

        public double RideHeight
        {
            get => _rideHeight;
            set => SetProperty(ref _rideHeight, value);
        }

        protected override void ApplyModel(WheelInfo model)
        {
            WheelName = model.WheelKind.ToString();
            DistanceUnits = Distance.GetUnitsSymbol(_settingsProvider.DisplaySettingsViewModel.DistanceUnitsVerySmall);
            SuspensionTravel = model.SuspensionTravel.GetByUnit(_settingsProvider.DisplaySettingsViewModel.DistanceUnitsVerySmall);
            RideHeight = model.RideHeight.GetByUnit(_settingsProvider.DisplaySettingsViewModel.DistanceUnitsVerySmall);
        }

        public override WheelInfo SaveToNewModel()
        {
            throw new InvalidOperationException();
        }
    }
}