namespace SecondMonitor.TelemetryPresentation.Behavior
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using Microsoft.Xaml.Behaviors;
    using Telemetry.TelemetryApplication.ViewModels.MapView;
    using WindowsControls.WPF;

    public class MapPointsSelectionsBehavior : Behavior<ContentPresenter>
    {
        private TrackMapWithCustomPathsViewModel _trackMapViewModel;
        private Path _selectionPath;
        private bool _isTracking;
        private Point _startTrackingPoint;
        private Grid _anchorFrameworkElement;
        private Panel _renderPanel;

        public static readonly DependencyProperty AnchorGridNameProperty = DependencyProperty.Register(
            "AnchorGridName", typeof(string), typeof(MapPointsSelectionsBehavior), new PropertyMetadata(default(string)));

        public static readonly DependencyProperty RenderPanelNameProperty = DependencyProperty.Register(
            "RenderPanelName", typeof(string), typeof(MapPointsSelectionsBehavior), new PropertyMetadata(default(string)));

        public string RenderPanelName
        {
            get => (string)GetValue(RenderPanelNameProperty);
            set => SetValue(RenderPanelNameProperty, value);
        }

        public string AnchorGridName
        {
            get => (string)GetValue(AnchorGridNameProperty);
            set => SetValue(AnchorGridNameProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject == null)
            {
                return;
            }

            _anchorFrameworkElement = VisualHelper.FindAncestorByName<Grid>(AssociatedObject, AnchorGridName);
            if (AssociatedObject.ContentTemplate != null)
            {
                _renderPanel = AssociatedObject.ContentTemplate.FindName(RenderPanelName, AssociatedObject) as Grid;
            }

            Subscribe();
            AssociatedObject.Loaded += AssociatedObjectOnDataContextChanged;
            DependencyPropertyDescriptor.FromProperty(ContentPresenter.ContentTemplateProperty, typeof(ContentPresenter)).AddValueChanged(AssociatedObject, AssociatedObjectOnDataContextChanged);
            DependencyPropertyDescriptor.FromProperty(ContentPresenter.ContentProperty, typeof(ContentPresenter)).AddValueChanged(AssociatedObject, AssociatedObjectOnDataContextChanged);
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject == null || _anchorFrameworkElement == null)
            {
                return;
            }

            UnSubscribe();
            DependencyPropertyDescriptor.FromProperty(ContentPresenter.ContentTemplateProperty, typeof(ContentPresenter)).RemoveValueChanged(AssociatedObject, AssociatedObjectOnDataContextChanged);
            DependencyPropertyDescriptor.FromProperty(ContentPresenter.ContentProperty, typeof(ContentPresenter)).RemoveValueChanged(AssociatedObject, AssociatedObjectOnDataContextChanged);
        }

        private void AssociatedObjectOnDataContextChanged(object sender, EventArgs e)
        {
            if (AssociatedObject == null)
            {
                return;
            }

            UnSubscribe();
            _trackMapViewModel = AssociatedObject.Content as TrackMapWithCustomPathsViewModel;
            _anchorFrameworkElement = VisualHelper.FindAncestorByName<Grid>(AssociatedObject, AnchorGridName);
            _renderPanel = VisualHelper.FindDescendantByName<Panel>(AssociatedObject, RenderPanelName);
            if (AssociatedObject.ContentTemplate != null)
            {
                _renderPanel = AssociatedObject.ContentTemplate.FindName(RenderPanelName, AssociatedObject) as Grid;
            }

            Subscribe();
        }

        private void Subscribe()
        {
            if (_trackMapViewModel == null || _anchorFrameworkElement == null)
            {
                return;
            }

            _anchorFrameworkElement.PreviewMouseDown += FullMapControlOnPreviewMouseDown;
            _anchorFrameworkElement.PreviewMouseMove += FullMapControlOnPreviewMouseMove;
            _anchorFrameworkElement.PreviewMouseUp += FullMapControlOnPreviewMouseUp;
            _anchorFrameworkElement.MouseLeave += AnchorFrameworkElementOnMouseLeave;
        }

        private void AnchorFrameworkElementOnMouseLeave(object sender, MouseEventArgs e)
        {
            if (_trackMapViewModel == null || !_isTracking)
            {
                return;
            }

            e.Handled = true;
            _isTracking = false;
            _anchorFrameworkElement.Children.Remove(_selectionPath);
        }

        private void UnSubscribe()
        {
            if (_trackMapViewModel == null || _anchorFrameworkElement == null)
            {
                return;
            }

            _anchorFrameworkElement.PreviewMouseDown -= FullMapControlOnPreviewMouseDown;
            _anchorFrameworkElement.PreviewMouseMove -= FullMapControlOnPreviewMouseMove;
            _anchorFrameworkElement.PreviewMouseUp -= FullMapControlOnPreviewMouseUp;
            _anchorFrameworkElement.MouseLeave -= AnchorFrameworkElementOnMouseLeave;
        }

        private void FullMapControlOnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_trackMapViewModel == null || !_isTracking)
            {
                return;
            }

            if (e.RightButton == MouseButtonState.Released || !(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.LeftAlt)))
            {
                _isTracking = false;
                _anchorFrameworkElement.Children.Remove(_selectionPath);
                return;
            }

            Point currentPoint = e.GetPosition(_anchorFrameworkElement);
            ((RectangleGeometry)_selectionPath.Data).Rect = new Rect(_startTrackingPoint, currentPoint);
            e.Handled = true;
        }

        private void FullMapControlOnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_renderPanel == null)
            {
                _renderPanel = VisualHelper.FindDescendantByName<Panel>(AssociatedObject, RenderPanelName);
            }

            if (_renderPanel == null || _trackMapViewModel == null || e.ChangedButton != MouseButton.Right || !(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.LeftAlt)) || _isTracking)
            {
                return;
            }

            _startTrackingPoint = e.GetPosition(_anchorFrameworkElement);

            _isTracking = true;
            _selectionPath = new Path
            {
                Fill = Keyboard.IsKeyDown(Key.LeftAlt) ? Brushes.Red : Brushes.Blue,
                Opacity = 0.5,
                Data = new RectangleGeometry(new Rect(_startTrackingPoint, new Size(5, 5)))
            };
            _anchorFrameworkElement.Children.Add(_selectionPath);
            e.Handled = true;
        }

        private void FullMapControlOnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_trackMapViewModel == null || e.ChangedButton != MouseButton.Right || !(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.LeftAlt)) || !_isTracking || _renderPanel == null)
            {
                return;
            }

            _isTracking = false;
            _anchorFrameworkElement.Children.Remove(_selectionPath);
            Point mapPoint1 = _anchorFrameworkElement.TranslatePoint(_startTrackingPoint, _renderPanel);
            Point mapPoint2 = _anchorFrameworkElement.TranslatePoint(e.GetPosition(_anchorFrameworkElement), _renderPanel);
            if (Keyboard.IsKeyDown(Key.LeftAlt))
            {
                _trackMapViewModel.DeselectTelemetryPointsInArea(mapPoint1, mapPoint2);
            }
            else
            {
                _trackMapViewModel.SelectTelemetryPointsInArea(mapPoint1, mapPoint2);
            }
        }
    }
}