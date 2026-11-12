namespace SecondMonitor.WindowsControls.WPF.DriverPosition
{
    using System.ComponentModel;
    using System.Windows;
    using ViewModels.Track;

    public class DriverByCoordinatesPositionControl : DriverByLapDistancePositionControl
    {
        public static readonly DependencyProperty IsAxisSwappedProperty = DependencyProperty.Register(nameof(IsAxisSwapped), typeof(bool), typeof(DriverByCoordinatesPositionControl), new FrameworkPropertyMetadata() { PropertyChangedCallback = OnRedrawBoundPropertyChangePropertyChangedCallback });
        public static readonly DependencyProperty XCoefProperty = DependencyProperty.Register(nameof(XCoef), typeof(double), typeof(DriverByCoordinatesPositionControl), new FrameworkPropertyMetadata() { PropertyChangedCallback = OnRedrawBoundPropertyChangePropertyChangedCallback });
        public static readonly DependencyProperty YCoefProperty = DependencyProperty.Register(nameof(YCoef), typeof(double), typeof(DriverByCoordinatesPositionControl), new FrameworkPropertyMetadata() { PropertyChangedCallback = OnRedrawBoundPropertyChangePropertyChangedCallback });
        public static readonly DependencyProperty ScaleDriverProperty = DependencyProperty.Register(nameof(ScaleDriver), typeof(bool), typeof(DriverByCoordinatesPositionControl), new FrameworkPropertyMetadata() { PropertyChangedCallback = OnRedrawBoundPropertyChangePropertyChangedCallback });
        public static readonly DependencyProperty DriverScaleProperty = DependencyProperty.Register(nameof(DriverScale), typeof(double), typeof(DriverByCoordinatesPositionControl), new FrameworkPropertyMetadata() { PropertyChangedCallback = OnRedrawBoundPropertyChangePropertyChangedCallback });

        public double DriverScale
        {
            get => (double)GetValue(DriverScaleProperty);
            set => SetValue(DriverScaleProperty, value);
        }

        public bool ScaleDriver
        {
            get => (bool)GetValue(ScaleDriverProperty);
            set => SetValue(ScaleDriverProperty, value);
        }

        public double YCoef
        {
            get => (double)GetValue(YCoefProperty);
            set => SetValue(YCoefProperty, value);
        }

        public double XCoef
        {
            get => (double)GetValue(XCoefProperty);
            set => SetValue(XCoefProperty, value);
        }

        public bool IsAxisSwapped
        {
            get => (bool)GetValue(IsAxisSwappedProperty);
            set => SetValue(IsAxisSwappedProperty, value);
        }

        protected override void OnDriverViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
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
                case nameof(DriverPositionViewModel.X):
                case nameof(DriverPositionViewModel.Y):
                    UpdateByCoordinates();
                    break;
                case nameof(DriverPositionViewModel.Position):
                    Position = DriverViewModel.Position;
                    break;
                case nameof(DriverPositionViewModel.PitStopsVisualization):
                    PitStopsVisualization = DriverViewModel.PitStopsVisualization;
                    break;
            }
        }

        protected override void UpdatePosition()
        {
            UpdateByCoordinates();
        }

        protected override void UpdateSize()
        {
            if (this.Visibility != Visibility.Visible)
            {
                return;
            }

            if (!ScaleDriver)
            {
                base.UpdateSize();
                return;
            }

            DesiredWidth = RenderGrid.ActualHeight * DriverScale;
            DesiredHeight = RenderGrid.ActualHeight * DriverScale;
        }

        private void UpdateByCoordinates()
        {
            X = GetX();
            Y = GetY();
        }

        private double GetX()
        {
            DriverPositionViewModel viewModel = DriverViewModel;
            double xCoord = IsAxisSwapped ? viewModel.Y * YCoef : viewModel.X * XCoef;
            return xCoord - (ActualWidth / 2);
        }

        private double GetY()
        {
            DriverPositionViewModel viewModel = DriverViewModel;
            double yCoord = IsAxisSwapped ? viewModel.X * XCoef : viewModel.Y * YCoef;
            return yCoord - (ActualHeight / 2);
        }
    }
}