namespace SecondMonitor.Contracts.NInject
{
    using System.Collections.Generic;
    using Ninject;
    using Ninject.Parameters;
    using Ninject.Syntax;

    public class NamedConstructorParameter<TFactory, TFacImplementation> : INamedConstructorParameter<TFactory> where TFacImplementation : TFactory
    {
        private readonly IResolutionRoot _resolutionRoot;
        private readonly string _parameterName;
        private readonly List<IParameter> _allParameters;

        public NamedConstructorParameter(IResolutionRoot resolutionRoot, string parameterName, params IParameter[] previousParameters)
        {
            _resolutionRoot = resolutionRoot;
            _parameterName = parameterName;
            _allParameters = new List<IParameter>(previousParameters);
        }

        public TFactory To(object instance)
        {
            _allParameters.Add(new ConstructorArgument(_parameterName, instance));

            using (PassThroughConstructorArgument passThroughParameter = new PassThroughConstructorArgument())
            {
                passThroughParameter.AddRange(_allParameters);
                _allParameters.Add(passThroughParameter);

                IParameter[] parameters = _allParameters.ToArray();
                TFacImplementation factory = _resolutionRoot.Get<TFacImplementation>(parameters);
                _allParameters.Clear();
                return factory;
            }
        }
    }
}