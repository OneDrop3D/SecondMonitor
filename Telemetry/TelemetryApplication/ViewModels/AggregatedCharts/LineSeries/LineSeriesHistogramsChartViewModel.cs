namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.AggregatedCharts.LineSeries
{
    using System;
    using System.Linq;

    using OxyPlot;
    using OxyPlot.Series;

    using SecondMonitor.ViewModels.Colors;
    using SecondMonitor.ViewModels.Colors.Extensions;

    using TelemetryApplication.AggregatedCharts.Histogram;

    public class LineSeriesHistogramsChartViewModel : AbstractLineSeriesChartViewModel
    {
        private readonly IColorPaletteProvider _colorPaletteProvider;

        public LineSeriesHistogramsChartViewModel(IColorPaletteProvider colorPaletteProvider)
        {
            _colorPaletteProvider = colorPaletteProvider;
            YUnits = "%";
            YTickInterval = 5;
        }

        public void AddSeries(Histogram histogram)
        {
            PlotModel.Title = Title;
            XUnits = histogram.Unit;
            XTickInterval = histogram.BandSize;
            UpdateAxisProperties(histogram);
            CheckAndCreateAxis();
            CreateLineSeries(histogram);
        }

        private void UpdateAxisProperties(Histogram histogram)
        {
            YMinimum = Math.Min(YMinimum, histogram.MinimumY);
            YMaximum = Math.Max(YMaximum, histogram.MaximumY);

            XMinimum = Math.Min(XMinimum, histogram.MinimumX);
            XMaximum = Math.Max(XMaximum, histogram.MaximumX);

            if (-XMinimum > XMaximum)
            {
                XMaximum = -XMinimum;
            }
            else
            {
                XMinimum = -XMaximum;
            }
        }

        private void CreateLineSeries(Histogram histogram)
        {
            LineSeries newSeries = new LineSeries
            {
                Title = histogram.Title,
                Color = _colorPaletteProvider.GetNext().ToOxyColor(),
                TextColor = OxyColors.Black,
                InterpolationAlgorithm = null,
                CanTrackerInterpolatePoints = false,
                StrokeThickness = 2,
                LineStyle = LineStyle.Solid,
                TrackerFormatString = "{0}\n" + XUnits + ": {2}\n" + YUnits + ": {4}",
            };

            newSeries.Points.AddRange(histogram.Items.SelectMany(x => x.Items).OrderBy(x => x.Category).Select(x => new DataPoint(x.Category, x.Percentage)));
            PlotModel.Series.Add(newSeries);
        }
    }
}
