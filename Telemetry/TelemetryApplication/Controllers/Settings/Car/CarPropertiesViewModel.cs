namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.Settings.Car
{
    using System.Windows.Input;
    using DataModel.BasicProperties;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;
    using TelemetryManagement.Settings;

    public class CarPropertiesViewModel : AbstractViewModel<CarPropertiesDto>
    {
        private ICommand _okCommand;
        private ICommand _cancelCommand;
        private string _carName;

        public CarPropertiesViewModel(IViewModelFactory viewModelFactory)
        {
            FrontLeftWheel = viewModelFactory.Create<WheelPropertiesViewModel>();
            FrontLeftWheel.WheelName = "Front Left";

            FrontRightWheel = viewModelFactory.Create<WheelPropertiesViewModel>();
            FrontRightWheel.WheelName = "Front Right";

            RearLeftWheel = viewModelFactory.Create<WheelPropertiesViewModel>();
            RearLeftWheel.WheelName = "Rear Left";

            RearRightWheel = viewModelFactory.Create<WheelPropertiesViewModel>();
            RearRightWheel.WheelName = "Rear Right";
        }

        public WheelPropertiesViewModel FrontLeftWheel { get; }
        public WheelPropertiesViewModel FrontRightWheel { get; }
        public WheelPropertiesViewModel RearLeftWheel { get; }
        public WheelPropertiesViewModel RearRightWheel { get; }

        public double SteeringLockDegrees { get; set; }

        public string CarName
        {
            get => _carName;
            set => SetProperty(ref _carName, value);
        }

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

        protected override void ApplyModel(CarPropertiesDto model)
        {
            CarName = model.CarName;
            FrontLeftWheel.FromModel(model.FrontLeftTyre);
            FrontRightWheel.FromModel(model.FrontRightTyre);
            RearLeftWheel.FromModel(model.RearLeftTyre);
            RearRightWheel.FromModel(model.RearRightTyre);
            SteeringLockDegrees = model.SteeringLock.InDegrees;
        }

        public override CarPropertiesDto SaveToNewModel()
        {
            return new CarPropertiesDto()
            {
                CarName = OriginalModel.CarName,
                Simulator = OriginalModel.Simulator,
                FrontLeftTyre = FrontLeftWheel.SaveToNewModel(),
                FrontRightTyre = FrontRightWheel.SaveToNewModel(),
                RearLeftTyre = RearLeftWheel.SaveToNewModel(),
                RearRightTyre = RearRightWheel.SaveToNewModel(),
                SteeringLock = Angle.GetFromDegrees(SteeringLockDegrees),
            };
        }
    }
}