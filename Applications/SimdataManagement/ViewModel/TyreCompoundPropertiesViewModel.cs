namespace SecondMonitor.SimdataManagement.ViewModel
{
    using System.Windows.Input;
    using DataModel.BasicProperties;
    using DataModel.OperationalRange;
    using ViewModels;
    using ViewModels.Settings;

    public class TyreCompoundPropertiesViewModel : AbstractViewModel<TyreCompoundProperties>, ITyreSettingsViewModel
    {
        private string _compoundName;

        private double _frontIdealPressure;
        private double _frontIdealPressureWindow;

        private double _frontMinimumIdealTyreTemperature;
        private double _frontMaximumIdealTyreTemperature;

        private bool _isGlobalCompound;
        private double _rearMinimalIdealTyreTemperature;
        private double _rearMaximumIdealTyreTemperature;
        private double _rearIdealPressure;
        private double _rearIdealPressureWindow;
        private bool _determineIdealPressureByTemperature;
        private double _noWearLimit;
        private double _lowWearLimit;
        private double _heavyWearLimit;
        private ICommand _copyCompoundCommand;
        private TemperatureUnits _temperatureUnit;
        private PressureUnits _pressureUnits;

        public TyreCompoundPropertiesViewModel(ISettingsProvider settingsProvider)
        {
            PressureUnits = settingsProvider.DisplaySettingsViewModel.DisplayPressureUnits;
            TemperatureUnit = settingsProvider.DisplaySettingsViewModel.TemperatureUnits;
            PressureUnitSymbol = Pressure.GetUnitSymbol(PressureUnits);
            TemperatureUnitSymbol = Temperature.GetUnitSymbol(TemperatureUnit);
        }

        public bool IsGlobalCompound
        {
            get => _isGlobalCompound;
            set
            {
                _isGlobalCompound = value;
                NotifyPropertyChanged();
            }
        }

        public string TemperatureUnitSymbol { get; }

        public string PressureUnitSymbol { get; }

        public string CompoundName
        {
            get => _compoundName;
            set
            {
                _compoundName = value;
                NotifyPropertyChanged();
            }
        }

        public double FrontIdealPressure
        {
            get => _frontIdealPressure;
            set => SetProperty(ref _frontIdealPressure, value);
        }

        public double FrontIdealPressureWindow
        {
            get => _frontIdealPressureWindow;
            set => SetProperty(ref _frontIdealPressureWindow, value);
        }

        public double RearIdealPressure
        {
            get => _rearIdealPressure;
            set => SetProperty(ref _rearIdealPressure, value);
        }

        public double RearIdealPressureWindow
        {
            get => _rearIdealPressureWindow;
            set => SetProperty(ref _rearIdealPressureWindow, value);
        }

        public double RearMinimalIdealTyreTemperature
        {
            get => _rearMinimalIdealTyreTemperature;
            set => SetProperty(ref _rearMinimalIdealTyreTemperature, value);
        }

        public double RearMaximumIdealTyreTemperature
        {
            get => _rearMaximumIdealTyreTemperature;
            set => SetProperty(ref _rearMaximumIdealTyreTemperature, value);
        }

        public double NoWearLimit
        {
            get => _noWearLimit;
            set => SetProperty(ref _noWearLimit, value);
        }

        public double LowWearLimit
        {
            get => _lowWearLimit;
            set => SetProperty(ref _lowWearLimit, value);
        }

        public double HeavyWearLimit
        {
            get => _heavyWearLimit;
            set => SetProperty(ref _heavyWearLimit, value);
        }

        public double FrontMinimalIdealTyreTemperature
        {
            get => _frontMinimumIdealTyreTemperature;
            set => SetProperty(ref _frontMinimumIdealTyreTemperature, value);
        }

        public double FrontMaximumIdealTyreTemperature
        {
            get => _frontMaximumIdealTyreTemperature;
            set => SetProperty(ref _frontMaximumIdealTyreTemperature, value);
        }

        public ICommand CopyCompoundCommand
        {
            get => _copyCompoundCommand;
            set => SetProperty(ref _copyCompoundCommand, value, false);
        }

        public TemperatureUnits TemperatureUnit
        {
            get => _temperatureUnit;
            set => SetProperty(ref _temperatureUnit, value);
        }

        public PressureUnits PressureUnits
        {
            get => _pressureUnits;
            set => SetProperty(ref _pressureUnits, value);
        }

        public bool DetermineIdealPressureByTemperature
        {
            get => _determineIdealPressureByTemperature;
            set => SetProperty(ref _determineIdealPressureByTemperature, value);
        }

        protected override void ApplyModel(TyreCompoundProperties model)
        {
            CompoundName = model.CompoundName;
            NoWearLimit = model.NoWearLimit * 100.0;
            LowWearLimit = model.LowWearLimit * 100.0;
            HeavyWearLimit = model.HeavyWearLimit * 100.0;

            FrontIdealPressure = model.FrontIdealPressure.GetValueInUnits(PressureUnits);
            FrontIdealPressureWindow = model.FrontIdealPressureWindow.GetValueInUnits(PressureUnits) * 2;

            RearIdealPressure = model.RearIdealPressure.GetValueInUnits(PressureUnits);
            //Times 2, because in coloring the window is just from center to one of the edges... for the user having it as the full range is more intuitive
            RearIdealPressureWindow = model.RearIdealPressureWindow.GetValueInUnits(PressureUnits) * 2;

            FrontMinimalIdealTyreTemperature = model.FrontIdealTemperature.GetValueInUnits(TemperatureUnit) - model.FrontIdealTemperatureWindow.GetValueInUnits(TemperatureUnit);
            FrontMaximumIdealTyreTemperature = model.FrontIdealTemperature.GetValueInUnits(TemperatureUnit) + model.FrontIdealTemperatureWindow.GetValueInUnits(TemperatureUnit);

            RearMinimalIdealTyreTemperature = model.RearIdealTemperature.GetValueInUnits(TemperatureUnit) - model.RearIdealTemperatureWindow.GetValueInUnits(TemperatureUnit);
            RearMaximumIdealTyreTemperature = model.RearIdealTemperature.GetValueInUnits(TemperatureUnit) + model.RearIdealTemperatureWindow.GetValueInUnits(TemperatureUnit);

            DetermineIdealPressureByTemperature = model.DetermineIdealPressureByTemperature;
        }

        public override TyreCompoundProperties SaveToNewModel()
        {
            return new TyreCompoundProperties()
            {
                CompoundName = CompoundName,
                LowWearLimit = LowWearLimit / 100.0,
                NoWearLimit = NoWearLimit / 100.0,
                HeavyWearLimit = HeavyWearLimit / 100.0,
                FrontIdealPressure = Pressure.GetFromUnits(FrontIdealPressure, PressureUnits),
                FrontIdealPressureWindow = Pressure.GetFromUnits(FrontIdealPressureWindow * 0.5, PressureUnits),
                FrontIdealTemperature = Temperature.FrontUnits((FrontMinimalIdealTyreTemperature + FrontMaximumIdealTyreTemperature) * 0.5, TemperatureUnit),
                FrontIdealTemperatureWindow = Temperature.FrontUnits((FrontMaximumIdealTyreTemperature - FrontMinimalIdealTyreTemperature) * 0.5, TemperatureUnit),

                RearIdealPressure = Pressure.GetFromUnits(RearIdealPressure, PressureUnits),
                RearIdealPressureWindow = Pressure.GetFromUnits(RearIdealPressureWindow * 0.5, PressureUnits),
                RearIdealTemperature = Temperature.FrontUnits((RearMinimalIdealTyreTemperature + RearMaximumIdealTyreTemperature) * 0.5, TemperatureUnit),
                RearIdealTemperatureWindow = Temperature.FrontUnits((RearMaximumIdealTyreTemperature - RearMinimalIdealTyreTemperature) * 0.5, TemperatureUnit),
                IsSimOriginal = OriginalModel.IsSimOriginal && !IsModified,
                DetermineIdealPressureByTemperature = DetermineIdealPressureByTemperature,
            };
        }
    }
}