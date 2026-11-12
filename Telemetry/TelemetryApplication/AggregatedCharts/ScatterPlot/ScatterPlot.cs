namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts.ScatterPlot
{
    using System.Collections.Generic;
    using OxyPlot.Annotations;

    public class ScatterPlot
    {
        private readonly List<ScatterPlotSeries> _scatterPlotSeries;
        private readonly List<Annotation> _annotations;

        public ScatterPlot()
        {
            _scatterPlotSeries = new List<ScatterPlotSeries>();
            _annotations = new List<Annotation>();
            IsLegendVisible = true;
        }

        public ScatterPlot(string title, AxisDefinition xAxis, AxisDefinition yAxis) : this()
        {
            Title = title;
            XAxis = xAxis;
            YAxis = yAxis;
        }

        public string Title { get; set; }
        public AxisDefinition XAxis { get; set; }
        public AxisDefinition YAxis { get; set; }
        public bool IsLegendVisible { get; set; }

        public IReadOnlyCollection<Annotation> Annotations => _annotations.AsReadOnly();

        public IReadOnlyCollection<ScatterPlotSeries> ScatterPlotSeries => _scatterPlotSeries.AsReadOnly();

        public void AddScatterPlotSeries(ScatterPlotSeries newSeries)
        {
            if (newSeries == null)
            {
                return;
            }

            _scatterPlotSeries.Add(newSeries);
        }

        public void AddAnnotation(Annotation annotation)
        {
            _annotations.Add(annotation);
        }
    }
}