namespace SecondMonitor.Timing.Application.LapTimings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using Common.SessionTiming;
    using Common.SessionTiming.Drivers;
    using Common.SessionTiming.Drivers.Lap;

    using SecondMonitor.DataModel.Extensions;

    using ViewModel;
    using ViewModels;
    using ViewModels.Controllers;
    using ViewModels.Factory;

    public class DriverDetailsController : IController
    {
        private readonly IWindowService _windowService;
        private readonly IViewModelFactory _viewModelFactory;
        private readonly Common.SessionTiming.ILapEventProvider _lapEventProvider;
        private readonly Dictionary<string, DriverLapWindowCacheItem> _openedWindows;

        public DriverDetailsController(IWindowService windowService, IViewModelFactory viewModelFactory, Common.SessionTiming.ILapEventProvider lapEventProvider)
        {
            _windowService = windowService;
            _viewModelFactory = viewModelFactory;
            _lapEventProvider = lapEventProvider;
            _openedWindows = new Dictionary<string, DriverLapWindowCacheItem>();
        }

        public void OpenWindowDefault(DriverTiming driverTiming)
        {
            OpenWindow(driverTiming);
        }

        public void Rebind(DriverTiming driverTiming)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => Rebind(driverTiming));
                return;
            }

            if (!_openedWindows.TryGetValue(driverTiming.DriverId, out var item))
            {
                return;
            }

            item.DriverTiming = driverTiming;
            UnSubscribeOnDriver(item.DriverTiming);
            SubscribeOnDriver(driverTiming);
            item.DriverLapsViewModel.DriverTiming = driverTiming;

            //_openedWindows.FindAll(p => ((DriverLapsViewModel)p.DataContext).DriverTiming.DriverShortName == driverTiming.DriverShortName).ForEach(p => Rebind(p, driverTiming));
        }

        public void RebindAll(IEnumerable<DriverTiming> driverTimings)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => RebindAll(driverTimings));
                return;
            }

            driverTimings.ForEach(Rebind);
        }

        public Task StartControllerAsync()
        {
            _lapEventProvider.BestLapChanged += LapEventProviderOnLapChanged;
            _lapEventProvider.BestClassLapChanged += LapEventProviderOnLapChanged;
            _lapEventProvider.BestSectorChanged += LapEventProviderOnSectorChanged;
            _lapEventProvider.BestSectorClassChanged += LapEventProviderOnSectorChanged;
            _lapEventProvider.BestLapPersonalChanged += DriverTimingOnLapChanged;
            return Task.CompletedTask;
        }

        public Task StopControllerAsync()
        {
            _lapEventProvider.BestLapChanged -= LapEventProviderOnLapChanged;
            _lapEventProvider.BestClassLapChanged -= LapEventProviderOnLapChanged;
            _lapEventProvider.BestSectorChanged -= LapEventProviderOnSectorChanged;
            _lapEventProvider.BestSectorClassChanged -= LapEventProviderOnSectorChanged;
            _lapEventProvider.BestLapPersonalChanged -= DriverTimingOnLapChanged;
            return Task.CompletedTask;
        }

        private void OpenWindow(DriverTiming driverTiming)
        {
            if (driverTiming == null)
            {
                return;
            }

            if (_openedWindows.TryGetValue(driverTiming.DriverId, out DriverLapWindowCacheItem cacheItem))
            {
                cacheItem.Window.Focus();
                return;
            }

            DriverLapsViewModel driverLapsViewModel = _viewModelFactory.Set("driverTiming").To(driverTiming).Create<DriverLapsViewModel>();
            Window newWindow = _windowService.OpenWindow(driverLapsViewModel, driverTiming.DriverLongName, WindowState.Normal, SizeToContent.WidthAndHeight, WindowStartupLocation.CenterOwner);

            _openedWindows.Add(driverTiming.DriverId, new DriverLapWindowCacheItem(newWindow, driverTiming, driverLapsViewModel));
            SubscribeOnDriver(driverTiming);
            newWindow.Closed += DriverLapsWindowOnClosed;
            newWindow.Show();
        }

        private void LapEventProviderOnLapChanged(object sender, BestLapChangedArgs e)
        {
            RefreshAllLaps();
        }

        private void LapEventProviderOnSectorChanged(object sender, BestSectorChangedArgs e)
        {
            RefreshAllLaps();
        }

        private void RefreshAllLaps()
        {
            _openedWindows.Values.ForEach(x => x.DriverLapsViewModel.RefreshAllLaps());
        }

        private void DriverLapsWindowOnClosed(object sender, EventArgs e)
        {
            foreach (var item in _openedWindows.Where(x => x.Value.Window == sender).ToList())
            {
                UnSubscribeOnDriver(item.Value.DriverTiming);
                _openedWindows.Remove(item.Key);
            }
        }

        private void SubscribeOnDriver(DriverTiming driverTiming)
        {
            driverTiming.NewLapStarted += DriverTimingOnNewLapStarted;
        }

        private void UnSubscribeOnDriver(DriverTiming driverTiming)
        {
            driverTiming.NewLapStarted -= DriverTimingOnNewLapStarted;
        }

        private void DriverTimingOnLapChanged(object sender, BestLapChangedArgs e)
        {
            if (!_openedWindows.TryGetValue(e.NewLap.Driver.DriverId, out DriverLapWindowCacheItem item))
            {
                return;
            }

            item.DriverLapsViewModel.RefreshAllLaps();
        }

        private void DriverTimingOnNewLapStarted(object sender, LapEventArgs e)
        {
            if (!_openedWindows.TryGetValue(e.Lap.Driver.DriverId, out DriverLapWindowCacheItem item))
            {
                return;
            }

            item.DriverLapsViewModel.AddLap(e.Lap);
        }
    }
}