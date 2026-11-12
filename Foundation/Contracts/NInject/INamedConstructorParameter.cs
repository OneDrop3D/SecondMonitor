namespace SecondMonitor.Contracts.NInject
{
    using Fluent;

    public interface INamedConstructorParameter<out TFactory> : IFluentSyntax
    {
        TFactory To(object instance);
    }
}