namespace SecondMonitor.ViewModels.SessionEvents
{
    using System;
    using System.Collections.Generic;
    using Contracts.SimSettings;
    using DataModel;
    using DataModel.Snapshot;
    using DataModel.Snapshot.Drivers;

    using SecondMonitor.DataModel.Snapshot.Systems;

    public class SessionEventProvider : ISessionEventProvider
    {
        public event EventHandler<DataSetArgs> SessionTypeChange;
        public event EventHandler<DataSetArgs> PlayerFinishStateChanged;
        public event EventHandler<DriversArgs> DriversAdded;
        public event EventHandler<DriversArgs> DriversRemoved;
        public event EventHandler<DataSetArgs> TrackChanged;
        public event EventHandler<DataSetArgs> PlayerPropertiesChanged;
        public event EventHandler<DataSetArgs> SimulatorChanged;
        public event EventHandler<DataSetArgs> FlagStateChanged;
        public event EventHandler<DataSetArgs> PlayerLeftSession;
        public event EventHandler<DataSetArgs> OnNewDataSet;

        public SimulatorDataSet LastDataSet { get; private set; }
        public SimulatorDataSet LastDataSetWithDrivers { get;  private set; }
        public SimulatorDataSet BeforeLastDataSet { get; private set; }

        public ISimSettings CurrentSimulatorSettings { get; private set; }

        public string CurrentCarName { get; private set; }

        public CarInfo CurrentCarInfo { get; private set; }

        public string LastTrackFullName { get; private set; }
        public string CurrentSimName { get; private set; }

        public void NotifySessionTypeChanged(SimulatorDataSet dataSet)
        {
            SessionTypeChange?.Invoke(this, new DataSetArgs(dataSet));
        }

        public void NotifyPlayerFinishStateChanged(SimulatorDataSet dataSet)
        {
            PlayerFinishStateChanged?.Invoke(this, new DataSetArgs(dataSet));
        }

        public void NotifyDriversAdded(SimulatorDataSet currentDataSet, SimulatorDataSet previousDataSet, IEnumerable<DriverInfo> drivers)
        {
            DriversAdded?.Invoke(this, new DriversArgs(currentDataSet, previousDataSet, drivers));
        }

        public void NotifyDriversRemoved(SimulatorDataSet currentDataSet, SimulatorDataSet previousDataSet, IEnumerable<DriverInfo> drivers)
        {
            DriversRemoved?.Invoke(this, new DriversArgs(currentDataSet, previousDataSet,  drivers));
        }

        public void NotifyTrackChanged(SimulatorDataSet dataSet)
        {
            LastTrackFullName = dataSet.SessionInfo.TrackInfo.TrackFullName;
            TrackChanged?.Invoke(this, new DataSetArgs(dataSet));
        }

        public void NotifyPlayerPropertiesChanged(SimulatorDataSet dataSet)
        {
            CurrentCarName = dataSet.PlayerInfo.CarName;
            CurrentCarInfo = dataSet.PlayerInfo.CarInfo;
            PlayerPropertiesChanged?.Invoke(this, new DataSetArgs(dataSet));
        }

        public void NotifySimulatorChanged(SimulatorDataSet dataSet)
        {
            CurrentSimName = dataSet.Source;
            SimulatorChanged?.Invoke(this, new DataSetArgs(dataSet));
        }

        public void NotifyFlagStateChanged(SimulatorDataSet dataSet)
        {
            FlagStateChanged?.Invoke(this, new DataSetArgs(dataSet));
        }

        public void SetLastDataSet(SimulatorDataSet dataSet)
        {
            BeforeLastDataSet = LastDataSet;
            LastDataSet = dataSet;
            if (dataSet.DriversInfo.Length > 0)
            {
                LastDataSetWithDrivers = dataSet;
            }

            OnNewDataSet?.Invoke(this, new DataSetArgs(dataSet));
        }

        public void SetCurrentSimulatorSettings(ISimSettings simSettings)
        {
            CurrentSimulatorSettings = simSettings;
        }

        public bool IsConnectedToSim()
        {
            return !string.IsNullOrWhiteSpace(CurrentSimName) && !SimulatorsNameMap.IsNotConnected(CurrentSimName);
        }

        public void NotifyPlayerLeftSession(SimulatorDataSet dataSet)
        {
            PlayerLeftSession?.Invoke(this, new DataSetArgs(dataSet));
        }
    }
}