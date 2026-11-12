namespace SecondMonitor.Timing.Application.AutoDelete
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.Timing.Application.AutoDelete.Deleters;
    using SecondMonitor.ViewModels.Controllers;
    using SecondMonitor.ViewModels.Settings;

    public class AutoDeleteController : IController
    {
        private readonly ISettingsProvider _settingsProvider;
        private readonly IList<IAutoDeleter> _autoDeleters;
        private readonly CancellationTokenSource _autoDeletersCancellationTokenSource;
        private Task _autoDeleteTask;

        public AutoDeleteController(ISettingsProvider settingsProvider, IList<IAutoDeleter> autoDeleters)
        {
            _autoDeletersCancellationTokenSource = new CancellationTokenSource();
            _settingsProvider = settingsProvider;
            _autoDeleters = autoDeleters;
        }

        public Task StartControllerAsync()
        {
            _autoDeleters.ForEach(x => x.CheckSettingsAndSetDefaults(_settingsProvider.DisplaySettingsViewModel.SimsAutoDeleteSettingsViewModel));
            _autoDeleteTask = Task.Delay(10000, _autoDeletersCancellationTokenSource.Token).ContinueWith(_ => DeleteObsoleteFiles(_autoDeletersCancellationTokenSource.Token));
            return Task.CompletedTask;
        }

        private async Task DeleteObsoleteFiles(CancellationToken cancellationToken)
        {
            await Task.Run(() => _autoDeleters.ForEach(x => x.PerformAutoDelete()), cancellationToken);
        }

        public async Task StopControllerAsync()
        {
            if (_autoDeleteTask == null)
            {
                return;
            }

            if (!_autoDeleteTask.IsCompleted)
            {
                _autoDeletersCancellationTokenSource.Cancel();
            }

            try
            {
                await _autoDeleteTask;
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
