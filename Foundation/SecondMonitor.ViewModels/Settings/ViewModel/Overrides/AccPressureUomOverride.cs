namespace SecondMonitor.ViewModels.Settings.ViewModel.Overrides
{
    using System;
    using DataModel;
    using DataModel.BasicProperties;
    using SessionEvents;

    public class AccPressureUomOverride : IUomOverride, IDisposable
    {
        private readonly ISessionEventProvider _sessionEventProvider;
        private bool _isActive;

        public AccPressureUomOverride(ISessionEventProvider sessionEventProvider)
        {
            _sessionEventProvider = sessionEventProvider;
            sessionEventProvider.SimulatorChanged += SessionEventProviderOnSimulatorChanged;
        }

        public event EventHandler<ActivationStateEventArgs> ActivationStateChanged;

        public PressureUnits GetPressureUnits(DisplaySettingsViewModel displaySettingsViewModel, PressureUnits preferredPressureUnits)
        {
            if (_isActive && displaySettingsViewModel.OverrideToPsiInAcc)
            {
                return PressureUnits.Psi;
            }

            return preferredPressureUnits;
        }

        private void SessionEventProviderOnSimulatorChanged(object sender, DataSetArgs e)
        {
            switch (_isActive)
            {
                case true when e.DataSet.Source != SimulatorsNameMap.AccSimName:
                    _isActive = false;
                    ActivationStateChanged?.Invoke(this, new ActivationStateEventArgs(false));
                    return;
                case false when e.DataSet.Source == SimulatorsNameMap.AccSimName:
                    _isActive = true;
                    ActivationStateChanged?.Invoke(this, new ActivationStateEventArgs(true));
                    break;
            }
        }

        public void Dispose()
        {
            _sessionEventProvider.SimulatorChanged -= SessionEventProviderOnSimulatorChanged;
        }
    }
}