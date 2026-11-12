namespace SecondMonitor.TelemetryPresentation.Behavior
{
    using System.Windows;
    using System.Windows.Input;
    using Microsoft.Xaml.Behaviors;
    using OxyPlot;
    using OxyPlot.SkiaSharp.Wpf;
    using Telemetry.TelemetryApplication.ViewModels.AggregatedCharts.Histogram;

    public class BandSelectionByClickBehavior : Behavior<PlotView>
    {
        public static readonly DependencyProperty HistogramChartViewModelProperty = DependencyProperty.Register("HistogramChartViewModel", typeof(HistogramChartViewModel), typeof(BandSelectionByClickBehavior), new PropertyMetadata(OnViewModelChanged));

        public HistogramChartViewModel HistogramChartViewModel
        {
            get => (HistogramChartViewModel)GetValue(HistogramChartViewModelProperty);
            set => SetValue(HistogramChartViewModelProperty, value);
        }

        private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BandSelectionByClickBehavior behavior && e.NewValue != e.OldValue)
            {
                behavior.Unsubscribe();
                behavior.Subscribe();
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            Subscribe();
        }

        private void Subscribe()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.Model.MouseDown += AssociatedObjectOnMouseUp;
            }
        }

        private void AssociatedObjectOnMouseUp(object sender, OxyMouseDownEventArgs e)
        {
            if (HistogramChartViewModel == null || e.ChangedButton == OxyMouseButton.Left || !Keyboard.IsKeyDown(Key.LeftShift))
            {
                return;
            }

            HistogramChartViewModel.ToggleSelection(new Point(e.Position.X, e.Position.Y));
        }

        protected override void OnDetaching()
        {
            Unsubscribe();
            base.OnDetaching();
        }

        private void Unsubscribe()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.Model.MouseDown -= AssociatedObjectOnMouseUp;
            }
        }
    }
}