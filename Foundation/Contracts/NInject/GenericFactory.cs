namespace SecondMonitor.Contracts.NInject
{
    using System;
    using System.Collections.Generic;
    using Ninject;
    using Ninject.Parameters;
    using Ninject.Syntax;

    public abstract class GenericFactory<TFactory>
    {
        private IParameter[] _constructorParameters;

        protected GenericFactory(IResolutionRoot resolutionRoot, params IParameter[] constructorParameters)
        {
            ResolutionRoot = resolutionRoot;
            _constructorParameters = constructorParameters;
        }

        protected IResolutionRoot ResolutionRoot { get; }

        protected IParameter[] ConstructorParameters => _constructorParameters;

        public T Create<T>()
        {
            T newInstance = ResolutionRoot.Get<T>(_constructorParameters);
            _constructorParameters = Array.Empty<IParameter>();
            return newInstance;
        }

        public IEnumerable<T> CreateAll<T>()
        {
            IEnumerable<T> newInstances = ResolutionRoot.GetAll<T>(_constructorParameters);
            _constructorParameters = Array.Empty<IParameter>();
            return newInstances;
        }

        public abstract INamedConstructorParameter<TFactory> Set(string parameterName);

        public abstract ITypedConstructorParameter<TFactory, T> Set<T>();
    }
}