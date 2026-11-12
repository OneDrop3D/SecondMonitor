namespace SecondMonitor.Contracts.NInject
{
    using System;
    using Ninject.Activation;
    using Ninject.Parameters;
    using Ninject.Planning.Targets;

    public class TypeMatchingConstructorArgument<TType> : IConstructorArgument
    {
        private readonly TypeMatchingConstructorArgument _decorate;
        public TypeMatchingConstructorArgument(Func<IContext, ITarget, TType> valueCallback)
        {
            _decorate = new TypeMatchingConstructorArgument(typeof(TType), (context, target) => valueCallback(context, target));
        }

        public string Name => _decorate.Name;
        public bool ShouldInherit => _decorate.ShouldInherit;

        public bool Equals(IParameter other)
        {
            return _decorate.Equals(other);
        }

        public virtual object GetValue(IContext context, ITarget target)
        {
            return _decorate.GetValue(context, target);
        }

        public bool AppliesToTarget(IContext context, ITarget target)
        {
            return _decorate.AppliesToTarget(context, target);
        }
    }
}