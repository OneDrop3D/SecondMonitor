namespace SecondMonitor.F12021Connector.DataModels
{
    internal class AllPacketsComposition
    {
        public PacketCarSetupData PacketCarSetupData;
        public PacketCarStatusData PacketCarStatusData;
        public PacketCarTelemetryData PacketCarTelemetryData;
        public PacketParticipantsData PacketParticipantsData;
        public PacketLapData PacketLapData;
        public PacketMotionData PacketMotionData;
        public PacketSessionData PacketSessionData;
        public AdditionalData AdditionalData = new AdditionalData();
        public PacketCarDamageData PacketCarDamageData;
        public PacketSessionHistoryData[] PacketSessionHistoryData;

        public AllPacketsComposition()
        {
            PacketSessionHistoryData = new PacketSessionHistoryData[22];
        }
    }
}