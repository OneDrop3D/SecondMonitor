namespace SecondMonitor.Timing.Api
{
    using Ninject;
    using Ninject.Syntax;

    using SecondMonitor.Timing.Common.SessionTiming;

    internal static class TimingApplicationProxy
    {
        /// <summary>
        /// Yes, this is an ugly way how to get DI working in this API project, but it is the easiest way how not to deal with ASP.NET Core vs .NET CORE WPF dependency conflicts
        /// </summary>
        public static IResolutionRoot? ResolutionRoot { get; private set; }

        public static ISessionInfoProvider? SessionInfoProvider { get; private set; }

        public static void Initialize(IResolutionRoot resolutionRoot)
        {
            SessionInfoProvider = resolutionRoot.Get<ISessionInfoProvider>();
            ResolutionRoot = resolutionRoot;
        }
    }
}
