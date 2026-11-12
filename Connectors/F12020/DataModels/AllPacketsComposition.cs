namespace SecondMonitor.F12020Connector.DataModels
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
    }
}