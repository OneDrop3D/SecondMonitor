namespace SecondMonitor.Timing.Application.Controllers
{
    using DataModel.Snapshot;

    using SecondMonitor.DataModel.Extensions;

    using ViewModels.Settings;

    public class ForceSingleClassVisitor : ISimulatorDataSetVisitor
    {
        private readonly ISettingsProvider _settingsProvider;

        public ForceSingleClassVisitor(ISettingsProvider settingsProvider)
        {
            _settingsProvider = settingsProvider;
        }

        public void Visit(SimulatorDataSet simulatorDataSet)
        {
            if (_settingsProvider.DisplaySettingsViewModel is not { IsForceSingleClassEnabled: true } || !simulatorDataSet.SessionInfo.IsMultiClass)
            {
                return;
            }

            simulatorDataSet.SessionInfo.IsMultiClass = false;
            simulatorDataSet.DriversInfo.ForEach(x =>
            {
                x.PositionInClass = x.Position;
            });
        }

        public void Reset()
        {
        }
    }
}