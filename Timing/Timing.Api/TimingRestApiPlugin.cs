namespace SecondMonitor.Timing.Api
{
    using Ninject.Syntax;

    using NLog;
    using NLog.Extensions.Logging;

    using SecondMonitor.Timing.Common.TimingPlugin;
    using SecondMonitor.ViewModels.Settings;

    public class TimingRestApiPlugin(IResolutionRoot resolutionRoot, ISettingsProvider settingsProvider) : ITimingPlugin
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public Task StartControllerAsync()
        {
            TimingApplicationProxy.Initialize(resolutionRoot);
            StartWebServer();
            return Task.CompletedTask;
        }

        private void StartWebServer()
        {
            try
            {
                if (settingsProvider == null || !settingsProvider.DisplaySettingsViewModel.RestApiSettingsViewModel.IsEnabled)
                {
                    return;
                }

                NLogProviderOptions nlpopts = new NLogProviderOptions
                {
                    IgnoreEmptyEventId = true,
                    CaptureMessageTemplates = true,
                    CaptureMessageProperties = true,
                    ParseMessageTemplates = true,
                    IncludeScopes = true,
                    ShutdownOnDispose = true
                };

                WebApplicationBuilder builder = WebApplication.CreateBuilder();
                builder.WebHost.ConfigureKestrel(serverOptions => serverOptions.ListenAnyIP(settingsProvider.DisplaySettingsViewModel.RestApiSettingsViewModel.Port));
                builder.Services.AddLogging(x =>
                {
                    x.ClearProviders();
                    x.AddNLog(nlpopts);
                });

                // Add services to the container.
                builder.Services.AddControllers().AddApplicationPart(typeof(TimingRestApiPlugin).Assembly);
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();

                // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

                var app = builder.Build();

                /*// Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {*/
                if (settingsProvider.DisplaySettingsViewModel.RestApiSettingsViewModel.IsSwaggerEnabled)
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                /*}*/

                app.UseAuthorization();
                app.MapControllers();

                app.RunAsync();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        public Task StopControllerAsync()
        {
            return Task.CompletedTask;
        }
    }
}
