namespace SecondMonitor.WindowsControls.WPF.DriverPosition
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using ViewModels;
    using ViewModels.Track;

    public class DriverByLapDistancePositionControl : Control
    {
        private const string RenderGridName = "PART_DriversGrid";
        private const int DefaultSize = 30;

        private static readonly DependencyProperty PlayerForegroundBrushProperty = DependencyProperty.Register(nameof(PlayerForegroundBrush), typeof(SolidColorBrush), typeof(DriverByLapDistancePositionControl), new FrameworkPropertyMetadata() { PropertyChangedCallback = OnRedrawBoundPropertyChangePropertyChangedCallback });
        private static readonly DependencyProperty PlayerBackgroundBrushProperty = DependencyProperty.Register(nameof(PlayerBackgroundBrush), typeof(SolidColorBrush), typeof(DriverByLapDistancePositionControl), new FrameworkPropertyMetadata() { PropertyChangedCallback = OnRedrawBoundPropertyChangePropertyChangedCallback });
        private static readonly DependencyProperty DriverForegroundBrushProperty = DependencyProperty.Register(nameof(DriverForegroundBrush), typeof(SolidColorBrush), typeof(DriverByLapDistancePositionControl), new FrameworkPropertyMetadata() { PropertyChangedCallback = OnRedrawBoundPropertyChangePropertyChangedCallback });
        private static readonly DependencyProperty DriverBackgroundBrushProperty = DependencyProperty.Register(nameof(DriverBackgroundBrush), typeof(SolidColorBrush), typeof(DriverByLapDistancePositionControl), new FrameworkPropertyMetadata() { PropertyChangedCallback = OnRedrawBoundPropertyChangePropertyChangedCallback });
        private static readonly DependencyProperty LappedDriverForegroundBrushProperty = DependencyProperty.Register(nameof(LappedDriverForegroundBrush), typeof(SolidColorBrush), typeof(DriverByLapDistancePositionControl), new FrameworkPropertyMetadata() { PropertyChangedCallback = OnRedrawBoundPropertyChangePropertyChangedCallback });
        private static readonly DependencyProperty LappedDriverBackgroundBrushProperty = DependencyProperty.Register(nameof(LappedDriverBackgroundBrush), typeof(SolidColorBrush), typeof(DriverByLapDistancePositionControl), new FrameworkPropertyMetadata() { PropertyChangedCallback = OnRedrawBoundPropertyChangePropertyChangedCallback });
        private static readonly DependencyProperty LappingDriverForegroundBrushProperty = DependencyProperty.Register(nameof(LappingDriverForegroundBrush), typeof(SolidColorBrush), typeof(DriverByLapDistancePositionControl), new FrameworkPropertyMetadata() { PropertyChangedCallback = OnRedrawBoundPropertyChangePropertyChangedCallback });
        private static readonly DependencyProperty LappingDriverBackgroundBrushProperty = DependencyProperty.Register(nameof(LappingDriverBackgroundBrush), typeof(SolidColorBrush), typeof(DriverByLapDistancePositionControl), new FrameworkPropertyMetadata() { PropertyChangedCallback = OnRedrawBoundPropertyChangePropertyChangedCallback });
        private static readonly DependencyProperty DriverPitsForegroundBrushProperty = DependencyProperty.Register(nameof(DriverPitsForegroundBrush), typeof(SolidColorBrush), typeof(DriverByLapDistancePositionControl), new FrameworkPropertyMetadata() { PropertyChangedCallback = OnRedrawBoundPropertyChangePropertyChangedCallback });
        private static readonly DependencyProperty DriverPitsBackgroundBrushProperty = DependencyProperty.Register(nameof(DriverPitsBackgroundBrush), typeof(SolidColorBrush), typeof(DriverByLapDistancePositionControl), new FrameworkPropertyMetadata() { PropertyChangedCallback = OnRedrawBoundPropertyChangePropertyChangedCallback });
        public static readonly DependencyProperty PlayerOutLineBrushProperty = DependencyProperty.Register(nameof(PlayerOutLineBrush), typeof(SolidColorBrush), typeof(DriverByLapDistancePositionControl), new FrameworkPropertyMetadata() { PropertyChangedCallback = OnRedrawBoundPropertyChangePropertyChangedCallback });
        public static readonly DependencyProperty DriverPitsMovingBackgroundProperty = DependencyProperty.Register(nameof(DriverPitsMovingBackground), typeof(SolidColorBrush), typeof(DriverByLapDistancePositionControl), new FrameworkPropertyMetadata() { PropertyChangedCallback = OnRedrawBoundPropertyChangePropertyChangedCallback });
        public static readonly DependencyProperty YellowSectorBrushProperty = DependencyProperty.Register(nameof(YellowSectorBrush), typeof(SolidColorBrush), typeof(DriverByLapDistancePositionControl), new FrameworkPropertyMetadata() { PropertyChangedCallback = OnRedrawBoundPropertyChangePropertyChangedCallback });
        private static readonly DependencyProperty PositionProperty = DependencyProperty.Register(nameof(Position), typeof(int), typeof(DriverByLapDistancePositionControl));
        private static readonly DependencyProperty CircleBrushProperty = DependencyProperty.Register(nameof(CircleBrush), typeof(SolidColorBrush), typeof(DriverByLapDistancePositionControl));
        private static readonly DependencyProperty TextBrushProperty = DependencyProperty.Register(nameof(TextBrush), typeof(SolidColorBrush), typeof(DriverByLapDistancePositionControl));
        public static readonly DependencyProperty XProperty = DependencyProperty.Register(nameof(X), typeof(double), typeof(DriverByLapDistancePositionControl), new FrameworkPropertyMetadata() { PropertyChangedCallback = OnXPropertyChanged });
        public static readonly DependencyProperty YProperty = DependencyProperty.Register(nameof(Y), typeof(double), typeof(DriverByLapDistancePositionControl), new FrameworkPropertyMetadata() { PropertyChangedCallback = OnYPropertyChanged });
        public static readonly DependencyProperty OutLineColorProperty = DependencyProperty.Register(nameof(OutLineColor), typeof(SolidColorBrush), typeof(DriverByLapDistancePositionControl), new PropertyMetadata(Brushes.Transparent));
        public static readonly DependencyProperty DriverViewModelProperty = DependencyProperty.Register(nameof(DriverViewModel), typeof(DriverPositionViewModel), typeof(DriverByLapDistancePositionControl), new PropertyMetadata(default(DriverPositionViewModel), PropertyChangedCallback));
        public static readonly DependencyProperty ClassIndicationBrushProperty = DependencyProperty.Register(nameof(ClassIndicationBrush), typeof(SolidColorBrush), typeof(DriverByLapDistancePositionControl), new PropertyMetadata(default(SolidColorBrush)));
        public static readonly DependencyProperty IsClassColorIndicationEnabledProperty = DependencyProperty.Register(nameof(IsClassColorIndicationEnabled), typeof(bool), typeof(DriverByLapDistancePositionControl), new PropertyMetadata(default(bool)));
        public static readonly DependencyProperty AnimateProperty = DependencyProperty.Register(nameof(Animate), typeof(bool), typeof(DriverByLapDistancePositionControl), new PropertyMetadata(default(bool)) { PropertyChangedCallback = OnAnimatePropertyChangeCallback });
        public static readonly DependencyProperty DriverFinishedForegroundProperty = DependencyProperty.Register(nameof(DriverFinishedForeground), typeof(SolidColorBrush), typeof(DriverByLapDistancePositionControl), new PropertyMetadata(default(SolidColorBrush)));
        public static readonly DependencyProperty DriverFinishedBackgroundProperty = DependencyProperty.Register(nameof(DriverFinishedBackground), typeof(SolidColorBrush), typeof(DriverByLapDistancePositionControl), new PropertyMetadata(default(SolidColorBrush)));
        public static readonly DependencyProperty PitStopsVisualizationProperty = DependencyProperty.Register(nameof(PitStopsVisualization), typeof(AbstractViewModel), typeof(DriverByLapDistancePositionControl), new PropertyMetadata(default(AbstractViewModel)));

        private readonly TimeSpan _initialAnimationTime = TimeSpan.FromMilliseconds(100);
        private readonly Stopwatch _lastYAnimationWatch;
        private readonly Stopwatch _lastXAnimationWatch;
        private TranslateTransform _translateTransform;

        public DriverByLapDistancePositionControl()
        {
            Unloaded += OnUnloaded;
            _translateTransform = new TranslateTransform(0, 0);
            RenderTransform = _translateTransform;
            _lastYAnimationWatch = new Stopwatch();
            _lastXAnimationWatch = new Stopwatch();
        }

        public SolidColorBrush DriverFinishedBackground
        {
            get => (SolidColorBrush)GetValue(DriverFinishedBackgroundProperty);
            set => SetValue(DriverFinishedBackgroundProperty, value);
        }

        public AbstractViewModel PitStopsVisualization
        {
            get => (AbstractViewModel)GetValue(PitStopsVisualizationProperty);
            set => SetValue(PitStopsVisualizationProperty, value);
        }

        public SolidColorBrush DriverFinishedForeground
        {
            get => (SolidColorBrush)GetValue(DriverFinishedForegroundProperty);
            set => SetValue(DriverFinishedForegroundProperty, value);
        }

        public SolidColorBrush DriverPitsMovingBackground
        {
            get => (SolidColorBrush)GetValue(DriverPitsMovingBackgroundProperty);
            set => SetValue(DriverPitsMovingBackgroundProperty, value);
        }

        public SolidColorBrush PlayerOutLineBrush
        {
            get => (SolidColorBrush)GetValue(PlayerOutLineBrushProperty);
            set => SetValue(PlayerOutLineBrushProperty, value);
        }

        public SolidColorBrush PlayerForegroundBrush
        {
            get => (SolidColorBrush)GetValue(PlayerForegroundBrushProperty);
            set => SetValue(PlayerForegroundBrushProperty, value);
        }

        public SolidColorBrush PlayerBackgroundBrush
        {
            get => (SolidColorBrush)GetValue(PlayerBackgroundBrushProperty);
            set => SetValue(PlayerBackgroundBrushProperty, value);
        }

        public SolidColorBrush DriverForegroundBrush
        {
            get => (SolidColorBrush)GetValue(DriverForegroundBrushProperty);
            set => SetValue(DriverForegroundBrushProperty, value);
        }

        public SolidColorBrush DriverBackgroundBrush
        {
            get => (SolidColorBrush)GetValue(DriverBackgroundBrushProperty);
            set => SetValue(DriverBackgroundBrushProperty, value);
        }

        public SolidColorBrush DriverPitsForegroundBrush
        {
            get => (SolidColorBrush)GetValue(DriverPitsForegroundBrushProperty);
            set => SetValue(DriverPitsForegroundBrushProperty, value);
        }

        public SolidColorBrush DriverPitsBackgroundBrush
        {
            get => (SolidColorBrush)GetValue(DriverPitsBackgroundBrushProperty);
            set => SetValue(DriverPitsBackgroundBrushProperty, value);
        }

        public SolidColorBrush LappedDriverForegroundBrush
        {
            get => (SolidColorBrush)GetValue(LappedDriverForegroundBrushProperty);
            set => SetValue(LappedDriverForegroundBrushProperty, value);
        }

        public SolidColorBrush LappedDriverBackgroundBrush
        {
            get => (SolidColorBrush)GetValue(LappedDriverBackgroundBrushProperty);
            set => SetValue(LappedDriverBackgroundBrushProperty, value);
        }

        public SolidColorBrush LappingDriverForegroundBrush
        {
            get => (SolidColorBrush)GetValue(LappingDriverForegroundBrushProperty);
            set => SetValue(LappingDriverForegroundBrushProperty, value);
        }

        public SolidColorBrush LappingDriverBackgroundBrush
        {
            get => (SolidColorBrush)GetValue(LappingDriverBackgroundBrushProperty);
            set => SetValue(LappingDriverBackgroundBrushProperty, value);
        }

        public SolidColorBrush YellowSectorBrush
        {
            get => (SolidColorBrush)GetValue(YellowSectorBrushProperty);
            set => SetValue(YellowSectorBrushProperty, value);
        }

        public DriverPositionViewModel DriverViewModel
        {
            get => (DriverPositionViewModel)GetValue(DriverViewModelProperty);
            set => SetValue(DriverViewModelProperty, value);
        }

        public bool IsClassColorIndicationEnabled
        {
            get => (bool)GetValue(IsClassColorIndicationEnabledProperty);
            set => SetValue(IsClassColorIndicationEnabledProperty, value);
        }

        public SolidColorBrush ClassIndicationBrush
        {
            get => (SolidColorBrush)GetValue(ClassIndicationBrushProperty);
            set => SetValue(ClassIndicationBrushProperty, value);
        }

        public SolidColorBrush OutLineColor
        {
            get => (SolidColorBrush)GetValue(OutLineColorProperty);
            set => SetValue(OutLineColorProperty, value);
        }

        public SolidColorBrush CircleBrush
        {
            get => (SolidColorBrush)GetValue(CircleBrushProperty);
            set => SetValue(CircleBrushProperty, value);
        }

        public SolidColorBrush TextBrush
        {
            get => (SolidColorBrush)GetValue(TextBrushProperty);
            set => SetValue(TextBrushProperty, value);
        }

        public int Position
        {
            get => (int)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        public double X
        {
            get => (double)GetValue(XProperty);
            set => SetValue(XProperty, value);
        }

        public double Y
        {
            get => (double)GetValue(YProperty);
            set => SetValue(YProperty, value);
        }

        public bool Animate
        {
            get => (bool)GetValue(AnimateProperty);
            set => SetValue(AnimateProperty, value);
        }

        protected double DesiredHeight { get; set; }

        protected double DesiredWidth { get; set; }

        protected Panel RenderGrid { get; private set; }

        protected void UpdateAll()
        {
            if (DriverViewModel == null)
            {
                return;
            }

            UpdatePosition();
            UpdateVisual();
            Position = DriverViewModel.Position;
        }

        protected virtual void UpdatePosition()
        {
            UpdateCoordinatesByLapDistance();
        }

        private void UpdateVisual()
        {
            if (DriverViewModel == null)
            {
                return;
            }

            UpdateSize();
            var driverViewModel = DriverViewModel;
            ClassIndicationBrush = driverViewModel.ClassIndicationColor.ToSolidColorBrush();
            if (driverViewModel.IsPlayer)
            {
                SetZIndex(5);
                Width = DesiredWidth * 1.2;
                Height = DesiredHeight * 1.2;
                if (DriverViewModel.DriverState == DriverState.Yellow)
                {
                    Height = DesiredHeight * 1.2;
                    Width = DesiredWidth * 1.2;
                    CircleBrush = YellowSectorBrush;
                    TextBrush = PlayerForegroundBrush;
                }
                else
                {
                    CircleBrush = PlayerBackgroundBrush;
                    TextBrush = PlayerForegroundBrush;
                }

                OutLineColor = PlayerOutLineBrush;
                return;
            }

            OutLineColor = driverViewModel.OutLineColor != null ? driverViewModel.OutLineColor.ToSolidColorBrush() : Brushes.Transparent;

            switch (driverViewModel.DriverState)
            {
                case DriverState.Finished:
                    SetZIndex(1);
                    CircleBrush = DriverFinishedBackground;
                    TextBrush = DriverFinishedForeground;
                    Height = DesiredHeight * 0.8;
                    Width = DesiredWidth * 0.8;
                    Visibility = Visibility.Visible;
                    break;
                case DriverState.InPits:
                    SetZIndex(1);
                    CircleBrush = DriverPitsBackgroundBrush;
                    TextBrush = DriverPitsForegroundBrush;
                    Height = DesiredHeight * 0.5;
                    Width = DesiredWidth * 0.5;
                    Visibility = Visibility.Visible;
                    break;
                case DriverState.Dnf:
                    SetZIndex(1);
                    CircleBrush = DriverPitsBackgroundBrush;
                    TextBrush = DriverPitsForegroundBrush;
                    Height = DesiredHeight * 0.5;
                    Width = DesiredWidth * 0.5;
                    Visibility = Visibility.Collapsed;
                    break;
                case DriverState.InPitsMoving:
                    SetZIndex(2);
                    Height = DesiredHeight * 0.8;
                    Width = DesiredWidth * 0.8;
                    CircleBrush = DriverPitsMovingBackground;
                    TextBrush = DriverPitsForegroundBrush;
                    Visibility = Visibility.Visible;
                    break;
                case DriverState.Yellow:
                    Height = DesiredHeight * 1.2;
                    Width = DesiredWidth * 1.2;
                    SetZIndex(5);
                    CircleBrush = YellowSectorBrush;
                    TextBrush = PlayerForegroundBrush;
                    Visibility = Visibility.Visible;
                    break;
                case DriverState.Lapped:
                    Height = DesiredHeight * 0.9;
                    Width = DesiredWidth * 0.9;
                    SetZIndex(1);
                    CircleBrush = LappedDriverBackgroundBrush;
                    TextBrush = LappedDriverForegroundBrush;
                    Visibility = Visibility.Visible;
                    break;
                case DriverState.Lapping:
                    Height = DesiredHeight * 0.9;
                    Width = DesiredWidth * 0.9;
                    SetZIndex(1);
                    CircleBrush = LappingDriverBackgroundBrush;
                    TextBrush = LappingDriverForegroundBrush;
                    Visibility = Visibility.Visible;
                    break;
                default:
                    Height = DesiredHeight;
                    Width = DesiredWidth;
                    SetZIndex(2);
                    CircleBrush = DriverBackgroundBrush;
                    TextBrush = DriverForegroundBrush;
                    Visibility = Visibility.Visible;
                    break;
            }
        }

        protected void SetZIndex(int zIndex)
        {
            Panel.SetZIndex(VisualHelper.FindUpVisualTree<ContentPresenter>(this), zIndex);
        }

        protected virtual void UpdateSize()
        {
            if (this.Visibility != Visibility.Visible)
            {
                return;
            }

            DesiredWidth = DefaultSize;
            DesiredHeight = DefaultSize;
        }

        protected virtual void OnDriverViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!CheckAccess())
            {
                Dispatcher.InvokeAsync(() => OnDriverViewModelPropertyChanged(sender, e));
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(DriverPositionViewModel.ClassIndicationColor):
                case nameof(DriverPositionViewModel.OutLineColor):
                case nameof(DriverPositionViewModel.IsPlayer):
                case nameof(DriverPositionViewModel.DriverState):
                    UpdateAll();
                    break;
                case nameof(DriverPositionViewModel.LapCompletedPercentage):
                    UpdateCoordinatesByLapDistance();
                    break;
                case nameof(DriverPositionViewModel.Position):
                    Position = DriverViewModel.Position;
                    break;
                case nameof(DriverPositionViewModel.PitStopsVisualization):
                    PitStopsVisualization = DriverViewModel.PitStopsVisualization;
                    break;
            }
        }

        protected virtual void UpdateCoordinatesByLapDistance()
        {
            if (RenderGrid == null && DriverViewModel != null)
            {
                return;
            }

            X = GetX();
            Y = GetY();
        }

        private double GetX()
        {
            double degrees = ((DriverViewModel.LapCompletedPercentage) * 2 * Math.PI) - (Math.PI / 2);
            double x = (RenderGrid.ActualWidth / 2) * Math.Cos(degrees);
            return double.IsNaN(x) ? 0 : x;
        }

        private double GetY()
        {
            double degrees = ((DriverViewModel.LapCompletedPercentage) * 2 * Math.PI) - (Math.PI / 2);
            double y = (RenderGrid.ActualHeight / 2) * Math.Sin(degrees);

            if (DriverViewModel.DriverState == DriverState.InPits || DriverViewModel.DriverState == DriverState.InPitsMoving)
            {
                y += 30;
            }

            return double.IsNaN(y) ? 0 : y;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            RenderGrid = VisualHelper.FindAncestorByName<Panel>(this, RenderGridName);
            if (RenderGrid == null)
            {
                return;
            }

            var pd = DependencyPropertyDescriptor.FromProperty(ActualHeightProperty, typeof(FrameworkElement));
            pd.AddValueChanged(RenderGrid, OnRenderGridSizeChanged);
            pd = DependencyPropertyDescriptor.FromProperty(ActualWidthProperty, typeof(FrameworkElement));
            pd.AddValueChanged(RenderGrid, OnRenderGridSizeChanged);
            UpdateAll();
        }

        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is DriverByLapDistancePositionControl driverPositionControl))
            {
                return;
            }

            if (e.OldValue is DriverPositionViewModel oldDriverPositionViewModel)
            {
                oldDriverPositionViewModel.PropertyChanged -= driverPositionControl.OnDriverViewModelPropertyChanged;
            }

            if (e.NewValue is DriverPositionViewModel newDriverPositionViewModel)
            {
                newDriverPositionViewModel.PropertyChanged += driverPositionControl.OnDriverViewModelPropertyChanged;
                driverPositionControl.PitStopsVisualization = newDriverPositionViewModel.PitStopsVisualization;
                driverPositionControl.UpdateAll();
            }
        }

        private static void OnYPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DriverByLapDistancePositionControl playerPositionControl)
            {
                playerPositionControl.OnYPropertyChanged();
            }
        }

        private static void OnXPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DriverByLapDistancePositionControl playerPositionControl)
            {
                playerPositionControl.OnXPropertyChanged();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (RenderGrid == null)
            {
                return;
            }

            DependencyPropertyDescriptor pd = DependencyPropertyDescriptor.FromProperty(ActualHeightProperty, typeof(FrameworkElement));
            pd?.RemoveValueChanged(RenderGrid, OnRenderGridSizeChanged);

            pd = DependencyPropertyDescriptor.FromProperty(ActualWidthProperty, typeof(FrameworkElement));
            pd?.RemoveValueChanged(RenderGrid, OnRenderGridSizeChanged);

            if (DriverViewModel != null)
            {
                DriverViewModel.PropertyChanged -= OnDriverViewModelPropertyChanged;
            }
        }

        private void OnRenderGridSizeChanged(object sender, EventArgs e)
        {
            UpdateAll();
        }

        private void OnYPropertyChanged()
        {
            if (Animate)
            {
                TimeSpan animationTime = _lastYAnimationWatch.IsRunning ? TimeSpan.FromMilliseconds(Math.Min(_lastYAnimationWatch.ElapsedMilliseconds * 1.2, 3000)) : _initialAnimationTime;
                _lastYAnimationWatch.Restart();
                DoubleAnimation da = new DoubleAnimation(Y, animationTime);
                Timeline.SetDesiredFrameRate(da, 20);
                _translateTransform.BeginAnimation(TranslateTransform.YProperty, da);
            }
            else
            {
                _translateTransform.Y = Y;
            }
        }

        private void OnXPropertyChanged()
        {
            if (Animate)
            {
                TimeSpan animationTime = _lastXAnimationWatch.IsRunning ? TimeSpan.FromMilliseconds(Math.Min(_lastXAnimationWatch.ElapsedMilliseconds * 1.2, 3000)) : _initialAnimationTime;
                _lastXAnimationWatch.Restart();
                DoubleAnimation da = new DoubleAnimation(X, animationTime);
                Timeline.SetDesiredFrameRate(da, 20);
                _translateTransform.BeginAnimation(TranslateTransform.XProperty, da);
            }
            else
            {
                _translateTransform.X = X;
            }
        }

        protected static void OnRedrawBoundPropertyChangePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DriverByLapDistancePositionControl driverPositionControl)
            {
                driverPositionControl.UpdateAll();
            }
        }

        private static void OnAnimatePropertyChangeCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DriverByLapDistancePositionControl driverPositionControl)
            {
                driverPositionControl._translateTransform = new TranslateTransform(driverPositionControl.X, driverPositionControl.Y);
                driverPositionControl.RenderTransform = driverPositionControl._translateTransform;
            }
        }
    }
}