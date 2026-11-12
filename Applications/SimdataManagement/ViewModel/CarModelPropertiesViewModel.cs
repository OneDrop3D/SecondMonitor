namespace SecondMonitor.SimdataManagement.ViewModel
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows.Input;

    using DataModel.BasicProperties;
    using DataModel.OperationalRange;

    using SecondMonitor.Contracts;

    using ViewModels;
    using ViewModels.Factory;
    using ViewModels.Settings;

    public class CarModelPropertiesViewModel : AbstractViewModel<CarModelProperties>
    {
        private readonly ViewModelFactory _viewModelFactory;
        private Distance _frontStaticRideHeight;
        private Distance _rearStaticRideHeight;
        private HumanReadableItem<double> _selectedMotionRatio;
        private double _minimalOptimalBrakeTemperature;
        private double _maximumOptimalBrakeTemperature;
        private Distance _frontWheelDiameter;
        private Distance _rearWheelDiameter;

        private string _carName;
        private int _wheelRotation;
        private bool _wasWheelDiameterSet;
        private ICommand _cancelCommand;
        private ICommand _okCommand;
        private ICommand _openTyreDiameterWizardCommand;
        private ICommand _resetCarSettingsCommand;
        private TyreCompoundPropertiesViewModel _selectedTyreCompound;
        private string _carCustomName;
        private ICommand _nextCarCommand;
        private ICommand _previousCarCommand;
        private string _carIndex;

        public CarModelPropertiesViewModel(ViewModelFactory viewModelFactory, ISettingsProvider settingsProvider)
        {
            _viewModelFactory = viewModelFactory;
            SelectedMotionRatio = new HumanReadableItem<double>(1.0, "One-To-One 1.0");
            TyreCompounds = new ObservableCollection<TyreCompoundPropertiesViewModel>();
            TemperatureUnit = settingsProvider.DisplaySettingsViewModel.TemperatureUnits;
            DistanceUnits = settingsProvider.DisplaySettingsViewModel.DistanceUnitsVerySmall;
            TemperatureUnitsSymbol = Temperature.GetUnitSymbol(TemperatureUnit);
            AllMotionRatioOptions = new ObservableCollection<HumanReadableItem<double>>
            {
                new(0.7, "Pushrod suspension (IndyCar, F1, GT3, etc.) ~0.6–0.8"),
                new(0.95, "MacPherson strut (road cars) ~0.9–1.0"),
                new(0.8, "Double wishbone (road + race) ~0.7–0.9"),
                new(1.0, "One-To-One 1.0")
            };
        }

        public string TemperatureUnitsSymbol { get; }

        public string CarName
        {
            get => _carName;
            set => SetProperty(ref _carName, value);
        }

        public string CarCustomName
        {
            get => _carCustomName;
            set => SetProperty(ref _carCustomName, value);
        }

        public int WheelRotation
        {
            get => _wheelRotation;
            set => SetProperty(ref _wheelRotation, value);
        }

        public Distance FrontStaticRideHeight
        {
            get => _frontStaticRideHeight;
            set => SetProperty(ref _frontStaticRideHeight, value);
        }

        public Distance RearStaticRideHeight
        {
            get => _rearStaticRideHeight;
            set => SetProperty(ref _rearStaticRideHeight, value);
        }

        public ObservableCollection<HumanReadableItem<double>> AllMotionRatioOptions { get; }

        public HumanReadableItem<double> SelectedMotionRatio
        {
            get => _selectedMotionRatio;
            set
            {
                if (value == null)
                {
                    // Handle null assignment gracefully, e.g. fallback to default or previous value
                    SetProperty(ref _selectedMotionRatio, new HumanReadableItem<double>(1.0, "One-To-One 1.0"));
                }
                else
                {
                    SetProperty(ref _selectedMotionRatio, new HumanReadableItem<double>(
                        Math.Max(0.0, Math.Min(1.0, value.Value)), value.HumanReadableValue));
                }
            }
        }

        public string SelectedMotionRatioText
        {
            get => SelectedMotionRatio.HumanReadableValue;
            set
            {
                if (SelectedMotionRatioText != value)
                {
                    if (double.TryParse(value, out double ratio))
                    {
                        ratio = Math.Max(0.0, Math.Min(1.0, ratio));
                        SelectedMotionRatio = new HumanReadableItem<double>(ratio, value);
                    }
                    else
                    {
                        SelectedMotionRatio = new HumanReadableItem<double>(1.0, "One-To-One 1.0");
                    }
                }

                NotifyPropertyChanged();
            }
        }

        public Distance FrontWheelDiameter
        {
            get => _frontWheelDiameter;
            set => SetProperty(ref _frontWheelDiameter, value);
        }

        public Distance RearWheelDiameter
        {
            get => _rearWheelDiameter;
            set => SetProperty(ref _rearWheelDiameter, value);
        }

        public double MinimalOptimalBrakeTemperature
        {
            get => _minimalOptimalBrakeTemperature;
            set => SetProperty(ref _minimalOptimalBrakeTemperature, value);
        }

        public double MaximumOptimalBrakeTemperature
        {
            get => _maximumOptimalBrakeTemperature;
            set => SetProperty(ref _maximumOptimalBrakeTemperature, value);
        }

        public ObservableCollection<TyreCompoundPropertiesViewModel> TyreCompounds { get; }

        public ICommand OkCommand
        {
            get => _okCommand;
            set => SetProperty(ref _okCommand, value);
        }

        public ICommand CancelCommand
        {
            get => _cancelCommand;
            set => SetProperty(ref _cancelCommand, value);
        }

        public TemperatureUnits TemperatureUnit { get; }

        public DistanceUnits DistanceUnits { get; }

        public ICommand OpenTyreDiameterWizardCommand
        {
            get => _openTyreDiameterWizardCommand;
            set => SetProperty(ref _openTyreDiameterWizardCommand, value);
        }

        public ICommand ResetCarSettingsCommand
        {
            get => _resetCarSettingsCommand;
            set => SetProperty(ref _resetCarSettingsCommand, value);
        }

        public ICommand NextCarCommand
        {
            get => _nextCarCommand;
            set => SetProperty(ref _nextCarCommand, value);
        }

        public ICommand PreviousCarCommand
        {
            get => _previousCarCommand;
            set => SetProperty(ref _previousCarCommand, value);
        }

        public string CarIndex
        {
            get => _carIndex;
            set => SetProperty(ref _carIndex, value);
        }

        public TyreCompoundPropertiesViewModel SelectedTyreCompound
        {
            get => _selectedTyreCompound;
            set => SetProperty(ref _selectedTyreCompound, value);
        }

        protected override void ApplyModel(CarModelProperties model)
        {
            TyreCompounds.Clear();
            CarName = model.Name;
            CarCustomName = model.CarCustomName;
            WheelRotation = model.WheelRotation;
            FrontStaticRideHeight = Distance.FromMeters(model.FrontStaticRideHeight.InMeters);
            RearStaticRideHeight = Distance.FromMeters(model.RearStaticRideHeight.InMeters);

            var matchedOption = AllMotionRatioOptions.FirstOrDefault(x => Math.Abs(x.Value - model.MotionRatio) < 0.0001);
            if (matchedOption != null)
            {
                SelectedMotionRatio = matchedOption;
            }
            else
            {
                SelectedMotionRatio = new HumanReadableItem<double>(
                    Math.Max(0.0, Math.Min(1.0, model.MotionRatio)),
                    model.MotionRatio.ToString("0.###"));
            }

            FrontWheelDiameter = Distance.FromMeters(model.FrontWheelDiameter.InMeters);
            RearWheelDiameter = Distance.FromMeters(model.RearWheelDiameter.InMeters);
            MinimalOptimalBrakeTemperature = model.OptimalBrakeTemperature.GetValueInUnits(TemperatureUnit) - model.OptimalBrakeTemperatureWindow.GetValueInUnits(TemperatureUnit);
            MaximumOptimalBrakeTemperature = model.OptimalBrakeTemperature.GetValueInUnits(TemperatureUnit) + model.OptimalBrakeTemperatureWindow.GetValueInUnits(TemperatureUnit);
            _wasWheelDiameterSet = model.WasWheelDiameterSet;
            foreach (TyreCompoundProperties modelTyreCompoundsProperty in model.TyreCompoundsProperties.Where(x => x.IsValid))
            {
                TyreCompoundPropertiesViewModel newViewModel = _viewModelFactory.Create<TyreCompoundPropertiesViewModel>();
                newViewModel.FromModel(modelTyreCompoundsProperty);
                TyreCompounds.Add(newViewModel);
            }
        }

        public override CarModelProperties SaveToNewModel()
        {
            bool wasWheelDiameterChanged = OriginalModel.FrontWheelDiameter.InMillimeter != FrontWheelDiameter.InMillimeter ||
                                           OriginalModel.RearWheelDiameter.InMillimeter != RearWheelDiameter.InMillimeter;
            return new CarModelProperties()
            {
                Name = CarName,
                CarCustomName = CarCustomName,
                OptimalBrakeTemperature = Temperature.FrontUnits((MinimalOptimalBrakeTemperature + MaximumOptimalBrakeTemperature) * 0.5, TemperatureUnit),
                OptimalBrakeTemperatureWindow = Temperature.FrontUnits((MaximumOptimalBrakeTemperature - MinimalOptimalBrakeTemperature) * 0.5, TemperatureUnit),
                TyreCompoundsProperties = TyreCompounds.Select(x => x.SaveToNewModel()).ToList(),
                WheelRotation = WheelRotation,
                OriginalContainsOptimalTemperature = OriginalModel.OriginalContainsOptimalTemperature,
                FrontStaticRideHeight = FrontStaticRideHeight,
                RearStaticRideHeight = RearStaticRideHeight,
                MotionRatio = SelectedMotionRatio.Value,
                FrontWheelDiameter = FrontWheelDiameter,
                RearWheelDiameter = RearWheelDiameter,
                WasWheelDiameterSet = _wasWheelDiameterSet || wasWheelDiameterChanged,
            };
        }
    }
}
