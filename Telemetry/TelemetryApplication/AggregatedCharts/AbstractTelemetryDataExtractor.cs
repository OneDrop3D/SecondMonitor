namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Controllers.Settings;
    using Controllers.Settings.Car;
    using DataModel.BasicProperties;
    using DataModel.BasicProperties.Units;
    using DataModel.Telemetry;
    using Filter;
    using SecondMonitor.ViewModels.Settings;
    using TelemetryManagement.DTO;
    using TelemetryManagement.Settings;
    using TelemetryManagement.StoryBoard;

    public abstract class AbstractTelemetryDataExtractor
    {
        protected AbstractTelemetryDataExtractor(ISettingsProvider settingsProvider, ISettingsController settingsController)
        {
            VelocityUnitsSmall = settingsProvider.DisplaySettingsViewModel.VelocityUnitsVerySmall;
            VelocityUnits = settingsProvider.DisplaySettingsViewModel.VelocityUnits;
            DistanceUnitsSmall = settingsProvider.DisplaySettingsViewModel.DistanceUnitsVerySmall;
            ForceUnits = settingsProvider.DisplaySettingsViewModel.ForceUnits;
            AngleUnits = settingsProvider.DisplaySettingsViewModel.AngleUnits;
            TorqueUnits = settingsProvider.DisplaySettingsViewModel.TorqueUnits;
            PowerUnits = settingsProvider.DisplaySettingsViewModel.PowerUnits;
            CarSettingsController = settingsController.CarSettingsController;
        }

        public VelocityUnits VelocityUnits { get; }
        public VelocityUnits VelocityUnitsSmall { get; }
        public DistanceUnits DistanceUnitsSmall { get; }
        public ForceUnits ForceUnits { get; }
        public AngleUnits AngleUnits { get; }
        public TorqueUnits TorqueUnits { get; }
        public PowerUnits PowerUnits { get; }
        public bool IsUsingComputedData { get; protected set; }

        protected ICarSettingsController CarSettingsController { get; }

        protected TimedValue[] ExtractTimedValuesOfLoadedLaps(IEnumerable<LapTelemetryDto> loadedLaps, Func<TimedTelemetrySnapshot, CarPropertiesDto, double> extractionFunc, IReadOnlyCollection<ITelemetryFilter> filters)
        {
            IEnumerable<TimedValue> timedValues = loadedLaps.SelectMany(x => ExtractTimedValues(x.DataPoints, CarSettingsController.GetCarProperties(x), extractionFunc, filters));
            return timedValues.ToArray();
        }

        private static IEnumerable<TimedValue> ExtractTimedValues(List<TimedTelemetrySnapshot> snapshots, CarPropertiesDto carPropertiesDto, Func<TimedTelemetrySnapshot, CarPropertiesDto, double> extractDataFunc, IReadOnlyCollection<ITelemetryFilter> filters)
        {
            for (int i = 0; i < snapshots.Count - 1; i++)
            {
                TimedTelemetrySnapshot firstSnapshot = snapshots[i];
                TimedTelemetrySnapshot secondSnapshot = snapshots[i + 1];

                if (filters?.Any(x => !x.Accepts(firstSnapshot)) == true)
                {
                    continue;
                }

                double value = extractDataFunc(firstSnapshot, carPropertiesDto);

                yield return new TimedValue(value, firstSnapshot, secondSnapshot);
            }
        }
    }
}