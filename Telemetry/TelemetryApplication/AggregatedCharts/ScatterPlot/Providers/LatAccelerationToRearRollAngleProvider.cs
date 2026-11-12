namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot.Providers
{
    using System.Collections.Generic;
    using System.Linq;
    using Controllers.Settings;
    using Controllers.Synchronization;
    using Extractors;
    using Filter;
    using OxyPlot;
    using OxyPlot.Annotations;
    using SecondMonitor.ViewModels.Colors;
    using SecondMonitor.ViewModels.Colors.Extensions;
    using Settings.DTO;
    using TelemetryManagement.DTO;
    using ViewModels.AggregatedCharts;
    using ViewModels.AggregatedCharts.ScatterPlot;
    using ViewModels.LoadedLapCache;

    public class LatAccelerationToRearRollAngleProvider : AbstractAggregatedChartProvider
    {
        protected static readonly List<OxyColor> ColorMap = new RedGreenGradientPalette().GetAllColors().Select(x => x.ToColor().ToOxyColor()).ToList();

        private readonly LatAccelerationToRearRollAngleExtractor _dataExtractor;
        private readonly LateralAccFilter _lateralAccFilter;
        private readonly IDataPointSelectionSynchronization _dataPointSelectionSynchronization;
        private readonly ISettingsController _settingsController;
        private readonly List<ITelemetryFilter> _filters;

        public LatAccelerationToRearRollAngleProvider(ILoadedLapsCache loadedLapsCache, LatAccelerationToRearRollAngleExtractor dataExtractor, LateralAccFilter lateralAccFilter, IDataPointSelectionSynchronization dataPointSelectionSynchronization, ISettingsController settingsController) : base(loadedLapsCache)
        {
            _dataExtractor = dataExtractor;
            _lateralAccFilter = lateralAccFilter;
            _dataPointSelectionSynchronization = dataPointSelectionSynchronization;
            _settingsController = settingsController;
            _filters = new List<ITelemetryFilter>() { _lateralAccFilter };
        }

        public override string ChartName => AggregatedChartNames.LatAccelerationToRearRollAngleChartName;

        public override AggregatedChartKind Kind => AggregatedChartKind.ScatterPlot;

        public override IReadOnlyCollection<IAggregatedChartViewModel> CreateAggregatedChartViewModels(AggregatedChartSettingsDto aggregatedChartSettings)
        {
            IEnumerable<IGrouping<int, LapTelemetryDto>> lapsStintGrouping = GetLapsGrouped(aggregatedChartSettings);
            if (aggregatedChartSettings.StintRenderingKind == StintRenderingKind.SingleChart)
            {
                return CreateChartForAllStints(lapsStintGrouping);
            }
            else
            {
                return CreateChartForEachStint(lapsStintGrouping, aggregatedChartSettings);
            }
        }

        private IReadOnlyCollection<IAggregatedChartViewModel> CreateChartForAllStints(IEnumerable<IGrouping<int, LapTelemetryDto>> lapsStintGrouping)
        {
            var lapsInStints = lapsStintGrouping as IGrouping<int, LapTelemetryDto>[] ?? lapsStintGrouping.ToArray();
            string title = BuildTitleForAllStints(lapsInStints);
            IColorPaletteProvider colorPaletteProvider = new BasicColorPaletteProvider();
            AxisDefinition xAxis = new AxisDefinition(_dataExtractor.XMajorTickSize, _dataExtractor.XMajorTickSize / 4, _dataExtractor.XUnit, "Lat Accel (G)");
            AxisDefinition yAxis = new AxisDefinition(_dataExtractor.YMajorTickSize, _dataExtractor.YMajorTickSize / 4, _dataExtractor.YUnit, "Rear Roll (Deg)");
            ScatterPlot scatterPlot = new ScatterPlot(title, xAxis, yAxis);
            _lateralAccFilter.MinimumG = 0;
            _lateralAccFilter.MaximumG = double.MaxValue;
            foreach (IGrouping<int, LapTelemetryDto> lapsInStint in lapsInStints)
            {
                string seriesTitle = BuildSeriesTitle(lapsInStint);
                ScatterPlotSeries newSeries = _dataExtractor.ExtractSeries(lapsInStint, _filters, seriesTitle, colorPaletteProvider.GetNext().ToOxyColor());
                scatterPlot.AddScatterPlotSeries(newSeries);
            }

            // Calculate gradient
            // Initialize lists to collect all X and Y values
            List<double> xData = new List<double>();
            List<double> yData = new List<double>();

            // Iterate through each series in the scatter plot
            foreach (var series in scatterPlot.ScatterPlotSeries)
            {
                // Add the X and Y values from the current series to the lists
                xData.AddRange(series.DataPoints.Select(p => p.X));
                yData.AddRange(series.DataPoints.Select(p => p.Y));
            }

            double xMean = xData.Average();
            double yMean = yData.Average();

            double numerator = 0;
            double denominator = 0;

            for (int i = 0; i < xData.Count; i++)
            {
                numerator += (xData[i] - xMean) * (yData[i] - yMean);
                denominator += (xData[i] - xMean) * (xData[i] - xMean);
            }

            double slope = numerator / denominator;
            double intercept = yMean - slope * xMean;

            string slopeText = $"Gradient: {slope:F2} deg/G";
            scatterPlot.Title += $" - {slopeText}";

            scatterPlot.AddAnnotation(new LineAnnotation() { Slope = slope, Intercept = 0, Color = OxyColors.Red, StrokeThickness = 1, LineStyle = LineStyle.Solid });
            ScatterPlotChartViewModel viewModel = new ScatterPlotChartViewModel(_dataPointSelectionSynchronization) { Title = title, HideNotSelectedPoints = _settingsController.TelemetrySettings.AggregatedChartSettings.HideNotSelectedPoints };
            viewModel.FromModel(scatterPlot);
            return new[] { viewModel };
        }

        public IReadOnlyCollection<IAggregatedChartViewModel> CreateChartForEachStint(IEnumerable<IGrouping<int, LapTelemetryDto>> lapsStintGrouping, AggregatedChartSettingsDto aggregatedChartSettings)
        {
            List<IAggregatedChartViewModel> charts = new List<IAggregatedChartViewModel>();

            foreach (IGrouping<int, LapTelemetryDto> lapsInStint in lapsStintGrouping)
            {
                string title = BuildChartTitle(lapsInStint, aggregatedChartSettings);
                AxisDefinition xAxis = new AxisDefinition(_dataExtractor.XMajorTickSize, _dataExtractor.XMajorTickSize / 4, _dataExtractor.XUnit, "Lat Accel (G)");
                AxisDefinition yAxis = new AxisDefinition(_dataExtractor.YMajorTickSize, _dataExtractor.YMajorTickSize / 4, _dataExtractor.YUnit, "Rear Roll (Deg)");
                ScatterPlot scatterPlot = new ScatterPlot(title, xAxis, yAxis);

                for (int i = 0; i < ColorMap.Count; i++)
                {
                    double minG = i * 0.25;
                    double maxG = i + 1 == ColorMap.Count ? double.MaxValue : (i + 1) * 0.25;
                    _lateralAccFilter.MinimumG = minG;
                    _lateralAccFilter.MaximumG = maxG;
                    string seriesTitle = maxG < double.MaxValue ? $"{minG:F2}G - {maxG:F2}G" : $"{minG:F2}G+";
                    ScatterPlotSeries newSeries = _dataExtractor.ExtractSeries(lapsInStint, _filters, seriesTitle, ColorMap[i]);
                    if (newSeries == null)
                    {
                        continue;
                    }

                    scatterPlot.AddScatterPlotSeries(newSeries);
                }

                // Calculate gradient
                // Initialize lists to collect all X and Y values
                List<double> xData = new List<double>();
                List<double> yData = new List<double>();

                // Iterate through each series in the scatter plot
                foreach (var series in scatterPlot.ScatterPlotSeries)
                {
                    // Add the X and Y values from the current series to the lists
                    xData.AddRange(series.DataPoints.Select(p => p.X));
                    yData.AddRange(series.DataPoints.Select(p => p.Y));
                }

                double xMean = xData.Average();
                double yMean = yData.Average();

                double numerator = 0;
                double denominator = 0;

                for (int i = 0; i < xData.Count; i++)
                {
                    numerator += (xData[i] - xMean) * (yData[i] - yMean);
                    denominator += (xData[i] - xMean) * (xData[i] - xMean);
                }

                double slope = numerator / denominator;
                double intercept = yMean - slope * xMean;

                string slopeText = $"Gradient: {slope:F2} deg/G";
                scatterPlot.Title += $" - {slopeText}";
                scatterPlot.AddAnnotation(new LineAnnotation() { Slope = slope, Intercept = 0, Color = OxyColors.Red, StrokeThickness = 1, LineStyle = LineStyle.Solid });
                ScatterPlotChartViewModel viewModel = new ScatterPlotChartViewModel(_dataPointSelectionSynchronization) { Title = title, HideNotSelectedPoints = _settingsController.TelemetrySettings.AggregatedChartSettings.HideNotSelectedPoints };
                viewModel.FromModel(scatterPlot);
                charts.Add(viewModel);
            }

            return charts;
        }
    }
}