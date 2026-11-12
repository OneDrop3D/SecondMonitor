namespace SecondMonitor.ViewModels.Track.MapView
{
    using System.Collections.Generic;
    using SecondMonitor.Contracts.Session;
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.DataModel.Snapshot.Drivers;

    public interface ISituationOverviewViewModel : IViewModel
    {
        IMapSidePanelViewModel MapSidePanelViewModel { get; set; }

        bool AnimateDrivers { get; set; }
        bool ShowMultiClassIndicator { get; set; }

        bool IsPitReturnPredictionVisible { get; set; }

        int DriversUpdatedPerTick { get; set; }

        List<DriverInfo> Update(SimulatorDataSet simulatorDataSet, ISessionInformationProvider sessionInformationProvider, bool usePositionInClass);

        void UpdateCustomOutline(string driverLongName, ColorDto outlineColor);

        void RemoveAllDrivers();

        void RemoveDriver(IDriverInfo driver);

        void AddDriver(IDriverInfo driver);

        void AddDriver(IDriverInfo driver, ColorDto customOutline);

        void UpdatePitStopReturnPrediction(PitReturnPrediction pitReturnPrediction, bool usePositionInClass);
    }
}