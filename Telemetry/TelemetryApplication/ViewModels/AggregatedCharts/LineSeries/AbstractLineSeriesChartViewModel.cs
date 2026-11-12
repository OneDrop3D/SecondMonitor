namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.AggregatedCharts.LineSeries
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    using OxyPlot;
    using OxyPlot.Axes;
    using OxyPlot.Legends;

    using SecondMonitor.Contracts.Extensions;

    public abstract class AbstractLineSeriesChartViewModel : AbstractAggregatedChartViewModel
    {
        private static readonly TimeSpan UpdateDelay = TimeSpan.FromMilliseconds(100);
        private readonly Stopwatch _lastChangeTimer;
        private bool _invalidatingPlot;
        private double _yMinimum;
        private double _yMaximum;
        private double _xMinimum;
        private double _xMaximum;
        private bool _hasValidData;

        protected AbstractLineSeriesChartViewModel()
        {
            _xMaximum = _yMaximum = double.MinValue;
            _xMinimum = _yMinimum = double.MaxValue;
            XTickInterval = YTickInterval = 0;
            XUnits = string.Empty;
            YUnits = string.Empty;
            _lastChangeTimer = new Stopwatch();
            Legend legend = new Legend()
            {
                LegendBorderThickness = 1,
                LegendBorder = OxyColor.Parse("#FFD6D6D6"),
                LegendPlacement = LegendPlacement.Inside,
                TextColor = OxyColor.Parse("#FFD6D6D6"),
            };
            PlotModel = new PlotModel()
            {
                TextColor = OxyColor.Parse("#FFD6D6D6"),
                Background = OxyColors.Black,
                SubtitleColor = OxyColor.Parse("#FFD6D6D6"),
                PlotAreaBorderColor = OxyColors.White,
            };

            PlotModel.Legends.Add(legend);
        }

        public PlotModel PlotModel { get; }

        protected LinearAxis YAxis { get; private set; }
        protected LinearAxis XAxis { get; private set; }

        protected string YUnits { get; set; }
        protected double YTickInterval { get; set; }

        public bool HasValidData
        {
            get => _hasValidData;
            set => SetProperty(ref _hasValidData, value);
        }

        protected double YMinimum
        {
            get => _yMinimum;
            set => SetProperty(ref _yMinimum, value, UpdateYAxis);
        }

        protected double YMaximum
        {
            get => _yMaximum;
            set => SetProperty(ref _yMaximum, value, UpdateYAxis);
        }

        protected double XMinimum
        {
            get => _xMinimum;
            set => SetProperty(ref _xMinimum, value, UpdateXAxis);
        }

        protected double XMaximum
        {
            get => _xMaximum;
            set => SetProperty(ref _xMaximum, value, UpdateXAxis);
        }

        protected string XUnits { get; set; }
        protected double XTickInterval { get; set; }

        private void UpdateYAxis(double oldValue, double newValue)
        {
            if (YAxis == null)
            {
                return;
            }

            YAxis.Maximum = YMaximum;
            YAxis.Minimum = YMinimum;
            InvalidatePlot();
        }

        private void UpdateXAxis(double oldValue, double newValue)
        {
            if (XAxis == null)
            {
                return;
            }

            XAxis.Maximum = XMaximum;
            XAxis.Minimum = XMinimum;
            InvalidatePlot();
        }

        protected void InvalidatePlot()
        {
            InvalidatePlotAsync().FireAndForget();
        }

        protected async Task InvalidatePlotAsync()
        {
            if ((_invalidatingPlot && !HasValidData) || PlotModel?.PlotView == null)
            {
                return;
            }

            if (_invalidatingPlot)
            {
                _lastChangeTimer.Restart();
                return;
            }

            _invalidatingPlot = true;
            while (_lastChangeTimer.Elapsed < UpdateDelay)
            {
                await Task.Delay(UpdateDelay);
            }

            PlotModel.PlotView.InvalidatePlot(false);
            _invalidatingPlot = false;
        }

        protected void CheckAndCreateAxis()
        {
            if (YAxis == null)
            {
                CreateYAxis();
            }

            if (XAxis == null)
            {
                CreateXAxis();
            }
        }

        private void CreateYAxis()
        {
            YAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = YMinimum,
                Maximum = YMaximum,
                TickStyle = TickStyle.Inside,
                AxislineColor = OxyColor.Parse("#FFD6D6D6"),
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Unit = YUnits,
                AxisTitleDistance = 0,
                AxisDistance = 0,
                TicklineColor = OxyColor.Parse("#FFD6D6D6"),
            };

            if (YTickInterval > 0)
            {
                YAxis.MajorStep = Math.Round(YTickInterval, 2);
                YAxis.MajorGridlineStyle = LineStyle.Solid;
                YAxis.MinorGridlineStyle = LineStyle.Dot;
                YAxis.MajorGridlineColor = OxyColors.White;
                YAxis.MinorGridlineColor = OxyColors.White;
            }

            PlotModel.Axes.Add(YAxis);
        }

        private void CreateXAxis()
        {
            XAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = XMinimum,
                Maximum = XMaximum,
                TickStyle = TickStyle.Inside,
                AxislineColor = OxyColor.Parse("#FFD6D6D6"),
                MajorGridlineColor = OxyColor.Parse("#464239"),
                MajorGridlineStyle = LineStyle.Solid,
                AxisTitleDistance = 0,
                AxisDistance = 0,
                TicklineColor = OxyColor.Parse("#FFD6D6D6"),
                MinorTicklineColor = OxyColor.Parse("#FFD6D6D6"),
                ExtraGridlineColor = OxyColors.Red,
                ExtraGridlineThickness = 1,
            };

            if (XTickInterval > 0)
            {
                XAxis.MajorStep = Math.Round(XTickInterval * 5, 2);
                XAxis.MajorGridlineStyle = LineStyle.Solid;
                XAxis.MinorGridlineStyle = LineStyle.None;
                XAxis.MajorGridlineColor = OxyColors.White;
                XAxis.MinorGridlineColor = OxyColors.White;
                XAxis.ExtraGridlineColor = OxyColors.Red;
                XAxis.ExtraGridlineThickness = 2;
                XAxis.ExtraGridlines = new double[] { 0 };
            }

            PlotModel.Axes.Add(XAxis);
        }
    }
}