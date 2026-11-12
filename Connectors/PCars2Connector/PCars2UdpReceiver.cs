namespace SecondMonitor.PCars2Connector
{
    using System;
    using System.Linq;
    using System.Net.Sockets;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Foundation.Connectors.Udp;
    using SecondMonitor.PluginsConfiguration.Common.DataModel;
    using SharedMemory;

    public class PCars2UdpReceiver
    {
        private readonly UdpReceiver _udpReceiver;
        public PCars2UdpReceiver(PCars2Configuration configuration)
        {
            _udpReceiver = new UdpReceiver(configuration.Port);
        }

        public async Task ReceiveLoop(Action<UDPTelemetryData> handler, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                UdpReceiveResult receiveResult = await _udpReceiver.Receive(cancellationToken);
                if (!IsCorrectPackageType(receiveResult.Buffer))
                {
                    continue;
                }

                UDPTelemetryData telemetryData = Deserialize(receiveResult.Buffer);
                handler(telemetryData);
            }
        }

        private bool IsCorrectPackageType(byte[] rawData)
        {
            UdpPacketType packetType = (UdpPacketType)rawData[10];
            return packetType == UdpPacketType.eCarPhysics;
        }

        private UDPTelemetryData Deserialize(byte[] rawData)
        {
            GCHandle telemetryHandle = GCHandle.Alloc(rawData.ToArray(), GCHandleType.Pinned);
            try
            {
                UDPTelemetryData udpTelemetry = (UDPTelemetryData)Marshal.PtrToStructure(telemetryHandle.AddrOfPinnedObject(), typeof(UDPTelemetryData));
                return udpTelemetry;
            }
            finally
            {
                telemetryHandle.Free();
            }
        }
    }
}