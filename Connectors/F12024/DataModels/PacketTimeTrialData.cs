namespace SecondMonitor.F12024Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketTimeTrialData
    {
        public PacketHeader MHeader; // Header

        public TimeTrialDataSet PlayerSessionBestDataSet; // Player session best data set
        public TimeTrialDataSet PersonalBestDataSet; // Personal best data set
        public TimeTrialDataSet RivalDataSet; // Rival data set
    }
}
