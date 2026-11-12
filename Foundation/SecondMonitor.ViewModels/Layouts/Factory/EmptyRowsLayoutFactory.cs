namespace SecondMonitor.ViewModels.Layouts.Factory
{
    using Settings.Model.Layout;

    public class EmptyRowsLayoutFactory : IDefaultLayoutFactory
    {
        public LayoutDescription CreateDefaultLayout()
        {
            return new LayoutDescription()
            {
                Name = "Empty",
                RootElement = new RowsDefinitionSetting(),
            };
        }
    }
}