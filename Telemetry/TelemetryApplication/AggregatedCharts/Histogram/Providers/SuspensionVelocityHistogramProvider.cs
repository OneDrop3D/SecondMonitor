namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.Histogram.Providers
{
    using System;
    using System.Collections.Generic;
    using Controllers.Settings;
    using DataModel.BasicProperties;
    using Extractors;
    using Filter;
    using SecondMonitor.ViewModels.Factory;
    using TelemetryManagement.DTO;
    using TelemetryManagement.Settings;
    using ViewModels.AggregatedCharts.Histogram;
    using ViewModels.LoadedLapCache;

    public class SuspensionVelocityHistogramProvider : AbstractWheelHistogramProvider<SuspensionVelocityWheelsChartViewModel, SuspensionVelocityHistogramChartViewModel>
    {
        private readonly SuspensionVelocityHistogramDataExtractor _suspensionVelocityHistogramDataExtractor;
        private readonly SuspensionVelocityFilter _suspensionVelocityFilter;
        private readonly ISettingsController _settingsController;
        private CarWithChartPropertiesDto _carWithChartProperties;

        public SuspensionVelocityHistogramProvider(SuspensionVelocityHistogramDataExtractor suspensionVelocityHistogramDataExtractor, ILoadedLapsCache loadedLapsCache, IViewModelFactory viewModelFactory, SuspensionVelocityFilter suspensionVelocityFilter, ISettingsController settingsController)
            : base(suspensionVelocityHistogramDataExtractor, loadedLapsCache, viewModelFactory, new[] { suspensionVelocityFilter })
        {
            _suspensionVelocityHistogramDataExtractor = suspensionVelocityHistogramDataExtractor;
            _suspensionVelocityFilter = suspensionVelocityFilter;
            _settingsController = settingsController;
        }

        public override string ChartName => "Suspension Velocity Histogram";

        public override AggregatedChartKind Kind => AggregatedChartKind.Histogram;

        public override bool IsUsingCarProperties => true;

        protected override bool ResetCommandVisible => true;

        protected override void OnNewViewModel(SuspensionVelocityWheelsChartViewModel newViewModel)
        {
            _carWithChartProperties = _settingsController.CarSettingsController.CurrentCarWithChartProperties;
            newViewModel.BandSize = _carWithChartProperties.ChartsProperties.SuspensionVelocityHistogram.BandSize.GetValueInUnits(_suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);
            newViewModel.Maximum = _carWithChartProperties.ChartsProperties.SuspensionVelocityHistogram.Maximum.GetValueInUnits(_suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);
            newViewModel.Minimum = _carWithChartProperties.ChartsProperties.SuspensionVelocityHistogram.Minimum.GetValueInUnits(_suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);
            newViewModel.IsZeroBandEnabled =
                _carWithChartProperties.ChartsProperties.SuspensionVelocityHistogram.IsZeroBandEnabled;

            ((SuspensionVelocityHistogramChartViewModel)newViewModel.FrontLeftChartViewModel).BumpTransition = _carWithChartProperties.CarPropertiesDto.FrontLeftTyre.BumpTransition.GetValueInUnits(_suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);
            ((SuspensionVelocityHistogramChartViewModel)newViewModel.FrontLeftChartViewModel).ReboundTransition = _carWithChartProperties.CarPropertiesDto.FrontLeftTyre.ReboundTransition.GetValueInUnits(_suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);
            ((SuspensionVelocityHistogramChartViewModel)newViewModel.FrontLeftChartViewModel).Unit = Velocity.GetUnitSymbol(_suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);

            ((SuspensionVelocityHistogramChartViewModel)newViewModel.FrontRightChartViewModel).BumpTransition = _carWithChartProperties.CarPropertiesDto.FrontRightTyre.BumpTransition.GetValueInUnits(_suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);
            ((SuspensionVelocityHistogramChartViewModel)newViewModel.FrontRightChartViewModel).ReboundTransition = _carWithChartProperties.CarPropertiesDto.FrontRightTyre.ReboundTransition.GetValueInUnits(_suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);
            ((SuspensionVelocityHistogramChartViewModel)newViewModel.FrontRightChartViewModel).Unit = Velocity.GetUnitSymbol(_suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);

            ((SuspensionVelocityHistogramChartViewModel)newViewModel.RearLeftChartViewModel).BumpTransition = _carWithChartProperties.CarPropertiesDto.RearLeftTyre.BumpTransition.GetValueInUnits(_suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);
            ((SuspensionVelocityHistogramChartViewModel)newViewModel.RearLeftChartViewModel).ReboundTransition = _carWithChartProperties.CarPropertiesDto.RearLeftTyre.ReboundTransition.GetValueInUnits(_suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);
            ((SuspensionVelocityHistogramChartViewModel)newViewModel.RearLeftChartViewModel).Unit = Velocity.GetUnitSymbol(_suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);

            ((SuspensionVelocityHistogramChartViewModel)newViewModel.RearRightChartViewModel).BumpTransition = _carWithChartProperties.CarPropertiesDto.RearRightTyre.BumpTransition.GetValueInUnits(_suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);
            ((SuspensionVelocityHistogramChartViewModel)newViewModel.RearRightChartViewModel).ReboundTransition = _carWithChartProperties.CarPropertiesDto.RearRightTyre.ReboundTransition.GetValueInUnits(_suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);
            ((SuspensionVelocityHistogramChartViewModel)newViewModel.RearRightChartViewModel).Unit = Velocity.GetUnitSymbol(_suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);

            newViewModel.BandSize = _carWithChartProperties.ChartsProperties.SuspensionVelocityHistogram.BandSize.GetValueInUnits(_suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);
        }

        protected override void ResetHistogramParameters(IReadOnlyCollection<LapTelemetryDto> loadedLaps, double bandSize, SuspensionVelocityWheelsChartViewModel wheelsChart)
        {
            _carWithChartProperties = _settingsController.CarSettingsController.CurrentCarWithChartProperties;
            var carPropertiesDefault = _settingsController.CarSettingsController.GetDefaultCarPropertiesForCurrentCar();
            _carWithChartProperties.ChartsProperties.SuspensionVelocityHistogram = carPropertiesDefault.ChartsProperties.SuspensionVelocityHistogram;

            _carWithChartProperties.CarPropertiesDto.FrontLeftTyre.BumpTransition = carPropertiesDefault.CarPropertiesDto.FrontLeftTyre.BumpTransition;
            _carWithChartProperties.CarPropertiesDto.FrontLeftTyre.ReboundTransition = carPropertiesDefault.CarPropertiesDto.FrontLeftTyre.ReboundTransition;

            _carWithChartProperties.CarPropertiesDto.RearLeftTyre.BumpTransition = carPropertiesDefault.CarPropertiesDto.RearLeftTyre.BumpTransition;
            _carWithChartProperties.CarPropertiesDto.RearLeftTyre.ReboundTransition = carPropertiesDefault.CarPropertiesDto.RearLeftTyre.ReboundTransition;

            _carWithChartProperties.CarPropertiesDto.RearRightTyre.BumpTransition = carPropertiesDefault.CarPropertiesDto.RearRightTyre.BumpTransition;
            _carWithChartProperties.CarPropertiesDto.RearRightTyre.ReboundTransition = carPropertiesDefault.CarPropertiesDto.RearRightTyre.ReboundTransition;

            _carWithChartProperties.CarPropertiesDto.FrontRightTyre.BumpTransition = carPropertiesDefault.CarPropertiesDto.FrontRightTyre.BumpTransition;
            _carWithChartProperties.CarPropertiesDto.FrontRightTyre.ReboundTransition = carPropertiesDefault.CarPropertiesDto.FrontRightTyre.ReboundTransition;

            _settingsController.CarSettingsController.UpdateCarProperties(_carWithChartProperties);
        }

        protected override void ApplyHistogramLimits(Histogram flHistogram, Histogram frHistogram, Histogram rlHistogram, Histogram rrHistogram, SuspensionVelocityWheelsChartViewModel viewModel)
        {
            double maxY = Math.Max(flHistogram.MaximumY, Math.Max(frHistogram.MaximumY, Math.Max(rlHistogram.MaximumY, rrHistogram.MaximumY)));
            flHistogram.MaximumY = maxY;
            frHistogram.MaximumY = maxY;
            rlHistogram.MaximumY = maxY;
            rrHistogram.MaximumY = maxY;

            flHistogram.MinimumX = viewModel.Minimum;
            frHistogram.MinimumX = viewModel.Minimum;
            rlHistogram.MinimumX = viewModel.Minimum;
            rrHistogram.MinimumX = viewModel.Minimum;

            flHistogram.MaximumX = viewModel.Maximum;
            frHistogram.MaximumX = viewModel.Maximum;
            rlHistogram.MaximumX = viewModel.Maximum;
            rrHistogram.MaximumX = viewModel.Maximum;
        }

        protected override double GetBandSize()
        {
            _carWithChartProperties = _settingsController.CarSettingsController.CurrentCarWithChartProperties;
            if (_carWithChartProperties?.ChartsProperties?.SuspensionVelocityHistogram?.BandSize == null)
            {
                return base.GetBandSize();
            }

            return _carWithChartProperties.ChartsProperties.SuspensionVelocityHistogram.BandSize.GetValueInUnits(_suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);
        }

        protected override void RefreshHistogram(IReadOnlyCollection<LapTelemetryDto> loadedLaps, double bandSize, SuspensionVelocityWheelsChartViewModel wheelsChart)
        {
            _carWithChartProperties.ChartsProperties.SuspensionVelocityHistogram.BandSize = Velocity.FromUnits(wheelsChart.BandSize, _suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);
            _carWithChartProperties.ChartsProperties.SuspensionVelocityHistogram.Minimum = Velocity.FromUnits(wheelsChart.Minimum, _suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);
            _carWithChartProperties.ChartsProperties.SuspensionVelocityHistogram.Maximum = Velocity.FromUnits(wheelsChart.Maximum, _suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);
            _carWithChartProperties.ChartsProperties.SuspensionVelocityHistogram.IsZeroBandEnabled = wheelsChart.IsZeroBandEnabled;

            _carWithChartProperties.CarPropertiesDto.FrontLeftTyre.BumpTransition = Velocity.FromUnits(((SuspensionVelocityHistogramChartViewModel)wheelsChart.FrontLeftChartViewModel).BumpTransition, _suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);
            _carWithChartProperties.CarPropertiesDto.FrontLeftTyre.ReboundTransition = Velocity.FromUnits(((SuspensionVelocityHistogramChartViewModel)wheelsChart.FrontLeftChartViewModel).ReboundTransition, _suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);

            _carWithChartProperties.CarPropertiesDto.FrontRightTyre.BumpTransition = Velocity.FromUnits(((SuspensionVelocityHistogramChartViewModel)wheelsChart.FrontRightChartViewModel).BumpTransition, _suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);
            _carWithChartProperties.CarPropertiesDto.FrontRightTyre.ReboundTransition = Velocity.FromUnits(((SuspensionVelocityHistogramChartViewModel)wheelsChart.FrontRightChartViewModel).ReboundTransition, _suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);

            _carWithChartProperties.CarPropertiesDto.RearLeftTyre.BumpTransition = Velocity.FromUnits(((SuspensionVelocityHistogramChartViewModel)wheelsChart.RearLeftChartViewModel).BumpTransition, _suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);
            _carWithChartProperties.CarPropertiesDto.RearLeftTyre.ReboundTransition = Velocity.FromUnits(((SuspensionVelocityHistogramChartViewModel)wheelsChart.RearLeftChartViewModel).ReboundTransition, _suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);

            _carWithChartProperties.CarPropertiesDto.RearRightTyre.BumpTransition = Velocity.FromUnits(((SuspensionVelocityHistogramChartViewModel)wheelsChart.RearRightChartViewModel).BumpTransition, _suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);
            _carWithChartProperties.CarPropertiesDto.RearRightTyre.ReboundTransition = Velocity.FromUnits(((SuspensionVelocityHistogramChartViewModel)wheelsChart.RearRightChartViewModel).ReboundTransition, _suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall);
            _settingsController.CarSettingsController.UpdateCarProperties(_carWithChartProperties);
        }

        protected override Histogram ExtractFlHistogram(IReadOnlyCollection<LapTelemetryDto> loadedLaps, double bandSize, SuspensionVelocityWheelsChartViewModel wheelsChart)
        {
            Filters.ForEach(x => x.FilterFrontLeft());
            _suspensionVelocityHistogramDataExtractor.BumpTransition = ((SuspensionVelocityHistogramChartViewModel)wheelsChart.FrontLeftChartViewModel).BumpTransition;
            _suspensionVelocityHistogramDataExtractor.ReboundTransition = ((SuspensionVelocityHistogramChartViewModel)wheelsChart.FrontLeftChartViewModel).ReboundTransition;
            return _suspensionVelocityHistogramDataExtractor.ExtractHistogramFrontLeft(loadedLaps, bandSize, Filters);
        }

        protected override Histogram ExtractFrHistogram(IReadOnlyCollection<LapTelemetryDto> loadedLaps, double bandSize, SuspensionVelocityWheelsChartViewModel wheelsChart)
        {
            Filters.ForEach(x => x.FilterFrontRight());
            _suspensionVelocityHistogramDataExtractor.BumpTransition = ((SuspensionVelocityHistogramChartViewModel)wheelsChart.FrontRightChartViewModel).BumpTransition;
            _suspensionVelocityHistogramDataExtractor.ReboundTransition = ((SuspensionVelocityHistogramChartViewModel)wheelsChart.FrontRightChartViewModel).ReboundTransition;
            return _suspensionVelocityHistogramDataExtractor.ExtractHistogramFrontRight(loadedLaps, bandSize, Filters);
        }

        protected override Histogram ExtractRlHistogram(IReadOnlyCollection<LapTelemetryDto> loadedLaps, double bandSize, SuspensionVelocityWheelsChartViewModel wheelsChart)
        {
            Filters.ForEach(x => x.FilterRearLeft());
            _suspensionVelocityHistogramDataExtractor.BumpTransition = ((SuspensionVelocityHistogramChartViewModel)wheelsChart.RearLeftChartViewModel).BumpTransition;
            _suspensionVelocityHistogramDataExtractor.ReboundTransition = ((SuspensionVelocityHistogramChartViewModel)wheelsChart.RearLeftChartViewModel).ReboundTransition;
            return _suspensionVelocityHistogramDataExtractor.ExtractHistogramRearLeft(loadedLaps, bandSize, Filters);
        }

        protected override Histogram ExtractRrHistogram(IReadOnlyCollection<LapTelemetryDto> loadedLaps, double bandSize, SuspensionVelocityWheelsChartViewModel wheelsChart)
        {
            Filters.ForEach(x => x.FilterRearRight());
            _suspensionVelocityHistogramDataExtractor.BumpTransition = ((SuspensionVelocityHistogramChartViewModel)wheelsChart.RearRightChartViewModel).BumpTransition;
            _suspensionVelocityHistogramDataExtractor.ReboundTransition = ((SuspensionVelocityHistogramChartViewModel)wheelsChart.RearRightChartViewModel).ReboundTransition;
            return _suspensionVelocityHistogramDataExtractor.ExtractHistogramRearRight(loadedLaps, bandSize, Filters);
        }

        protected override void BeforeHistogramFilling(SuspensionVelocityWheelsChartViewModel wheelsChart)
        {
            _suspensionVelocityHistogramDataExtractor.SetIsZeroBandInMiddle(wheelsChart.IsZeroBandEnabled);
            _suspensionVelocityFilter.MinimumVelocity = wheelsChart.Minimum;
            _suspensionVelocityFilter.MaximumVelocity = wheelsChart.Maximum;
            _suspensionVelocityFilter.VelocityUnits = _suspensionVelocityHistogramDataExtractor.VelocityUnitsSmall;
            base.BeforeHistogramFilling(wheelsChart);
        }
    }
}