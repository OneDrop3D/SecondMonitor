namespace SecondMonitor.ViewModels.Layouts.Factory
{
    using System.Collections.Generic;

    public interface ILayoutElementNamesProvider
    {
        List<string> GetAllowableContentNames();
    }
}