namespace SecondMonitor.F12024Connector.DataModels
{
    internal class AllPacketsComposition
    {
        public PacketCarSetupData PacketCarSetupData;
        public PacketCarStatusData PacketCarStatusData;
        public PacketCarTelemetryData PacketCarTelemetryData;
        public PacketParticipantsData PacketParticipantsData;
        public PacketLapData PacketLapData;
        public PacketMotionData PacketMotionData;
        public PacketMotionExData PacketMotionExData;
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