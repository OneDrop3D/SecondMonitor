namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.Histogram.Providers
{
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

    public class CamberHistogramProvider : AbstractWheelHistogramProvider<CamberWheelsChartViewModel, CamberHistogramChartViewModel>
    {
        private readonly CamberHistogramExtractor _camberHistogramExtractor;
        private readonly LoadedWheelFilter _loadedWheelFilter;
        private readonly CamberFilter _camberFilter;
        private readonly ISettingsController _settingsController;
        private CarWithChartPropertiesDto _currentCarWithChart;

        public CamberHistogramProvider(CamberHistogramExtractor camberHistogramExtractor, ILoadedLapsCache loadedLapsCache, IViewModelFactory viewModelFactory,  LoadedWheelFilter loadedWheelFilter,
            CamberFilter camberFilter, ISettingsController settingsController) : base(camberHistogramExtractor, loadedLapsCache, viewModelFactory, new IWheelTelemetryFilter[] { loadedWheelFilter, camberFilter })
        {
            _camberHistogramExtractor = camberHistogramExtractor;
            _loadedWheelFilter = loadedWheelFilter;
            _camberFilter = camberFilter;
            _settingsController = settingsController;
        }

        public override string ChartName => "Camber Histogram";
        public override AggregatedChartKind Kind => AggregatedChartKind.Histogram;
        protected override bool ResetCommandVisible => true;
        public override bool IsUsingCarProperties => true;
        protected override void OnNewViewModel(CamberWheelsChartViewModel newViewModel)
        {
            _currentCarWithChart = _settingsController.CarSettingsController.CurrentCarWithChartProperties;

            newViewModel.IsLoadedChecked = _currentCarWithChart.ChartsProperties.CamberHistogram.IsLoadedSelected;
            newViewModel.IsUnloadedChecked = _currentCarWithChart.ChartsProperties.CamberHistogram.IsUnloadedSelected;
            newViewModel.FromG = _currentCarWithChart.ChartsProperties.CamberHistogram.FromG;
            newViewModel.ToG = _currentCarWithChart.ChartsProperties.CamberHistogram.ToG;
            newViewModel.BandSize = _currentCarWithChart.ChartsProperties.CamberHistogram.BandSize.GetValueInUnits(_camberHistogramExtractor.AngleUnits);
            newViewModel.FromCamber = _currentCarWithChart.ChartsProperties.CamberHistogram.FromCamber.GetValueInUnits(_camberHistogramExtractor.AngleUnits);
            newViewModel.ToCamber = _currentCarWithChart.ChartsProperties.CamberHistogram.ToCamber.GetValueInUnits(_camberHistogramExtractor.AngleUnits);

            ((CamberHistogramChartViewModel)newViewModel.FrontLeftChartViewModel).IdealCamber = _currentCarWithChart.CarPropertiesDto.FrontLeftTyre.IdealCamber.GetValueInUnits(_camberHistogramExtractor.AngleUnits);
            ((CamberHistogramChartViewModel)newViewModel.FrontRightChartViewModel).IdealCamber = _currentCarWithChart.CarPropertiesDto.FrontRightTyre.IdealCamber.GetValueInUnits(_camberHistogramExtractor.AngleUnits);
            ((CamberHistogramChartViewModel)newViewModel.RearLeftChartViewModel).IdealCamber = _currentCarWithChart.CarPropertiesDto.RearLeftTyre.IdealCamber.GetValueInUnits(_camberHistogramExtractor.AngleUnits);
            ((CamberHistogramChartViewModel)newViewModel.RearRightChartViewModel).IdealCamber = _currentCarWithChart.CarPropertiesDto.RearRightTyre.IdealCamber.GetValueInUnits(_camberHistogramExtractor.AngleUnits);

            ((CamberHistogramChartViewModel)newViewModel.FrontLeftChartViewModel).AngleUnits = newViewModel.Unit;
            ((CamberHistogramChartViewModel)newViewModel.FrontRightChartViewModel).AngleUnits = newViewModel.Unit;
            ((CamberHistogramChartViewModel)newViewModel.RearLeftChartViewModel).AngleUnits = newViewModel.Unit;
            ((CamberHistogramChartViewModel)newViewModel.RearRightChartViewModel).AngleUnits = newViewModel.Unit;
        }

        protected override void RefreshHistogram(IReadOnlyCollection<LapTelemetryDto> loadedLaps, double bandSize, CamberWheelsChartViewModel wheelsChart)
        {
            _currentCarWithChart.ChartsProperties.CamberHistogram.IsLoadedSelected = wheelsChart.IsLoadedChecked;
            _currentCarWithChart.ChartsProperties.CamberHistogram.IsUnloadedSelected = wheelsChart.IsUnloadedChecked;
            _currentCarWithChart.ChartsProperties.CamberHistogram.FromG = wheelsChart.FromG;
            _currentCarWithChart.ChartsProperties.CamberHistogram.ToG = wheelsChart.ToG;
            _currentCarWithChart.ChartsProperties.CamberHistogram.BandSize = Angle.GetFromValue(wheelsChart.BandSize, _camberHistogramExtractor.AngleUnits);
            _currentCarWithChart.ChartsProperties.CamberHistogram.FromCamber = Angle.GetFromValue(wheelsChart.FromCamber, _camberHistogramExtractor.AngleUnits);
            _currentCarWithChart.ChartsProperties.CamberHistogram.ToCamber = Angle.GetFromValue(wheelsChart.ToCamber, _camberHistogramExtractor.AngleUnits);

            _currentCarWithChart.CarPropertiesDto.FrontLeftTyre.IdealCamber = Angle.GetFromValue(((CamberHistogramChartViewModel)wheelsChart.FrontLeftChartViewModel).IdealCamber, _camberHistogramExtractor.AngleUnits);
            _currentCarWithChart.CarPropertiesDto.FrontRightTyre.IdealCamber = Angle.GetFromValue(((CamberHistogramChartViewModel)wheelsChart.FrontRightChartViewModel).IdealCamber, _camberHistogramExtractor.AngleUnits);
            _currentCarWithChart.CarPropertiesDto.RearLeftTyre.IdealCamber = Angle.GetFromValue(((CamberHistogramChartViewModel)wheelsChart.RearLeftChartViewModel).IdealCamber, _camberHistogramExtractor.AngleUnits);
            _currentCarWithChart.CarPropertiesDto.RearRightTyre.IdealCamber = Angle.GetFromValue(((CamberHistogramChartViewModel)wheelsChart.RearRightChartViewModel).IdealCamber, _camberHistogramExtractor.AngleUnits);
            _settingsController.CarSettingsController.UpdateCarProperties(_currentCarWithChart);
        }

        protected override void ResetHistogramParameters(IReadOnlyCollection<LapTelemetryDto> loadedLaps, double bandSize, CamberWheelsChartViewModel wheelsChart)
        {
            var carProperties = _settingsController.CarSettingsController.CurrentCarWithChartProperties;
            var carPropertiesDefault = _settingsController.CarSettingsController.GetDefaultCarPropertiesForCurrentCar();
            carProperties.ChartsProperties.CamberHistogram = carPropertiesDefault.ChartsProperties.CamberHistogram;

            carProperties.CarPropertiesDto.FrontLeftTyre.IdealCamber = carPropertiesDefault.CarPropertiesDto.FrontLeftTyre.IdealCamber;
            carProperties.CarPropertiesDto.FrontRightTyre.IdealCamber = carPropertiesDefault.CarPropertiesDto.FrontRightTyre.IdealCamber;
            carProperties.CarPropertiesDto.RearRightTyre.IdealCamber = carPropertiesDefault.CarPropertiesDto.RearRightTyre.IdealCamber;
            carProperties.CarPropertiesDto.RearLeftTyre.IdealCamber = carPropertiesDefault.CarPropertiesDto.RearLeftTyre.IdealCamber;

            _settingsController.CarSettingsController.UpdateCarProperties(carProperties);
        }

        protected override Histogram ExtractFlHistogram(IReadOnlyCollection<LapTelemetryDto> loadedLaps, double bandSize, CamberWheelsChartViewModel wheelsChart)
        {
            Filters.ForEach(x => x.FilterFrontLeft());
            _camberHistogramExtractor.IdealCamber = ((CamberHistogramChartViewModel)wheelsChart.FrontLeftChartViewModel).IdealCamber;
            return _camberHistogramExtractor.ExtractHistogramFrontLeft(loadedLaps, bandSize, Filters);
        }

        protected override Histogram ExtractFrHistogram(IReadOnlyCollection<LapTelemetryDto> loadedLaps, double bandSize, CamberWheelsChartViewModel wheelsChart)
        {
            Filters.ForEach(x => x.FilterFrontRight());
            _camberHistogramExtractor.IdealCamber = ((CamberHistogramChartViewModel)wheelsChart.FrontRightChartViewModel).IdealCamber;
            return _camberHistogramExtractor.ExtractHistogramFrontRight(loadedLaps, bandSize, Filters);
        }

        protected override Histogram ExtractRlHistogram(IReadOnlyCollection<LapTelemetryDto> loadedLaps, double bandSize, CamberWheelsChartViewModel wheelsChart)
        {
            Filters.ForEach(x => x.FilterRearLeft());
            _camberHistogramExtractor.IdealCamber = ((CamberHistogramChartViewModel)wheelsChart.RearLeftChartViewModel).IdealCamber;
            return _camberHistogramExtractor.ExtractHistogramRearLeft(loadedLaps, bandSize, Filters);
        }

        protected override Histogram ExtractRrHistogram(IReadOnlyCollection<LapTelemetryDto> loadedLaps, double bandSize, CamberWheelsChartViewModel wheelsChart)
        {
            Filters.ForEach(x => x.FilterRearRight());
            _camberHistogramExtractor.IdealCamber = ((CamberHistogramChartViewModel)wheelsChart.RearRightChartViewModel).IdealCamber;
            return _camberHistogramExtractor.ExtractHistogramRearRight(loadedLaps, bandSize, Filters);
        }

        protected override void BeforeHistogramFilling(CamberWheelsChartViewModel wheelsChart)
        {
            _loadedWheelFilter.MinimumG = wheelsChart.FromG;
            _loadedWheelFilter.MaximumG = wheelsChart.ToG;
            _loadedWheelFilter.IncludeLoaded = wheelsChart.IsLoadedChecked;
            _loadedWheelFilter.IncludeUnloaded = wheelsChart.IsUnloadedChecked;

            _camberFilter.AngleUnits = _camberHistogramExtractor.AngleUnits;
            _camberFilter.MinimumCamber = wheelsChart.FromCamber;
            _camberFilter.MaximumCamber = wheelsChart.ToCamber;
            base.BeforeHistogramFilling(wheelsChart);
        }
    }
}