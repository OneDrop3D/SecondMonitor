namespace SecondMonitor.Foundation.Connectors
{
    using System;
    using SecondMonitor.DataModel.Snapshot;

    public class DataEventArgs : EventArgs
    {
        public DataEventArgs(SimulatorDataSet data)
        {
            this.Data = data;
        }

        public SimulatorDataSet Data
        {
            get;
            set;
        }
    }
}