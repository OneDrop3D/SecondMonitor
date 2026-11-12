namespace SecondMonitor.ForzaMotorsportConnector
{
    using System;
    using System.Linq;
    using System.Net.Sockets;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Foundation.Connectors.Udp;

    using SecondMonitor.ForzaMotorsportConnector.DataModels;
    using SecondMonitor.PluginsConfiguration.Common.DataModel;

    public class ForzaMotorsportUdpReceiver
    {
        private readonly UdpReceiver _udpReceiver;
        public ForzaMotorsportUdpReceiver(ForzaMotorsportConfiguration configuration)
        {
            _udpReceiver = new UdpReceiver(configuration.Port);
        }

        public async Task ReceiveLoop(Action<ForzaMotorsportUDPTelemetryData> handler, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                UdpReceiveResult receiveResult = await _udpReceiver.Receive(cancellationToken).ConfigureAwait(false);
                if (!IsCorrectPackageType(receiveResult.Buffer))
                {
                    continue;
                }

                ForzaMotorsportUDPTelemetryData telemetryData = Deserialize(receiveResult.Buffer);
                handler(telemetryData);
            }
        }

        public bool IsPackageReceived()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            try
            {
                UdpReceiveResult result = _udpReceiver.Receive(cts.Token).GetAwaiter().GetResult();
                return IsCorrectPackageType(result.Buffer);
            }
            catch
            {
                return false;
            }
        }

        private bool IsCorrectPackageType(byte[] rawData)
        {
            return rawData.Length == 331;
        }
        
        private ForzaMotorsportUDPTelemetryData Deserialize(byte[] rawData)
        {
            GCHandle telemetryHandle = GCHandle.Alloc(rawData.ToArray(), GCHandleType.Pinned);
            try
            {
                ForzaMotorsportUDPTelemetryData udpTelemetry = (ForzaMotorsportUDPTelemetryData)Marshal.PtrToStructure(telemetryHandle.AddrOfPinnedObject(), typeof(ForzaMotorsportUDPTelemetryData));
                return udpTelemetry;
            }
            finally
            {
                telemetryHandle.Free();
            }
        }
    }
}