namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using SecondMonitor.Contracts;
    using SecondMonitor.DataModel.BasicProperties;

    public class SessionBestInformationMap : AbstractHumanReadableMap<SessionBestInformationKind>
    {
        public SessionBestInformationMap()
        {
            Translations.Add(SessionBestInformationKind.BestOfClass, "Player Class Best");
            Translations.Add(SessionBestInformationKind.BestOverall, "Overall Best");
            Translations.Add(SessionBestInformationKind.BestOverallAndClass, "Switch Between Player Class & Overall Best");
            Translations.Add(SessionBestInformationKind.AllClasses, "Switch Between All Classes");
        }
    }
}