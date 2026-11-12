namespace SecondMonitor.Contracts.NInject
{
    using System;
    using System.Collections.Generic;
    using Ninject.Activation;
    using Ninject.Parameters;
    using Ninject.Planning.Targets;

    public class PassThroughConstructorArgument : TypeMatchingConstructorArgument<IParameter[]>, IDisposable
    {
        private readonly List<IParameter> _parameters;
        public PassThroughConstructorArgument(params IParameter[] parameters) : base(((context, target) => parameters))
        {
            _parameters = new List<IParameter>();
        }

        public override object GetValue(IContext context, ITarget target)
        {
            return _parameters.ToArray();
        }

        public void Add(IParameter value)
        {
            _parameters.Add(value);
        }

        public void AddRange(IEnumerable<IParameter> values)
        {
            _parameters.AddRange(values);
        }

        public void Dispose()
        {
            _parameters.Clear();
        }
    }
}