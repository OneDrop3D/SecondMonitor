namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using Layouts;
    using Model;

    public class LayoutSettingsViewModel : AbstractViewModel<LayoutSettings>
    {
        private LayoutDescription _layoutDescription;

        public LayoutDescription LayoutDescription
        {
            get => _layoutDescription;
            set => SetProperty(ref _layoutDescription, value);
        }

        protected override void ApplyModel(LayoutSettings model)
        {
            LayoutDescription = model.Description;
        }

        public override LayoutSettings SaveToNewModel()
        {
            return new LayoutSettings()
            {
                Description = LayoutDescription
            };
        }
    }
}