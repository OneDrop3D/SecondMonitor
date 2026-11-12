namespace SecondMonitor.ViewModels.Layouts.Editor
{
    using System;
    using System.Linq;
    using Settings.Model.Layout;

    public class GenericSettingsPropertiesViewModel : AbstractViewModel<GenericContentSetting>
    {
        private bool _isCustomWidth;
        private bool _isCustomHeight;
        private int _customWidth;
        private int _customHeight;
        private bool _isExpanderEnabled;
        private ExpandDirection _expandDirection;
        private bool _isExpanderExpanded;

        public GenericSettingsPropertiesViewModel()
        {
            AllowableExpandDirections = Enum.GetValues(typeof(ExpandDirection)).Cast<ExpandDirection>().ToArray();
        }

        public bool IsCustomWidth
        {
            get => _isCustomWidth;
            set => SetProperty(ref _isCustomWidth, value);
        }

        public bool IsCustomHeight
        {
            get => _isCustomHeight;
            set => SetProperty(ref _isCustomHeight, value);
        }

        public int CustomWidth
        {
            get => _customWidth;
            set => SetProperty(ref _customWidth, value);
        }

        public int CustomHeight
        {
            get => _customHeight;
            set => SetProperty(ref _customHeight, value);
        }

        public bool IsExpanderEnabled
        {
            get => _isExpanderEnabled;
            set => SetProperty(ref _isExpanderEnabled, value);
        }

        public ExpandDirection ExpandDirection
        {
            get => _expandDirection;
            set => SetProperty(ref _expandDirection, value);
        }

        public bool IsExpanderExpanded
        {
            get => _isExpanderExpanded;
            set => SetProperty(ref _isExpanderExpanded, value);
        }

        public ExpandDirection[] AllowableExpandDirections { get; set; }

        protected override void ApplyModel(GenericContentSetting model)
        {
            IsCustomWidth = model.IsCustomWidth;
            IsCustomHeight = model.IsCustomHeight;
            CustomWidth = model.CustomWidth;
            CustomHeight = model.CustomHeight;
            IsExpanderExpanded = model.IsExpanderExpanded;
            IsExpanderEnabled = model.IsExpanderEnabled;
            ExpandDirection = model.ExpandDirection;
        }

        public override GenericContentSetting SaveToNewModel()
        {
            throw new NotSupportedException();
        }

        public void ApplyGenericSettings(GenericContentSetting genericContentSetting)
        {
            genericContentSetting.ExpandDirection = ExpandDirection;
            genericContentSetting.CustomHeight = CustomHeight;
            genericContentSetting.CustomWidth = CustomWidth;
            genericContentSetting.IsCustomHeight = IsCustomHeight;
            genericContentSetting.IsCustomWidth = IsCustomWidth;
            genericContentSetting.IsExpanderEnabled = IsExpanderEnabled;
            genericContentSetting.IsExpanderExpanded = IsExpanderExpanded;
        }
    }
}