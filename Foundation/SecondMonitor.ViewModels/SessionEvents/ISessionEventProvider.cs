namespace SecondMonitor.ViewModels.SessionEvents
{
    using System;
    using System.Collections.Generic;
    using Contracts.SimSettings;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;

    using SecondMonitor.DataModel.Snapshot.Systems;

    public interface ISessionEventProvider
    {
        event EventHandler<DataSetArgs> SessionTypeChange;
        event EventHandler<DataSetArgs> PlayerFinishStateChanged;
        event EventHandler<DriversArgs> DriversAdded;
        event EventHandler<DriversArgs> DriversRemoved;
        event EventHandler<DataSetArgs> TrackChanged;
        event EventHandler<DataSetArgs> PlayerPropertiesChanged;
        event EventHandler<DataSetArgs> SimulatorChanged;
        event EventHandler<DataSetArgs> FlagStateChanged;
        event EventHandler<DataSetArgs> PlayerLeftSession;

        event EventHandler<DataSetArgs> OnNewDataSet;
        SimulatorDataSet LastDataSet { get; }
        SimulatorDataSet LastDataSetWithDrivers { get; }

        SimulatorDataSet BeforeLastDataSet { get; }

        ISimSettings CurrentSimulatorSettings { get; }
        string CurrentCarName { get; }

        CarInfo CurrentCarInfo { get; }

        string LastTrackFullName { get; }
        string CurrentSimName { get; }

        void NotifySessionTypeChanged(SimulatorDataSet dataSet);
        void NotifyPlayerFinishStateChanged(SimulatorDataSet dataSet);
        void NotifyDriversAdded(SimulatorDataSet currentDataSet, SimulatorDataSet previousDataSet,  IEnumerable<DriverInfo> drivers);
        void NotifyDriversRemoved(SimulatorDataSet currentDataSet, SimulatorDataSet previousDataSet,  IEnumerable<DriverInfo> drivers);
        void NotifyTrackChanged(SimulatorDataSet dataSet);
        void NotifyPlayerPropertiesChanged(SimulatorDataSet dataSet);
        void NotifySimulatorChanged(SimulatorDataSet dataSet);
        void NotifyFlagStateChanged(SimulatorDataSet dataSet);

        void SetLastDataSet(SimulatorDataSet dataSet);

        void SetCurrentSimulatorSettings(ISimSettings simSettings);

        bool IsConnectedToSim();

        void NotifyPlayerLeftSession(SimulatorDataSet dataSet);
    }
}