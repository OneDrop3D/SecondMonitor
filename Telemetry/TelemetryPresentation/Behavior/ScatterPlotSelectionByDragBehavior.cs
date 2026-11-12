namespace SecondMonitor.TelemetryPresentation.Behavior
{
    using System.Windows;
    using System.Windows.Input;
    using Microsoft.Xaml.Behaviors;
    using OxyPlot;
    using OxyPlot.SkiaSharp.Wpf;
    using Telemetry.TelemetryApplication.ViewModels.AggregatedCharts.ScatterPlot;

    public class ScatterPlotSelectionByDragBehavior : Behavior<PlotView>
    {
        public static readonly DependencyProperty ScatterPlotChartViewModelProperty = DependencyProperty.Register("ScatterPlotChartViewModel", typeof(ScatterPlotChartViewModel), typeof(ScatterPlotSelectionByDragBehavior), new PropertyMetadata(ScatterPlotChartViewModelChanged));

        private bool _isTracking;
        private Point _startTrackingPoint;

        public ScatterPlotChartViewModel ScatterPlotChartViewModel
        {
            get => (ScatterPlotChartViewModel)GetValue(ScatterPlotChartViewModelProperty);
            set => SetValue(ScatterPlotChartViewModelProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            Subscribe();
        }

        private static void ScatterPlotChartViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is ScatterPlotSelectionByDragBehavior behavior) || e.OldValue == e.NewValue)
            {
                return;
            }

            behavior.Unsubscribe();
            behavior.Subscribe();
        }

        private void AssociatedObjectOnMouseMove(object sender, OxyMouseEventArgs e)
        {
            if (ScatterPlotChartViewModel == null || !_isTracking)
            {
                return;
            }

            if (Mouse.RightButton == MouseButtonState.Released || !(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.LeftAlt)))
            {
                _isTracking = false;
                ScatterPlotChartViewModel.HideSelectionRectangle();
                if (Keyboard.IsKeyDown(Key.LeftAlt))
                {
                    ScatterPlotChartViewModel.DeSelectPointsInArea(_startTrackingPoint, new Point(e.Position.X, e.Position.Y));
                }
                else
                {
                    ScatterPlotChartViewModel.SelectPointsInArea(_startTrackingPoint, new Point(e.Position.X, e.Position.Y));
                }

                return;
            }

            ScatterPlotChartViewModel.MoveSelectionRectangle(_startTrackingPoint, new Point(e.Position.X, e.Position.Y));
        }

        private void AssociatedObjectOnMouseUp(object sender, OxyMouseEventArgs e)
        {
            if (ScatterPlotChartViewModel == null || Mouse.LeftButton == MouseButtonState.Pressed || !(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.LeftAlt)) || !_isTracking)
            {
                return;
            }

            _isTracking = false;
            ScatterPlotChartViewModel.HideSelectionRectangle();
            if (Keyboard.IsKeyDown(Key.LeftAlt))
            {
                ScatterPlotChartViewModel.DeSelectPointsInArea(_startTrackingPoint, new Point(e.Position.X, e.Position.Y));
            }
            else
            {
                ScatterPlotChartViewModel.SelectPointsInArea(_startTrackingPoint, new Point(e.Position.X, e.Position.Y));
            }
        }

        private void AssociatedObjectOnMouseDown(object sender, OxyMouseDownEventArgs e)
        {
            if (ScatterPlotChartViewModel == null || Mouse.LeftButton == MouseButtonState.Pressed || !(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.LeftAlt)) || _isTracking)
            {
                return;
            }

            _isTracking = true;
            _startTrackingPoint = new Point(e.Position.X, e.Position.Y);
            ScatterPlotChartViewModel.ShowSelectionRectangle(_startTrackingPoint, new Point(e.Position.X + 10, e.Position.Y + 10), Keyboard.IsKeyDown(Key.LeftAlt));
        }

        protected override void OnDetaching()
        {
            Unsubscribe();
            base.OnDetaching();
        }

        private void Unsubscribe()
        {
            if (AssociatedObject?.Model == null)
            {
                return;
            }

            AssociatedObject.Model.MouseDown -= AssociatedObjectOnMouseDown;
            AssociatedObject.Model.MouseUp -= AssociatedObjectOnMouseUp;
            AssociatedObject.Model.MouseMove -= AssociatedObjectOnMouseMove;
        }

        private void Subscribe()
        {
            if (AssociatedObject?.Model == null)
            {
                return;
            }

            AssociatedObject.Model.MouseDown += AssociatedObjectOnMouseDown;
            AssociatedObject.Model.MouseUp += AssociatedObjectOnMouseUp;
            AssociatedObject.Model.MouseMove += AssociatedObjectOnMouseMove;
        }
    }
}