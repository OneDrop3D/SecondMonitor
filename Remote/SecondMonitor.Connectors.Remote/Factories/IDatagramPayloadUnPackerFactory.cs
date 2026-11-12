namespace SecondMonitor.Connectors.Remote.Factories
{
    using SecondMonitor.Remote.Adapter;

    public interface IDatagramPayloadUnPackerFactory
    {
        IDatagramPayloadUnPacker Create();
    }
}