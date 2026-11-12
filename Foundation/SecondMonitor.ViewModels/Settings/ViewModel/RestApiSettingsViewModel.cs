namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using SecondMonitor.ViewModels.Settings.Model;

    public class RestApiSettingsViewModel : AbstractViewModel<RestApiSettings>
    {
        private bool _isEnabled;
        private int _port;
        private bool _isSwaggerEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        public int Port
        {
            get => _port;
            set => SetProperty(ref _port, value, (_, __) => NotifyPropertyChanged(nameof(SwaggerUrl)));
        }

        public bool IsSwaggerEnabled
        {
            get => _isSwaggerEnabled;
            set => SetProperty(ref _isSwaggerEnabled, value);
        }

        public string SwaggerUrl => $"http://localhost:{Port}/swagger/index.html";

        protected override void ApplyModel(RestApiSettings model)
        {
            IsEnabled = model.IsEnabled;
            Port = model.Port;
            IsSwaggerEnabled = model.IsSwaggerEnabled;
        }

        public override RestApiSettings SaveToNewModel()
        {
            return new RestApiSettings()
            {
                IsEnabled = IsEnabled,
                IsSwaggerEnabled = IsSwaggerEnabled,
                Port = Port
            };
        }
    }
}
