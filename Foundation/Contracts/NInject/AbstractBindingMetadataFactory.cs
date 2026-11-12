namespace SecondMonitor.Contracts.NInject
{
    using System.Collections.Generic;
    using System.Linq;
    using Ninject;
    using Ninject.Planning.Bindings;
    using Ninject.Syntax;

    public abstract class AbstractBindingMetadataFactory<T>
    {
        private readonly IResolutionRoot _resolutionRoot;

        protected AbstractBindingMetadataFactory(IResolutionRoot resolutionRoot)
        {
            _resolutionRoot = resolutionRoot;
        }

        protected abstract string MetadataName { get; }

        public T Create(string bindingMetadataValue)
        {
            if (string.IsNullOrWhiteSpace(bindingMetadataValue))
            {
                return CreateEmpty();
            }

            try
            {
                return _resolutionRoot.Get<T>(bm => WhenMetadataMatched(bm, bindingMetadataValue));
            }
            catch (ActivationException)
            {
                return CreateEmpty();
            }
        }

        public IEnumerable<T> CreateAll(string bindingMetadataValue)
        {
            if (string.IsNullOrWhiteSpace(bindingMetadataValue))
            {
                return Enumerable.Empty<T>();
            }

            try
            {
                return _resolutionRoot.GetAll<T>(bm => WhenMetadataMatched(bm, bindingMetadataValue));
            }
            catch (ActivationException)
            {
                return Enumerable.Empty<T>();
            }
        }

        protected abstract T CreateEmpty();

        private bool WhenMetadataMatched(IBindingMetadata metadata, string bindingMetadataValue)
        {
            if (string.IsNullOrWhiteSpace(bindingMetadataValue))
            {
                return !metadata.Has(MetadataName);
            }

            if (!metadata.Has(MetadataName))
            {
                return false;
            }

            return metadata.Get<string>(MetadataName) == bindingMetadataValue;
        }
    }
}