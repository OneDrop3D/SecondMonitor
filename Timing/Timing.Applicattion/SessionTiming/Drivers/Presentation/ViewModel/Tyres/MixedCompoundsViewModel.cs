namespace SecondMonitor.Timing.Application.SessionTiming.Drivers.Presentation.ViewModel.Tyres
{
    using System;
    using System.Linq;

    using SecondMonitor.Timing.Common.SessionTiming.Drivers;
    using SecondMonitor.ViewModels;

    public class MixedCompoundsViewModel : AbstractViewModel<DriverTiming>, ITyresViewModel
    {
        private string _frontLeftTyreCompound;
        private string _frontRightTyreCompound;
        private string _rearLeftTyreCompound;
        private string _rearRightTyreCompound;

        public string FrontLeftTyreCompound
        {
            get => _frontLeftTyreCompound;
            set => SetProperty(ref _frontLeftTyreCompound, value);
        }

        public string FrontRightTyreCompound
        {
            get => _frontRightTyreCompound;
            set => SetProperty(ref _frontRightTyreCompound, value);
        }

        public string RearLeftTyreCompound
        {
            get => _rearLeftTyreCompound;
            set => SetProperty(ref _rearLeftTyreCompound, value);
        }

        public string RearRightTyreCompound
        {
            get => _rearRightTyreCompound;
            set => SetProperty(ref _rearRightTyreCompound, value);
        }

        public bool HasValue => (!string.IsNullOrWhiteSpace(FrontLeftTyreCompound)) &&
                                (!string.IsNullOrWhiteSpace(FrontRightTyreCompound)) &&
                                (!string.IsNullOrWhiteSpace(RearLeftTyreCompound)) &&
                                (!string.IsNullOrWhiteSpace(RearRightTyreCompound));

        protected override void ApplyModel(DriverTiming model)
        {
            FrontLeftTyreCompound = model.DriverInfo.CarInfo?.WheelsInfo?.FrontLeft?.TyreVisualType ?? string.Empty;
            FrontRightTyreCompound = model.DriverInfo.CarInfo?.WheelsInfo?.FrontRight?.TyreVisualType ?? string.Empty;
            RearLeftTyreCompound = model.DriverInfo.CarInfo?.WheelsInfo?.RearLeft?.TyreVisualType ?? string.Empty;
            RearRightTyreCompound = model.DriverInfo.CarInfo?.WheelsInfo?.RearRight?.TyreVisualType ?? string.Empty;
        }

        public override DriverTiming SaveToNewModel()
        {
            throw new NotSupportedException();
        }

        public bool CanBeUSed(DriverTiming driverTiming)
        {
            return driverTiming.DriverInfo.CarInfo.WheelsInfo.AllWheels.Any(x => x.TyreVisualType != driverTiming.DriverInfo.CarInfo.WheelsInfo.FrontLeft.TyreVisualType);
        }
    }
}
