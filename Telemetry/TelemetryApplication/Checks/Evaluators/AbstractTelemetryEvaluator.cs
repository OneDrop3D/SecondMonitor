namespace SecondMonitor.Telemetry.TelemetryApplication.Checks.Evaluators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DataModel.BasicProperties;
    using DataModel.BasicProperties.Units;
    using DataModel.Telemetry;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.Settings;
    using TelemetryManagement.DTO;

    public abstract class AbstractTelemetryEvaluator<T> : ITelemetryEvaluator where T : class, IEvaluationViewModel
    {
        private readonly List<IssueSummary> _issueSummaries;

        protected AbstractTelemetryEvaluator(IViewModelFactory viewModelFactory, ISettingsProvider settingsProvider) : this(viewModelFactory, settingsProvider, string.Empty,
            Array.Empty<string>(), Array.Empty<string>())
        {
        }

        protected AbstractTelemetryEvaluator(IViewModelFactory viewModelFactory, ISettingsProvider settingsProvider, string evaluatorName, string[] supportedSimulators, string[] notSupportedSimulators)
        {
            ViewModel = viewModelFactory.Set<string>().To(evaluatorName).Create<T>();
            ViewModel.EvaluationResultKind = EvaluationResultKind.Ok;
            VelocityUnitsSmall = settingsProvider.DisplaySettingsViewModel.VelocityUnitsVerySmall;
            VelocityUnits = settingsProvider.DisplaySettingsViewModel.VelocityUnits;
            DistanceUnitsSmall = settingsProvider.DisplaySettingsViewModel.DistanceUnitsVerySmall;
            ForceUnits = settingsProvider.DisplaySettingsViewModel.ForceUnits;
            AngleUnits = settingsProvider.DisplaySettingsViewModel.AngleUnits;
            TorqueUnits = settingsProvider.DisplaySettingsViewModel.TorqueUnits;
            PowerUnits = settingsProvider.DisplaySettingsViewModel.PowerUnits;
            TemperatureUnits = settingsProvider.DisplaySettingsViewModel.TemperatureUnits;
            VolumeUnits = settingsProvider.DisplaySettingsViewModel.VolumeUnits;
            PressureUnits = settingsProvider.DisplaySettingsViewModel.DisplayPressureUnits;
            _issueSummaries = new List<IssueSummary>();
            SupportedSimulators = supportedSimulators;
            NotSupportedSimulators = notSupportedSimulators;
        }

        public VelocityUnits VelocityUnits { get; }
        public VelocityUnits VelocityUnitsSmall { get; }
        public DistanceUnits DistanceUnitsSmall { get; }
        public ForceUnits ForceUnits { get; }
        public AngleUnits AngleUnits { get; }
        public TorqueUnits TorqueUnits { get; }
        public PowerUnits PowerUnits { get; }
        public TemperatureUnits TemperatureUnits { get; }
        public VolumeUnits VolumeUnits { get; }
        public PressureUnits PressureUnits { get; }

        protected int TotalDataPoints { get; private set; }

        protected TimeSpan LapTime { get; private set; }

        public IReadOnlyCollection<IssueSummary> Issues => _issueSummaries.AsReadOnly();

        protected string[] NotSupportedSimulators { get; }

        protected string[] SupportedSimulators { get; }

        public IEvaluationViewModel EvaluationViewModel => ViewModel;

        protected T ViewModel { get; }

        public bool SupportsSimulator(string simulatorName)
        {
            if (SupportedSimulators.Length == 0 && NotSupportedSimulators.Length == 0)
            {
                return true;
            }

            return (SupportedSimulators.Length > 0 && SupportedSimulators.Contains(simulatorName)) && !NotSupportedSimulators.Contains(simulatorName);
        }

        public void StartEvaluation(LapTelemetryDto lapTelemetryDto)
        {
            TotalDataPoints = lapTelemetryDto.DataPoints.Count;
            LapTime = lapTelemetryDto.LapSummary.LapTime;
            _issueSummaries.Clear();
            StartEvaluationInternal(lapTelemetryDto);
        }

        protected double ComputeAverageValue(double value)
        {
            return value / TotalDataPoints;
        }

        protected TimeSpan ComputeTimeSpent(int numberOfDataPoints)
        {
            return TimeSpan.FromSeconds(numberOfDataPoints * (LapTime.TotalSeconds / TotalDataPoints));
        }

        protected double ComputeTimeSpentPercentage(int numberOfDataPoints)
        {
            return (ComputeTimeSpent(numberOfDataPoints).TotalSeconds / LapTime.TotalSeconds) * 100;
        }

        protected int GetDataPointsCountForTime(TimeSpan timeSpan)
        {
            double dataPointsForOneSecond = TotalDataPoints / LapTime.TotalSeconds;
            return (int)Math.Ceiling(timeSpan.TotalSeconds * dataPointsForOneSecond);
        }

        public abstract void ProcessDataPoints(TimedTelemetrySnapshot telemetrySnapshot);
        public abstract void FinishEvaluation();

        protected abstract void StartEvaluationInternal(LapTelemetryDto lapTelemetryDto);

        protected void AddIssue(EvaluationResultKind evaluationResultKind, string header, string issueDescription)
        {
            if (ViewModel.EvaluationResultKind < evaluationResultKind)
            {
                ViewModel.EvaluationResultKind = evaluationResultKind;
            }

            _issueSummaries.Add(new IssueSummary(evaluationResultKind, header, issueDescription));
        }
    }
}