namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using System.Windows.Media;

    using Controllers.Synchronization;
    using Controllers.Synchronization.Graphs;
    using DataModel.BasicProperties;
    using DataModel.BasicProperties.Units;
    using DataModel.Telemetry;
    using NLog;
    using OxyPlot;
    using OxyPlot.Annotations;
    using OxyPlot.Axes;
    using OxyPlot.Legends;
    using OxyPlot.Series;

    using SecondMonitor.Contracts.Extensions;
    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Colors.Extensions;
    using Settings.DTO;
    using TelemetryManagement.DTO;
    using TelemetryManagement.Settings;
    using Wheels;

    public abstract class AbstractGraphViewModel : AbstractViewModel, IGraphViewModel
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly TimeSpan UpdateDelay = TimeSpan.FromMilliseconds(100);
        private readonly Dictionary<string, LineAnnotation> _selectedXValue;
        private readonly Stopwatch _lastChangeTimer;
        private readonly Dictionary<string, CarPropertiesDto> _lapCarSettingsCache;
        private LinearAxis _yAxis;
        private LinearAxis _xAxis;
        private PlotModel _plotModel;
        private bool _invalidatingPlot;
        private double _yMaximum;
        private bool _updating;
        private bool _hasValidData;
        private ILapColorSynchronization _lapColorSynchronization;
        private IGraphViewSynchronization _graphViewSynchronization;
        private double _yMinimum;
        private bool _syncWithOtherGraphs;

        private double _xMaximum;
        private double? _lapDistanceSector1;
        private double? _lapDistanceSector2;

        protected AbstractGraphViewModel()
        {
            _lapCarSettingsCache = new Dictionary<string, CarPropertiesDto>();
            SyncWithOtherGraphs = true;
            _lastChangeTimer = new Stopwatch();
            LoadedSeries = new Dictionary<string, (LapTelemetryDto Telemetry, List<LineSeries> LineSeries)>();
            _selectedXValue = new Dictionary<string, LineAnnotation>();
            GraphDetailViewModel = new GraphDetailViewModel();
            InitializeViewModel();
        }

        protected Dictionary<string, (LapTelemetryDto Telemetry, List<LineSeries> LineSeries)> LoadedSeries { get; }

        public bool HasValidData
        {
            get => _hasValidData;
            set => SetProperty(ref _hasValidData, value);
        }

        public XAxisKind XAxisKind { get; set; }

        public GraphDetailViewModel GraphDetailViewModel { get; }

        public PlotModel PlotModel
        {
            get => _plotModel;
            set
            {
                _plotModel = value;
                NotifyPropertyChanged();
            }
        }

        public bool SyncWithOtherGraphs
        {
            get => _syncWithOtherGraphs;
            set => SetProperty(ref _syncWithOtherGraphs, value);
        }

        public ILapColorSynchronization LapColorSynchronization
        {
            get => _lapColorSynchronization;
            set
            {
                UnsubscribeLapColorSync();
                _lapColorSynchronization = value;
                SubscribeLapColorSync();
            }
        }

        public IGraphViewSynchronization GraphViewSynchronization
        {
            get => _graphViewSynchronization;
            set
            {
                UnsubscribeGraphViewSync();
                _graphViewSynchronization = value;
                SubscribeGraphViewSync();
            }
        }

        public virtual bool IsCarSettingsDependant => false;

        public UnitsCollection UnitsCollection { get; set; }

        protected double YMaximum
        {
            get => _yMaximum;
            set
            {
                SetProperty(ref _yMaximum, value);
                UpdateYAxis();
            }
        }

        protected double YMinimum
        {
            get => _yMinimum;
            set
            {
                SetProperty(ref _yMinimum, value);
                UpdateYAxis();
            }
        }

        protected double XMaximum
        {
            get => _xMaximum;
            set
            {
                _xMaximum = value;
                UpdateXAxis();
            }
        }

        public abstract string Title { get; }
        protected abstract string YUnits { get; }
        protected abstract double YTickInterval { get; }
        protected abstract bool CanYZoom { get; }

        protected virtual bool AutoApplyYChartLimit => true;

        private void InitializeViewModel()
        {
            Legend legend = new Legend()
            {
                LegendBorderThickness = 1,
                LegendBorder = OxyColor.Parse("#FFD6D6D6"),
                LegendPlacement = LegendPlacement.Outside,
                TextColor = OxyColor.Parse("#FFD6D6D6"),
            };
            _plotModel = new PlotModel()
            {
                TextColor = OxyColor.Parse("#FFD6D6D6"),
                Background = OxyColors.Black,
                SubtitleColor = OxyColor.Parse("#FFD6D6D6"),
            };

            _plotModel.Legends.Add(legend);
        }

        public void AddLapTelemetry(LapTelemetryDto lapTelemetryDto, CarPropertiesDto carPropertiesDto)
        {
            Color color = GetLapColor(lapTelemetryDto.LapSummary).ToColor();
            if (LoadedSeries.ContainsKey(lapTelemetryDto.LapSummary.Id))
            {
                RemoveLapTelemetry(lapTelemetryDto.LapSummary);
            }

            _lapCarSettingsCache[lapTelemetryDto.LapSummary.Id] = carPropertiesDto;

            CheckAndCreateAxis();

            List<TimedTelemetrySnapshot> dataPoints = lapTelemetryDto.DataPoints.OrderBy(x => x.PlayerData.LapDistance).ToList();
            //TimedTelemetrySnapshot[] dataPoints = lapTelemetryDto.TimedTelemetrySnapshots.OrderBy(x => x.PlayerData.LapDistance).WhereWithPrevious(FilterFunction).ToArray();
            List<LineSeries> series = GetLineSeries(lapTelemetryDto.LapSummary, carPropertiesDto, dataPoints, OxyColor.Parse(color.ToString()));

            if (series.Count == 0)
            {
                LoadedSeries.Add(lapTelemetryDto.LapSummary.Id, (lapTelemetryDto, series));
                return;
            }

            PreviewOnLapLoaded(lapTelemetryDto, carPropertiesDto);
            ApplyChartLimits(series);

            var selectionAnnotation = new LineAnnotation() { Color = color.ToOxyColor(), StrokeThickness = 1, Type = LineAnnotationType.Vertical, LineStyle = LineStyle.Solid };
            _selectedXValue[lapTelemetryDto.LapSummary.Id] = selectionAnnotation;
            _plotModel.Annotations.Add(selectionAnnotation);
            LoadedSeries.Add(lapTelemetryDto.LapSummary.Id, (lapTelemetryDto, series));
            CheckIfHasValidData();
            InitializeSectorDistance(dataPoints);
            AddSectorGridLines();
            if (HasValidData)
            {
                series.ForEach(_plotModel.Series.Add);
                _plotModel.InvalidatePlot(true);
                NotifyPropertyChanged(nameof(PlotModel));
                NotifyPropertyChanged(nameof(PlotModel.Series));
            }

            _plotModel.MouseDown += PlotModelOnMouseDown;
            _plotModel.MouseMove += PlotModelOnMouseMove;
        }

        public void AddLapTelemetries(IEnumerable<(LapTelemetryDto LapTelemetryDto, CarPropertiesDto CarPropertiesDto)> lapSummaryDtos)
        {
            lapSummaryDtos.ForEach(x => AddLapTelemetry(x.LapTelemetryDto, x.CarPropertiesDto));
        }

        private void PlotModelOnMouseMove(object sender, OxyMouseEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Released)
            {
                return;
            }

            double value = _xAxis.InverseTransform(e.Position.X);
            if (XAxisKind == XAxisKind.LapDistance)
            {
                _graphViewSynchronization.RequestDistanceSelectionForAll(value);
            }
            else
            {
                _graphViewSynchronization.RequestTimeSelectionForAll(TimeSpan.FromSeconds(value));
            }
        }

        private void PlotModelOnMouseDown(object sender, OxyMouseDownEventArgs e)
        {
            if (e.ChangedButton != OxyMouseButton.Left)
            {
                return;
            }

            double value = _xAxis.InverseTransform(e.Position.X);
            if (XAxisKind == XAxisKind.LapDistance)
            {
                _graphViewSynchronization.RequestDistanceSelectionForAll(value);
            }
            else
            {
                _graphViewSynchronization.RequestTimeSelectionForAll(TimeSpan.FromSeconds(value));
            }
        }

        private void ApplyChartLimits(List<LineSeries> series)
        {
            double maxY = 0.0;
            double minY = YMinimum;
            double maxX = 0.0;
            foreach (var x in series)
            {
                maxX = Math.Max(maxX, x.Points.Max(y => y.X));
                maxY = Math.Max(maxY, x.Points.Max(y => y.Y));
                minY = Math.Min(minY, x.Points.Min(y => y.Y));
            }

            minY += minY * 0.1;
            maxY += maxY * 0.1;
            XMaximum = maxX;
            if (AutoApplyYChartLimit)
            {
                YMinimum = minY;
                YMaximum = maxY;
            }
        }

        protected void RecreateAllLineSeries()
        {
            YMaximum = 0;
            YMinimum = 0;
            _yAxis?.Reset();
            InvalidatePlot();

            try
            {
                List<LapTelemetryDto> loadedLaps = LoadedSeries.Values.Select(x => x.Telemetry).ToList();
                loadedLaps.ForEach(x => RemoveLapTelemetry(x.LapSummary));
                loadedLaps.ForEach(x => AddLapTelemetry(x, x.CarPropertiesDto));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void InitializeSectorDistance(List<TimedTelemetrySnapshot> dataPoints)
        {
            _lapDistanceSector1 = dataPoints.LastOrDefault(x => x.PlayerData.Timing.CurrentSector == 1)?.PlayerData.LapDistance;
            _lapDistanceSector2 = dataPoints.LastOrDefault(x => x.PlayerData.Timing.CurrentSector == 2)?.PlayerData.LapDistance;
        }

        private void AddSectorGridLines()
        {
            if (XAxisKind == XAxisKind.LapTime)
            {
                _xAxis.ExtraGridlines = Array.Empty<double>();
            }

            if (!_lapDistanceSector1.HasValue || !_lapDistanceSector2.HasValue)
            {
                return;
            }

            double[] xValues = new[] { Distance.FromMeters(_lapDistanceSector1.Value).GetByUnit(UnitsCollection.DistanceUnits), Distance.FromMeters(_lapDistanceSector2.Value).GetByUnit(UnitsCollection.DistanceUnits) };
            _xAxis.ExtraGridlines = xValues;
        }

        private void CheckIfHasValidData()
        {
            bool hasValidData = false;
            foreach (List<LineSeries> loadedSeriesValue in LoadedSeries.Values.Select(x => x.LineSeries))
            {
                if (hasValidData)
                {
                    HasValidData = true;
                    return;
                }

                loadedSeriesValue.ForEach(x => hasValidData = hasValidData || x.Points.Any(y => y.Y != x.Points.First().Y));
            }

            HasValidData = hasValidData;
        }

        private void CheckAndCreateAxis()
        {
            if (_yAxis == null)
            {
                CreateYAxis();
            }

            if (_xAxis == null)
            {
                CreateXAxis();
            }
        }

        private void CreateYAxis()
        {
            _yAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = YMinimum,
                Maximum = YMaximum,
                TickStyle = TickStyle.Inside,
                AxislineColor = OxyColor.Parse("#FFD6D6D6"),
                IsZoomEnabled = CanYZoom,
                IsPanEnabled = CanYZoom,
                Unit = YUnits,
                AxisTitleDistance = 0,
                AxisDistance = 0,
                ExtraGridlineColor = OxyColors.Red,
                ExtraGridlines = new[] { 0.0 },
                ExtraGridlineThickness = 1.5,
                TicklineColor = OxyColor.Parse("#FFD6D6D6"),
                MinorTicklineColor = OxyColor.Parse("#FFD6D6D6"),
            };

            if (YTickInterval > 0)
            {
                _yAxis.MajorStep = Math.Round(YTickInterval, 2);
                _yAxis.MajorGridlineStyle = LineStyle.Solid;
                _yAxis.MajorGridlineThickness = 1;
                _yAxis.MajorGridlineColor = OxyColor.Parse("#FF7F7F7F");
            }

            _plotModel.Axes.Add(_yAxis);
        }

        private void CreateXAxis()
        {
            _xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = -1,
                Maximum = 0,
                TickStyle = TickStyle.Inside,
                AxislineColor = OxyColor.Parse("#FFD6D6D6"),
                MajorStep = XAxisKind == XAxisKind.LapTime ? 20 : 200,
                MajorGridlineColor = OxyColor.Parse("#464239"),
                MajorGridlineStyle = LineStyle.Solid,
                AxisTitleDistance = 0,
                AxisDistance = 0,
                TicklineColor = OxyColor.Parse("#FFD6D6D6"),
                MinorTicklineColor = OxyColor.Parse("#FFD6D6D6"),
                ExtraGridlineColor = OxyColors.Red,
                ExtraGridlineThickness = 1,
            };
            _plotModel.Axes.Add(_xAxis);
            _xAxis.AxisChanged += XAxisOnAxisChanged;
        }

        private void XAxisOnAxisChanged(object sender, AxisChangedEventArgs e)
        {
            if (_updating || !_syncWithOtherGraphs)
            {
                return;
            }

            if (e.ChangeType == AxisChangeTypes.Reset)
            {
                _updating = true;
                _xAxis.Minimum = 0;
                _xAxis.Maximum = _xMaximum;
                _xAxis.Reset();
                _updating = false;
                _graphViewSynchronization.NotifyXAxisReset(this);
            }
            else
            {
                _graphViewSynchronization.NotifyPanChanged(this, _xAxis.ActualMinimum, _xAxis.ActualMaximum);
            }
        }

        public void RemoveLapTelemetry(LapSummaryDto lapSummaryDto)
        {
            if (LoadedSeries.TryGetValue(lapSummaryDto.Id, out (LapTelemetryDto Telemetry, List<LineSeries> LineSeries) value))
            {
                LoadedSeries.Remove(lapSummaryDto.Id);
                value.LineSeries.ForEach(x => _plotModel.Series.Remove(x));
            }

            if (_selectedXValue.TryGetValue(lapSummaryDto.Id, out LineAnnotation lineAnnotation))
            {
                _plotModel.Annotations.Remove(lineAnnotation);
                _selectedXValue.Remove(lapSummaryDto.Id);
            }

            _lapCarSettingsCache.Remove(lapSummaryDto.Id);
            GraphDetailViewModel.RemoveLapDetail(lapSummaryDto.Id);

            InvalidatePlot();
            if (LoadedSeries.Count == 0)
            {
                XMaximum = 0;
            }
        }

        public void UpdateXSelection(LapSummaryDto lapSummary, TimedTelemetrySnapshot timedTelemetrySnapshot)
        {
            if (!_selectedXValue.TryGetValue(lapSummary.Id, out LineAnnotation value))
            {
                return;
            }

            value.X = GetXValue(timedTelemetrySnapshot);
            UpdateGraphDetails(lapSummary, timedTelemetrySnapshot);
            InvalidatePlot();
        }

        public void RemoveAllLapsTelemetry()
        {
            _plotModel.Series.Clear();
            _plotModel.Annotations.Clear();
            _selectedXValue.Clear();
            LoadedSeries.Clear();
            XMaximum = 0;
            InvalidatePlot();
        }

        protected double GetXValue(TimedTelemetrySnapshot timedTelemetrySnapshot)
        {
            switch (XAxisKind)
            {
                case XAxisKind.LapDistance:
                    return Distance.FromMeters(timedTelemetrySnapshot.PlayerData.LapDistance).GetByUnit(UnitsCollection.DistanceUnits);
                case XAxisKind.LapTime:
                    return timedTelemetrySnapshot.LapTimeSeconds;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected void InvalidatePlot()
        {
            InvalidatePlotAsync().FireAndForget();
        }

        protected async Task InvalidatePlotAsync()
        {
            if ((_invalidatingPlot && !HasValidData) || _plotModel?.PlotView == null)
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

            _plotModel.PlotView.InvalidatePlot(false);
            _invalidatingPlot = false;
        }

        private void UpdateYAxis()
        {
            if (_yAxis == null)
            {
                return;
            }

            _yAxis.Maximum = YMaximum;
            _yAxis.Minimum = YMinimum;
            InvalidatePlot();
        }

        private void UpdateXAxis()
        {
            if (_xAxis == null)
            {
                return;
            }

            _xAxis.Maximum = XMaximum;
            InvalidatePlot();
        }

        protected virtual void SubscribeGraphViewSync()
        {
            if (_graphViewSynchronization == null)
            {
                return;
            }

            _graphViewSynchronization.PanChanged += GraphViewSynchronizationOnPanChanged;
            _graphViewSynchronization.XAxisReset += GraphViewSynchronizationOnXAxisReset;
        }

        protected virtual void UnsubscribeGraphViewSync()
        {
            if (_graphViewSynchronization == null)
            {
                return;
            }

            _graphViewSynchronization.PanChanged -= GraphViewSynchronizationOnPanChanged;
            _graphViewSynchronization.XAxisReset -= GraphViewSynchronizationOnXAxisReset;
        }

        private void GraphViewSynchronizationOnXAxisReset(object sender, EventArgs e)
        {
            if (!SyncWithOtherGraphs || ReferenceEquals(sender, this) || _xAxis == null)
            {
                return;
            }

            _updating = true;
            _xAxis.Minimum = 0;
            _xAxis.Maximum = _xMaximum;
            _xAxis.Reset();
            InvalidatePlot();
            _updating = false;
        }

        private void GraphViewSynchronizationOnPanChanged(object sender, PanEventArgs e)
        {
            if (!SyncWithOtherGraphs || ReferenceEquals(sender, this) || _xAxis == null)
            {
                return;
            }

            _updating = true;
            _xAxis.Minimum = e.Minimum;
            _xAxis.Maximum = e.Maximum;
            _xAxis.Reset();
            InvalidatePlot();
            _updating = false;
        }

        protected virtual void UnsubscribeLapColorSync()
        {
            if (_lapColorSynchronization == null)
            {
                return;
            }

            _lapColorSynchronization.LapColorChanged -= LapColorSynchronizationOnLapColorChanged;
        }

        private void SubscribeLapColorSync()
        {
            if (_lapColorSynchronization == null)
            {
                return;
            }

            _lapColorSynchronization.LapColorChanged += LapColorSynchronizationOnLapColorChanged;
        }

        private void LapColorSynchronizationOnLapColorChanged(object sender, LapColorArgs e)
        {
            if (_selectedXValue.TryGetValue(e.LapId, out LineAnnotation value))
            {
                value.Color = e.Color.ToColor().ToOxyColor();
            }

            if (LoadedSeries.TryGetValue(e.LapId, out (LapTelemetryDto Telemetry, List<LineSeries> LineSeries) series))
            {
                OxyColor color = e.Color.ToColor().ToOxyColor();
                ApplyNewLineColor(series.LineSeries, color);
            }

            InvalidatePlot();
        }

        protected void AddLineAnnotations(List<Annotation> lineAnnotations)
        {
            lineAnnotations.ForEach(_plotModel.Annotations.Add);
        }

        protected void RemoveLineAnnotations(List<Annotation> lineAnnotations)
        {
            lineAnnotations.ForEach(x => _plotModel.Annotations.Remove(x));
        }

        protected virtual void ApplyNewLineColor(List<LineSeries> series, OxyColor newColor)
        {
            foreach (LineSeries lineSeries in series)
            {
                lineSeries.Color = newColor;
                lineSeries.TextColor = newColor;
            }
        }

        private ColorDto GetLapColor(LapSummaryDto lapSummaryDto)
        {
            if (LapColorSynchronization == null || !LapColorSynchronization.TryGetColorForLap(lapSummaryDto.Id, out ColorDto color))
            {
                color = ColorDto.RedColor;
            }

            return color;
        }

        protected abstract List<LineSeries> GetLineSeries(LapSummaryDto lapSummary, CarPropertiesDto carPropertiesDto,  List<TimedTelemetrySnapshot> dataPoints, OxyColor color);

        protected LineSeries CreateLineSeries(string title, OxyColor color, LineStyle lineStyle = LineStyle.Solid)
        {
            return new LineSeries
            {
                Title = title,
                Color = color,
                TextColor = OxyColors.Black,
                InterpolationAlgorithm = null,
                CanTrackerInterpolatePoints = false,
                StrokeThickness = 2,
                LineStyle = lineStyle,
                TrackerFormatString = "{0}\n" + (XAxisKind == XAxisKind.LapTime ? "s" : Distance.GetUnitsSymbol(UnitsCollection.DistanceUnits)) + ": {2}\n" + YUnits + ": {4}",
            };
        }

        public virtual void UpdateGraphDetails(LapSummaryDto lapSummary, TimedTelemetrySnapshot timedTelemetrySnapshot)
        {
            var lapDetail = GraphDetailViewModel.GetOrCreateLapDetail(lapSummary.Id);

            lapDetail.LapName = lapSummary.CustomDisplayName;
            lapDetail.DataInfo = GetDataInfo(timedTelemetrySnapshot, _lapCarSettingsCache[lapSummary.Id]);
        }

        protected abstract string GetDataInfo(TimedTelemetrySnapshot timedTelemetrySnapshot, CarPropertiesDto carPropertiesDto);

        protected virtual void PreviewOnLapLoaded(LapTelemetryDto lapTelemetryDto, CarPropertiesDto carPropertiesDto)
        {
        }

        //protected abstract bool FilterFunction(TimedTelemetrySnapshot previousSnapshot, TimedTelemetrySnapshot currentSnapshot);

        public virtual void Dispose()
        {
            UnsubscribeLapColorSync();
            UnsubscribeGraphViewSync();

            if (_plotModel != null)
            {
                _plotModel.MouseDown -= PlotModelOnMouseDown;
                _plotModel.MouseMove -= PlotModelOnMouseMove;
            }

            if (_xAxis != null)
            {
                _xAxis.AxisChanged -= XAxisOnAxisChanged;
            }

            LoadedSeries.Clear();
        }
    }
}