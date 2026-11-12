namespace SecondMonitor.F12024Connector
{
    using System;
    using System.Linq;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.F12024Connector.DataModels;

    internal class DataSetCompositor
    {
        private readonly Action<AllPacketsComposition> _sessionStartedHandler;
        private readonly Action<AllPacketsComposition> _newDataHandler;

        private bool _resetWhenData;
        private bool _isPacketCarSetupDataFilled;
        private bool _isPacketCarStatusDataFilled;
        private bool _isPacketCarTelemetryDataFilled;
        private bool _isPacketParticipantsDataFilled;
        private bool _isPacketLapDataFilled;
        private bool _isPacketMotionDataFilled;
        private bool _isPacketSessionDataFilled;
        private bool _isPacketCarDamageDataFilled;
        private bool _isPacketMotionExDataFilled;

        public DataSetCompositor(Action<AllPacketsComposition> sessionStartedHandler, Action<AllPacketsComposition> newDataHandler)
        {
            _sessionStartedHandler = sessionStartedHandler;
            _newDataHandler = newDataHandler;
            AllPacketsComposition = new AllPacketsComposition();
            _resetWhenData = true;
        }

        private AllPacketsComposition AllPacketsComposition { get; }

        private bool IsAllPacketsFilled =>
            _isPacketCarSetupDataFilled &&
            _isPacketCarStatusDataFilled &&
            _isPacketCarTelemetryDataFilled &&
            _isPacketParticipantsDataFilled &&
            _isPacketLapDataFilled &&
            _isPacketMotionDataFilled &&
            _isPacketSessionDataFilled &&
            _isPacketCarDamageDataFilled &&
            _isPacketMotionExDataFilled;

        private void Reset()
        {
            _isPacketCarSetupDataFilled = false;
            _isPacketCarStatusDataFilled = false;
            _isPacketCarTelemetryDataFilled = false;
            _isPacketParticipantsDataFilled = false;
            _isPacketLapDataFilled = false;
            _isPacketMotionDataFilled = false;
            _isPacketSessionDataFilled = false;
            _isPacketCarDamageDataFilled = false;
            _isPacketMotionExDataFilled = false;
            AllPacketsComposition.AdditionalData.RetiredDrivers = Enumerable.Repeat(false, 22).ToArray();
            _resetWhenData = true;
        }

        internal void ProcessPacket(PacketParticipantsData packetParticipantsData)
        {
            AllPacketsComposition.PacketParticipantsData = packetParticipantsData;
            _isPacketParticipantsDataFilled = true;
        }

        internal void ProcessPacket(PacketEventData packetEventData)
        {
            string eventCode = packetEventData.MEventStringCode.FromArray();
            switch (eventCode)
            {
                case "SCAR":
                    AllPacketsComposition.AdditionalData.LastSafetyCarUpdate = packetEventData.MEventDetails.SafetyCar;
                    return;
                case "STLG" when packetEventData.MEventDetails.StartLights.NumLights == 1:
                case "SSTA" or "SEND":
                    Reset();
                    return;
                case "RTMT":
                    AllPacketsComposition.AdditionalData.RetiredDrivers[packetEventData.MEventDetails.Retirement.vehicleIdx] = true;
                    return;
            }
        }

        internal void ProcessPacket(PacketCarSetupData packetCarSetupData)
        {
            AllPacketsComposition.PacketCarSetupData = packetCarSetupData;
            _isPacketCarSetupDataFilled = true;
        }

        public void ProcessPacket(PacketMotionData packetMotionData)
        {
            AllPacketsComposition.PacketMotionData = packetMotionData;
            _isPacketMotionDataFilled = true;
        }

        internal void ProcessPacket(PacketCarTelemetryData packetCarTelemetryData)
        {
            AllPacketsComposition.PacketCarTelemetryData = packetCarTelemetryData;
            _isPacketCarTelemetryDataFilled = true;
        }

        internal void ProcessPacket(PacketCarStatusData packetCarStatusData)
        {
            AllPacketsComposition.PacketCarStatusData = packetCarStatusData;
            _isPacketCarStatusDataFilled = true;
        }

        internal void ProcessPacket(PacketSessionData packetSessionData)
        {
            AllPacketsComposition.PacketSessionData = packetSessionData;
            _isPacketSessionDataFilled = true;
        }

        internal void ProcessPacked(PacketCarDamageData packetCarDamageData)
        {
            AllPacketsComposition.PacketCarDamageData = packetCarDamageData;
            _isPacketCarDamageDataFilled = true;
        }

        public void ProcessPacked(PacketSessionHistoryData packetSessionHistoryData)
        {
            AllPacketsComposition.PacketSessionHistoryData[packetSessionHistoryData.CarIdx] = packetSessionHistoryData;
        }

        public void ProcessPacked(PacketMotionExData packetMotionExData)
        {
            AllPacketsComposition.PacketMotionExData = packetMotionExData;
            _isPacketMotionExDataFilled = true;
        }

        internal void ProcessPacket(PacketLapData packetLapData)
        {
            AllPacketsComposition.PacketLapData = packetLapData;
            _isPacketLapDataFilled = true;
            HandleCompletePacket();
        }

        private void HandleCompletePacket()
        {
            if (!IsAllPacketsFilled)
            {
                return;
            }

            if (_resetWhenData)
            {
                _sessionStartedHandler(AllPacketsComposition);
                _resetWhenData = false;
                return;
            }

            _newDataHandler(AllPacketsComposition);
        }
    }
}