namespace SecondMonitor.Timing.Application.ViewModel
{
    using System;
    using System.Collections.Generic;
    using ViewModels.CarStatus;

    public class PaceProviderFromTimingApplicationViewModel : IPaceProvider
    {
        private TimingApplicationViewModel _timingApplicationViewModel;

        public TimeSpan? PlayersPace => _timingApplicationViewModel?.PlayersPace;
        public TimeSpan? LeadersPace => _timingApplicationViewModel?.LeadersPace;
        public Dictionary<string, TimeSpan> GetPaceForDriversMap()
        {
            return _timingApplicationViewModel?.GetPaceForDriversMap() ?? new Dictionary<string, TimeSpan>();
        }

        public void SetTimingApplication(TimingApplicationViewModel timingApplicationViewModel)
        {
            _timingApplicationViewModel = timingApplicationViewModel;
        }
    }
}