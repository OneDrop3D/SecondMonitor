namespace SecondMonitor.Timing.Application.SessionTiming.Drivers.Presentation.ViewModel.Tyres
{
    using System;
    using System.Linq;
    
    using SecondMonitor.Timing.Common.SessionTiming.Drivers;
    using SecondMonitor.ViewModels;

    public class SingleCompoundViewModel : AbstractViewModel<DriverTiming>, ITyresViewModel
    {
        private string _tyreCompound;
        public string TyreCompound
        {
            get => _tyreCompound;
            set => SetProperty(ref _tyreCompound, value);
        }

        public bool HasValue => !string.IsNullOrWhiteSpace(TyreCompound);

        protected override void ApplyModel(DriverTiming model)
        {
            TyreCompound = model.DriverInfo.CarInfo?.WheelsInfo?.FrontLeft?.TyreVisualType ?? string.Empty;
        }

        public override DriverTiming SaveToNewModel()
        {
            throw new NotSupportedException();
        }

        public bool CanBeUSed(DriverTiming driverTiming)
        {
            return driverTiming.DriverInfo.CarInfo.WheelsInfo.AllWheels.All(x => x.TyreVisualType == driverTiming.DriverInfo.CarInfo.WheelsInfo.FrontLeft.TyreVisualType);
        }
    }
}
