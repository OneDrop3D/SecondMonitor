namespace SecondMonitor.Timing.Application.ViewModel.TelemetryChecklist
{
    using System.Collections.Generic;
    using System.Linq;
    using Common.SessionTiming.Drivers.Lap;
    using ViewModels;
    using ViewModels.Factory;

    public class LapListViewModel : AbstractViewModel
    {
        private readonly IViewModelFactory _viewModelFactory;

        public LapListViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
            BestLap = viewModelFactory.Create<LapInfoViewModel>();
            LastLap = viewModelFactory.Create<LapInfoViewModel>();
            IsBestLapFilled = false;
        }

        public bool IsBestLapFilled { get; private set; }

        public LapInfoViewModel BestLap { get; }

        public LapInfoViewModel LastLap { get; }

        public List<LapInfoViewModel> AllLaps { get; private set; }

        public void ApplyLaps(ILapInfo bestLap, ILapInfo lastLap, IEnumerable<ILapInfo> allLaps)
        {
            if (bestLap != null)
            {
                IsBestLapFilled = true;
                BestLap.FromModel(bestLap);
            }

            LastLap.FromModel(lastLap);
            AllLaps = allLaps.Select(x =>
            {
                LapInfoViewModel lapInfoViewModel = _viewModelFactory.Create<LapInfoViewModel>();
                lapInfoViewModel.FromModel(x);
                return lapInfoViewModel;
            }).ToList();
        }
    }
}
