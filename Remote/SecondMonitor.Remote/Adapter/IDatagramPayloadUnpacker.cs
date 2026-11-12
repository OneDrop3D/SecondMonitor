namespace SecondMonitor.Remote.Adapter
{
    using DataModel.Snapshot;
    using Models;

    public interface IDatagramPayloadUnPacker
    {
        SimulatorDataSet UnpackDatagramPayload(DatagramPayload datagramPayload);
    }
}