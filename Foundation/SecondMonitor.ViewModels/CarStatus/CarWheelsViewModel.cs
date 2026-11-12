namespace SecondMonitor.ViewModels.CarStatus
{
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;
    using DataModel.Snapshot.Systems;
    using FuelStatus;

    public class CarWheelsViewModel : AbstractViewModel, ISimulatorDataSetViewModel
    {
        public const string ViewModelLayoutName = "Wheels Information";
        private WheelStatusViewModel _leftFrontTyre;
        private WheelStatusViewModel _rightFrontTyre;
        private WheelStatusViewModel _leftRearTyre;
        private WheelStatusViewModel _rightRearTyre;

        public CarWheelsViewModel(SessionRemainingCalculator sessionRemainingCalculator, IPaceProvider paceProvider, WheelStatusViewModelFactory wheelStatusViewModelFactory, IFuelPredictionProvider fuelPredictionProvider)
        {
            LeftFrontTyre = wheelStatusViewModelFactory.Create(true, sessionRemainingCalculator, paceProvider, fuelPredictionProvider);
            LeftRearTyre = wheelStatusViewModelFactory.Create(true, sessionRemainingCalculator, paceProvider, fuelPredictionProvider);
            RightFrontTyre = wheelStatusViewModelFactory.Create(false, sessionRemainingCalculator, paceProvider, fuelPredictionProvider);
            RightRearTyre = wheelStatusViewModelFactory.Create(false, sessionRemainingCalculator, paceProvider, fuelPredictionProvider);
            FrontWheelRadiusInMeters = DefaultWheelRadius;
            RearWheelRadiusInMeters = DefaultWheelRadius;
        }

        public CarWheelsViewModel(WheelStatusViewModelFactory wheelStatusViewModelFactory)
        {
            LeftFrontTyre = wheelStatusViewModelFactory.Create(true);
            LeftRearTyre = wheelStatusViewModelFactory.Create(true);
            RightFrontTyre = wheelStatusViewModelFactory.Create(false);
            RightRearTyre = wheelStatusViewModelFactory.Create(false);
            FrontWheelRadiusInMeters = DefaultWheelRadius;
            RearWheelRadiusInMeters = DefaultWheelRadius;
        }

        public static double DefaultWheelRadius { get; } = 0.3556;

        public WheelStatusViewModel LeftFrontTyre
        {
            get => _leftFrontTyre;
            private set
            {
                _leftFrontTyre = value;
                NotifyPropertyChanged();
            }
        }

        public WheelStatusViewModel RightFrontTyre
        {
            get => _rightFrontTyre;
            private set
            {
                _rightFrontTyre = value;
                NotifyPropertyChanged();
            }
        }

        public WheelStatusViewModel LeftRearTyre
        {
            get => _leftRearTyre;
            private set
            {
                _leftRearTyre = value;
                NotifyPropertyChanged();
            }
        }

        public WheelStatusViewModel RightRearTyre
        {
            get => _rightRearTyre;
            private set
            {
                _rightRearTyre = value;
                NotifyPropertyChanged();
            }
        }

        public double FrontWheelRadiusInMeters { get; set; }
        public double RearWheelRadiusInMeters { get; set; }

        public void UpdateHighFrequencyInformation(SimulatorDataSet dataSet)
        {
            Wheels wheels = dataSet?.PlayerInfo?.CarInfo?.WheelsInfo;

            if (wheels == null)
            {
                return;
            }

            LeftFrontTyre.UpdateSlippingInformation(dataSet, wheels.FrontLeft, FrontWheelRadiusInMeters);
            LeftFrontTyre.UpdateBottomOutInformation(dataSet, wheels.FrontLeft);

            RightFrontTyre.UpdateSlippingInformation(dataSet, wheels.FrontRight, FrontWheelRadiusInMeters);
            RightFrontTyre.UpdateBottomOutInformation(dataSet, wheels.FrontRight);

            LeftRearTyre.UpdateSlippingInformation(dataSet, wheels.RearLeft, RearWheelRadiusInMeters);
            LeftRearTyre.UpdateBottomOutInformation(dataSet, wheels.RearLeft);

            RightRearTyre.UpdateSlippingInformation(dataSet, wheels.RearRight, RearWheelRadiusInMeters);
            RightRearTyre.UpdateBottomOutInformation(dataSet, wheels.RearRight);
        }

        public void ApplyDateSet(SimulatorDataSet dataSet)
        {
            Wheels wheels = dataSet?.PlayerInfo?.CarInfo?.WheelsInfo;

            if (wheels == null)
            {
                return;
            }

            LeftFrontTyre.ApplyWheelCondition(dataSet, wheels.FrontLeft);
            RightFrontTyre.ApplyWheelCondition(dataSet, wheels.FrontRight);
            LeftRearTyre.ApplyWheelCondition(dataSet, wheels.RearLeft);
            RightRearTyre.ApplyWheelCondition(dataSet, wheels.RearRight);
        }

        public void ApplyDateSet(DriverInfo playerInfo)
        {
            Wheels wheels = playerInfo.CarInfo?.WheelsInfo;

            if (wheels == null)
            {
                return;
            }

            LeftFrontTyre.ApplyWheelCondition(wheels.FrontLeft);
            RightFrontTyre.ApplyWheelCondition(wheels.FrontRight);
            LeftRearTyre.ApplyWheelCondition(wheels.RearLeft);
            RightRearTyre.ApplyWheelCondition(wheels.RearRight);
        }

        public void Reset()
        {
            LeftFrontTyre.Reset();
            RightFrontTyre.Reset();
            LeftRearTyre.Reset();
            RightRearTyre.Reset();
        }
    }
}