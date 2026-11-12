namespace SecondMonitor.ViewModels.Controllers
{
#if !DEBUG
    using AutoUpdaterDotNET;
#endif

    public class AutoUpdateController
    {
        public void CheckForUpdate()
        {
#if !DEBUG
            AutoUpdater.Start("https://gitlab.com/winzarten/SecondMonitor/raw/master/AutoUpdater.xml");
//AutoUpdater.Start("https://gitlab.com/winzarten/SecondMonitor/raw/master/AutoUpdater2.xml");
#endif
        }
    }
}