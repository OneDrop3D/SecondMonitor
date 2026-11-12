namespace SecondMonitor.ViewModels.Layouts.Editor
{
    public class LayoutEditorRootViewModel : AbstractViewModel, ILayoutContainer
    {
        private ILayoutConfigurationViewModel _layoutElement;

        public ILayoutConfigurationViewModel LayoutElement
        {
            get => _layoutElement;
            set => SetProperty(ref _layoutElement, value);
        }

        public void SetLayout(ILayoutConfigurationViewModel layoutElement)
        {
            LayoutElement = layoutElement;
        }
    }
}