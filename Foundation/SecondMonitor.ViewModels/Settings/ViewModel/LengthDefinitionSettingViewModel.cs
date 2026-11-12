namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using Model.Layout;

    public class LengthDefinitionSettingViewModel : AbstractViewModel<LengthDefinitionSetting>
    {
        private bool _isAutomaticModeEnabled;
        private bool _isRemainingModeEnabled;
        private bool _isManualModeEnabled;
        private int _manualSize;

        public bool IsAutomaticModeEnabled
        {
            get => _isAutomaticModeEnabled;
            set => SetProperty(ref _isAutomaticModeEnabled, value);
        }

        public bool IsRemainingModeEnabled
        {
            get => _isRemainingModeEnabled;
            set => SetProperty(ref _isRemainingModeEnabled, value);
        }

        public bool IsManualModeEnabled
        {
            get => _isManualModeEnabled;
            set => SetProperty(ref _isManualModeEnabled, value);
        }

        public int ManualSize
        {
            get => _manualSize;
            set => SetProperty(ref _manualSize, value);
        }

        protected override void ApplyModel(LengthDefinitionSetting model)
        {
            IsAutomaticModeEnabled = false;
            IsRemainingModeEnabled = false;
            IsManualModeEnabled = false;
            switch (model.SizeKind)
            {
                case SizeKind.Automatic:
                    IsAutomaticModeEnabled = true;
                    break;
                case SizeKind.Remaining:
                    IsRemainingModeEnabled = true;
                    break;
                case SizeKind.Manual:
                    IsManualModeEnabled = true;
                    break;
            }

            ManualSize = model.ManualSize;
        }

        public override LengthDefinitionSetting SaveToNewModel()
        {
            SizeKind newSizeKind;
            if (IsAutomaticModeEnabled)
            {
                newSizeKind = SizeKind.Automatic;
            }
            else if (IsRemainingModeEnabled)
            {
                newSizeKind = SizeKind.Remaining;
            }
            else
            {
                newSizeKind = SizeKind.Manual;
            }

            return new LengthDefinitionSetting()
            {
                ManualSize = ManualSize,
                SizeKind = newSizeKind
            };
        }
    }
}