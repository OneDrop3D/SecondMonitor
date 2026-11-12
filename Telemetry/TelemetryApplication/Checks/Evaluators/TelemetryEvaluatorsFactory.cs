namespace SecondMonitor.Telemetry.TelemetryApplication.Checks.Evaluators
{
    using System.Collections.Generic;
    using System.Linq;
    using Ninject;
    using Ninject.Syntax;
    using TelemetryManagement.DTO;

    public class TelemetryEvaluatorsFactory
    {
        private readonly IResolutionRoot _resolutionRoot;

        public TelemetryEvaluatorsFactory(IResolutionRoot resolutionRoot)
        {
            _resolutionRoot = resolutionRoot;
        }

        public IEnumerable<ITelemetryEvaluator> CreateEvaluators(LapTelemetryDto lapTelemetryDto)
        {
            return _resolutionRoot.GetAll<ITelemetryEvaluator>().Where(x => x.SupportsSimulator(lapTelemetryDto.LapSummary.Simulator));
        }
    }
}