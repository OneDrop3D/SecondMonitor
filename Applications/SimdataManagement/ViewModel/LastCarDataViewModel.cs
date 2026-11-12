namespace SecondMonitor.SimdataManagement.ViewModel
{
    using DataModel.Snapshot;
    using ViewModels;
    using ViewModels.Factory;

    public class LastCarDataViewModel : AbstractViewModel<SimulatorDataSet>
    {
        public LastCarDataViewModel(IViewModelFactory viewModelFactory)
        {
            FrontLeftWheel = viewModelFactory.Create<LastWheelDateViewModel>();
            FrontRightWheel = viewModelFactory.Create<LastWheelDateViewModel>();
            RearLeftWheel = viewModelFactory.Create<LastWheelDateViewModel>();
            RearRightWheel = viewModelFactory.Create<LastWheelDateViewModel>();
        }

        public LastWheelDateViewModel FrontLeftWheel { get; }
        public LastWheelDateViewModel FrontRightWheel { get; }
        public LastWheelDateViewModel RearLeftWheel { get; }
        public LastWheelDateViewModel RearRightWheel { get; }

        protected override void ApplyModel(SimulatorDataSet model)
        {
            var wheelsInfo = model.PlayerInfo.CarInfo.WheelsInfo;
            FrontLeftWheel.FromModel(wheelsInfo.FrontLeft);
            FrontRightWheel.FromModel(wheelsInfo.FrontRight);
            RearLeftWheel.FromModel(wheelsInfo.RearLeft);
            RearRightWheel.FromModel(wheelsInfo.RearRight);
        }

        public override SimulatorDataSet SaveToNewModel()
        {
            throw new System.InvalidOperationException();
        }
    }
}