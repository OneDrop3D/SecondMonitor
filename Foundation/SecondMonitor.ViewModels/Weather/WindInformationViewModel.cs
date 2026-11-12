namespace SecondMonitor.ViewModels.Weather
{
    using System;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.TrackMap;
    using Settings;
    using Settings.ViewModel;

    public class WindInformationViewModel : AbstractViewModel<WeatherInfo>
    {
        private readonly DisplaySettingsViewModel _displaySettings;
        private double _directionFrom;
        private string _velocityFormatted;
        private bool _isVisible;
        private double _additionalRotation;

        public WindInformationViewModel(ISettingsProvider settingsProvider)
        {
            _displaySettings = settingsProvider.DisplaySettingsViewModel;
            IsVisible = true;
            DirectionFrom = 90;
        }

        public double DirectionFrom
        {
            get => _directionFrom;
            set => SetProperty(ref _directionFrom, value);
        }

        public string VelocityFormatted
        {
            get => _velocityFormatted;
            set => SetProperty(ref _velocityFormatted, value);
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        protected override void ApplyModel(WeatherInfo model)
        {
            IsVisible = model.HasWindInformation;
            VelocityFormatted = model.WindSpeed.GetValueInUnits(_displaySettings.VelocityUnits).ToString("N0") + Velocity.GetUnitSymbol(_displaySettings.VelocityUnits);
            DirectionFrom = Math.Round(model.WindDirectionFrom + _additionalRotation);
        }

        public void UpdateRotationByGeometryDto(TrackGeometryDto trackGeometryDto)
        {
            if (trackGeometryDto.IsSwappedAxis)
            {
                _additionalRotation = trackGeometryDto.XCoef == 1 ? 90 : 270;
            }
            else
            {
                _additionalRotation = trackGeometryDto.XCoef == 1 ? 0 : 180;
            }
        }

        public override WeatherInfo SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}