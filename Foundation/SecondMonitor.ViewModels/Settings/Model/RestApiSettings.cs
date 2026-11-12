namespace SecondMonitor.ViewModels.Settings.Model
{
    public class RestApiSettings
    {
        public bool IsEnabled { get; set; } = false;

        public int Port { get; set; } = 5007;

        public bool IsSwaggerEnabled { get; set; } = true;
    }
}
