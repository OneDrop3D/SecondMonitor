namespace SecondMonitor.Contracts.NInject
{
    using Fluent;

    public interface ITypedConstructorParameter<out TResult, in T> : IFluentSyntax
    {
        TResult To(T instance);
    }
}