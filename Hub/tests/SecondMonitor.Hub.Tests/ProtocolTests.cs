namespace SecondMonitor.Hub.Tests;

using System.Diagnostics;
using NUnit.Framework;
using ProtoBuf;
using SecondMonitor.DataModel.Snapshot;
using SecondMonitor.Hub.Core.Demo;
using SecondMonitor.Hub.Core.Network;
using SecondMonitor.Remote.Models;

[TestFixture]
public class ProtocolTests
{
    [Test]
    public void DatagramPayload_RoundTrips_WithProtobuf()
    {
        var sim = new DemoRaceSimulator();
        SimulatorDataSet data = null;
        for (int i = 0; i < 300; i++)
        {
            data = sim.Step(0.5);
        }

        var payload = new DatagramPayload
        {
            ContainsOtherDriversTiming = true,
            ContainsPlayersTiming = true,
            ContainsSimulatorSourceInfo = true,
            DriversInfo = data.DriversInfo,
            PlayerInfo = data.PlayerInfo,
            LeaderInfo = data.LeaderInfo,
            SessionInfo = data.SessionInfo,
            InputInfo = data.InputInfo,
            Source = data.Source,
            SimulatorSourceInfo = data.SimulatorSourceInfo,
        };

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, payload);
        ms.Position = 0;
        var back = Serializer.Deserialize<DatagramPayload>(ms);

        Assert.That(back.DriversInfo, Has.Length.EqualTo(16));
        Assert.That(back.SessionInfo.TrackInfo.LayoutLength.InMeters, Is.EqualTo(5200).Within(0.01));
        var player = back.DriversInfo.Single(d => d.IsPlayer);
        Assert.That(player.CarInfo.FuelSystemInfo.FuelRemaining.InLiters, Is.EqualTo(data.PlayerInfo.CarInfo.FuelSystemInfo.FuelRemaining.InLiters).Within(1e-6));
        Assert.That(player.CarInfo.WheelsInfo.FrontLeft.TyreType, Is.Not.Empty);
        Assert.That(back.SessionInfo.SessionTime, Is.EqualTo(data.SessionInfo.SessionTime));
    }

    [Test]
    public void Receiver_GetsData_FromWindowsStyleServer_ByManualAddress()
    {
        const int port = 52711;
        using var server = new DemoBroadcastServer();
        Assert.That(server.Start(port), Is.True);

        using var receiver = new RemoteReceiver(port, new[] { "127.0.0.1" });
        int received = 0;
        DatagramPayloadKind? firstKind = null;
        receiver.DataReceived += (d, k) =>
        {
            firstKind ??= k;
            Interlocked.Increment(ref received);
        };
        receiver.Start();

        var sim = new DemoRaceSimulator();
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < TimeSpan.FromSeconds(10) && Volatile.Read(ref received) < 5)
        {
            server.Poll();
            server.Send(sim.Step(0.1));
            Thread.Sleep(20);
        }

        Assert.That(received, Is.GreaterThanOrEqualTo(5), "Aucune donnée reçue du serveur de démonstration");
        Assert.That(firstKind, Is.EqualTo(DatagramPayloadKind.SessionStart));
        Assert.That(receiver.GetStatus().IsConnected, Is.True);
    }
}
