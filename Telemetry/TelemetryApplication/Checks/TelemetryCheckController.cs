namespace SecondMonitor.Telemetry.TelemetryApplication.Checks
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using DataModel.Telemetry;
    using Evaluators;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;
    using TelemetryManagement.DTO;

    public class TelemetryCheckController : ITelemetryCheckController
    {
        private readonly IWindowService _windowService;
        private readonly TelemetryEvaluatorsFactory _telemetryEvaluatorsFactory;
        private readonly IViewModelFactory _viewModelFactory;

        public TelemetryCheckController(IWindowService windowService, TelemetryEvaluatorsFactory telemetryEvaluatorsFactory, IViewModelFactory viewModelFactory)
        {
            _windowService = windowService;
            _telemetryEvaluatorsFactory = telemetryEvaluatorsFactory;
            _viewModelFactory = viewModelFactory;
        }

        public Task StartControllerAsync()
        {
            return Task.CompletedTask;
        }

        public Task StopControllerAsync()
        {
            return Task.CompletedTask;
        }

        public void OpenCheckWindow(LapTelemetryDto lapTelemetryDto)
        {
            List<ITelemetryEvaluator> evaluators = _telemetryEvaluatorsFactory.CreateEvaluators(lapTelemetryDto).ToList();

            evaluators.ForEach(x => x.StartEvaluation(lapTelemetryDto));

            foreach (TimedTelemetrySnapshot timedTelemetrySnapshot in lapTelemetryDto.DataPoints)
            {
                evaluators.ForEach(x => x.ProcessDataPoints(timedTelemetrySnapshot));
            }

            evaluators.ForEach(x => x.FinishEvaluation());

            TelemetryCheckWindowViewModel telemetryCheckWindowViewModel = _viewModelFactory.Create<TelemetryCheckWindowViewModel>();
            evaluators.ForEach(telemetryCheckWindowViewModel.AddEvaluationResult);

            _windowService.OpenWindow(telemetryCheckWindowViewModel, $"Lap {lapTelemetryDto.LapSummary.LapNumber} evaluation.", WindowState.Normal, SizeToContent.Width, WindowStartupLocation.CenterOwner);
        }
    }
}