namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.Settings.Car
{
    using DataModel.BasicProperties;
    using DataModel.BasicProperties.Units;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Settings;
    using TelemetryManagement.Settings;

    public class WheelPropertiesViewModel : AbstractViewModel<WheelPropertiesDto>
    {
        private readonly UnitsCollection _unitsCollection;
        private string _distanceUnits;
        private string _forceUnits;
        private string _angleUnits;
        private string _velocityUnits;
        private double _neutralSuspensionTravel;
        private double _sprintStiffnessPerMm;
        private double _idealCamber;
        private double _reboundTransition;
        private double _bumpTransition;

        public WheelPropertiesViewModel(ISettingsProvider settingsProvider)
        {
            _unitsCollection = settingsProvider.DisplaySettingsViewModel.GetUnitsCollection();
        }

        public string WheelName { get; set; }

        public double BumpTransition
        {
            get => _bumpTransition;
            set => SetProperty(ref _bumpTransition, value);
        }

        public double ReboundTransition
        {
            get => _reboundTransition;
            set => SetProperty(ref _reboundTransition, value);
        }

        public double IdealCamber
        {
            get => _idealCamber;
            set => SetProperty(ref _idealCamber, value);
        }

        public double SprintStiffnessPerMm
        {
            get => _sprintStiffnessPerMm;
            set => SetProperty(ref _sprintStiffnessPerMm, value);
        }

        public double NeutralSuspensionTravel
        {
            get => _neutralSuspensionTravel;
            set => SetProperty(ref _neutralSuspensionTravel, value);
        }

        public string VelocityUnits
        {
            get => _velocityUnits;
            set => SetProperty(ref _velocityUnits, value);
        }

        public string AngleUnits
        {
            get => _angleUnits;
            set => SetProperty(ref _angleUnits, value);
        }

        public string ForceUnits
        {
            get => _forceUnits;
            set => SetProperty(ref _forceUnits, value);
        }

        public string DistanceUnits
        {
            get => _distanceUnits;
            set => SetProperty(ref _distanceUnits, value);
        }

        protected override void ApplyModel(WheelPropertiesDto model)
        {
            BumpTransition = model.BumpTransition.GetValueInUnits(_unitsCollection.VelocityUnitsSmall);
            ReboundTransition = model.ReboundTransition.GetValueInUnits(_unitsCollection.VelocityUnitsSmall);
            IdealCamber = model.IdealCamber.GetValueInUnits(_unitsCollection.AngleUnits);
            SprintStiffnessPerMm = model.SprintStiffnessPerMm.GetValueInUnits(_unitsCollection.ForceUnits);
            NeutralSuspensionTravel = model.NeutralSuspensionTravel.GetByUnit(_unitsCollection.DistanceUnitsVerySmall);
            VelocityUnits = Velocity.GetUnitSymbol(_unitsCollection.VelocityUnitsSmall);
            AngleUnits = Angle.GetUnitsSymbol(_unitsCollection.AngleUnits);
            ForceUnits = Force.GetUnitSymbol(_unitsCollection.ForceUnits);
            DistanceUnits = Distance.GetUnitsSymbol(_unitsCollection.DistanceUnitsVerySmall);
        }

        public override WheelPropertiesDto SaveToNewModel()
        {
            return new WheelPropertiesDto()
            {
                BumpTransition = Velocity.FromUnits(BumpTransition, _unitsCollection.VelocityUnitsSmall),
                ReboundTransition = Velocity.FromUnits(ReboundTransition, _unitsCollection.VelocityUnitsSmall),
                IdealCamber = Angle.GetFromValue(IdealCamber, _unitsCollection.AngleUnits),
                NeutralSuspensionTravel = Distance.CreateByUnits(NeutralSuspensionTravel, _unitsCollection.DistanceUnitsVerySmall),
                SprintStiffnessPerMm = Force.GetFromNewtons(SprintStiffnessPerMm),
            };
        }
    }
}