namespace SecondMonitor.Bootstrapping
{
    using Ninject;
    public static class StandardKernelFactory
    {
        public static IKernel CreateNew()
        {
            var options = new NinjectSettings
            {
                InjectNonPublic = true
            };
            return new StandardKernel(options);
        }
    }
}