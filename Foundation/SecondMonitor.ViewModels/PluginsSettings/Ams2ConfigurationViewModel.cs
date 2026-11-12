namespace SecondMonitor.ViewModels.PluginsSettings
{
    using PluginsConfiguration.Common.DataModel;

    public class Ams2ConfigurationViewModel : AbstractViewModel<Ams2Configuration>
    {
        private bool _splitVintageTouringTier1;
        private bool _splitVintageTouringTier2;
        private bool _splitTsiCup;
        private bool _splitCarreraCup;
        private bool _splitLancerCup;
        private bool _splitGtClassics;

        public bool SplitVintageTouringTier1
        {
            get => _splitVintageTouringTier1;
            set => SetProperty(ref _splitVintageTouringTier1, value);
        }

        public bool SplitVintageTouringTier2
        {
            get => _splitVintageTouringTier2;
            set => SetProperty(ref _splitVintageTouringTier2, value);
        }

        public bool SplitTsiCup
        {
            get => _splitTsiCup;
            set => SetProperty(ref _splitTsiCup, value);
        }

        public bool SplitCarreraCup
        {
            get => _splitCarreraCup;
            set => SetProperty(ref _splitCarreraCup, value);
        }

        public bool SplitLancerCup
        {
            get => _splitLancerCup;
            set => SetProperty(ref _splitLancerCup, value);
        }

        public bool SplitGtClassics
        {
            get => _splitGtClassics;
            set => SetProperty(ref _splitGtClassics, value);
        }

        protected override void ApplyModel(Ams2Configuration model)
        {
            SplitVintageTouringTier1 = model.SplitVintageTouringTier1;
            SplitVintageTouringTier2 = model.SplitVintageTouringTier2;
            SplitTsiCup = model.SplitTsiCup;
            SplitCarreraCup = model.SplitCarreraCup;
            SplitLancerCup = model.SplitLancerCup;
            _splitGtClassics = model.SplitGtClassics;
        }

        public override Ams2Configuration SaveToNewModel()
        {
            return new Ams2Configuration()
            {
                SplitVintageTouringTier1 = SplitVintageTouringTier1,
                SplitVintageTouringTier2 = SplitVintageTouringTier2,
                SplitCarreraCup = SplitLancerCup,
                SplitLancerCup = SplitLancerCup,
                SplitGtClassics = SplitGtClassics,
                SplitTsiCup = SplitTsiCup,
            };
        }
    }
}