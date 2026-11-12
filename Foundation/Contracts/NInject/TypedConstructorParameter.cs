namespace SecondMonitor.Contracts.NInject
{
    using System.Collections.Generic;
    using Ninject;
    using Ninject.Parameters;
    using Ninject.Syntax;

    public class TypedConstructorParameter<TResult, TFactory, T> : ITypedConstructorParameter<TResult, T> where TFactory : TResult
    {
        private readonly IResolutionRoot _resolutionRoot;
        private readonly List<IParameter> _allParameters;

        public TypedConstructorParameter(IResolutionRoot resolutionRoot, params IParameter[] previousParameters)
        {
            _resolutionRoot = resolutionRoot;
            _allParameters = new List<IParameter>(previousParameters);
        }

        public TResult To(T instance)
        {
            _allParameters.Add(new TypeMatchingConstructorArgument<T>((context, target) => instance));
            using (PassThroughConstructorArgument passThroughParameter = new PassThroughConstructorArgument())
            {
                passThroughParameter.AddRange(_allParameters);
                _allParameters.Add(passThroughParameter);

                IParameter[] parameters = _allParameters.ToArray();
                TFactory factory = _resolutionRoot.Get<TFactory>(parameters);
                _allParameters.Clear();
                return factory;
            }
        }
    }
}