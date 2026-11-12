namespace SecondMonitor.Remote.Adapter
{
    using DataModel.Snapshot;
    using Models;

    public interface IDatagramPayloadPacker
    {
        bool IsMinimalPackageDelayPassed();
        DatagramPayload CreateHandshakeDatagramPayload();
        DatagramPayload CreateHearthBeatDatagramPayload();
        DatagramPayload CreateRegularDatagramPayload(SimulatorDataSet simulatorData);
        DatagramPayload CreateSessionStartedPayload(SimulatorDataSet simulatorData);
    }
}