namespace SecondMonitor.Timing.Application.ViewModel.TelemetryChecklist
{
    using System;
    using System.Windows.Input;
    using Common.SessionTiming.Drivers.Lap;

    using SecondMonitor.DataModel.Extensions;

    using ViewModels;

    public class LapInfoViewModel : AbstractViewModel<ILapInfo>
    {
        public string LapNumber { get; private set; }

        public string LapTime { get; private set; }

        public ICommand OpenCommand { get; set; }

        protected override void ApplyModel(ILapInfo model)
        {
            LapNumber = model.LapNumber.ToString();
            LapTime = model.LapTime.FormatToDefault();
        }

        public override ILapInfo SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}