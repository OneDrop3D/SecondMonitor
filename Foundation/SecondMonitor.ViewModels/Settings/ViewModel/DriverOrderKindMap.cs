namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using Contracts;
    using Model;

    public class DriverOrderKindMap : AbstractHumanReadableMap<DriverOrderKind>
    {
        public DriverOrderKindMap()
        {
            Translations.Add(DriverOrderKind.Absolute, "Absolute");
            Translations.Add(DriverOrderKind.Relative, "Relative");
            Translations.Add(DriverOrderKind.AbsoluteByClass, "Absolute by Class");
            Translations.Add(DriverOrderKind.HybridByClass, "Hybrid, Grouped by Class");
            Translations.Add(DriverOrderKind.Hybrid, "Hybrid");
        }
    }
}