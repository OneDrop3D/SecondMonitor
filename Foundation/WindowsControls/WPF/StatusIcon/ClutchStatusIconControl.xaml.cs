namespace SecondMonitor.WindowsControls.WPF.StatusIcon
{
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for StatusIconControl.xaml
    /// </summary>
    public partial class ClutchStatusIconControl : UserControl
    {
        public static readonly DependencyProperty StatusIconProperty = DependencyProperty.Register(nameof(StatusIcon), typeof(Viewbox), typeof(ClutchStatusIconControl), new PropertyMetadata(default(Viewbox)));

        public static readonly DependencyProperty IconMarginProperty = DependencyProperty.Register(nameof(IconMargin), typeof(Thickness), typeof(ClutchStatusIconControl), new PropertyMetadata(new Thickness(5)));

        public static readonly DependencyProperty IconMaxWidthProperty = DependencyProperty.Register(nameof(IconMaxWidth), typeof(double), typeof(ClutchStatusIconControl), new PropertyMetadata(default(double)));

        public ClutchStatusIconControl()
        {
            InitializeComponent();
        }

        public double IconMaxWidth
        {
            get => (double)GetValue(IconMaxWidthProperty);
            set => SetValue(IconMaxWidthProperty, value);
        }

        public Thickness IconMargin
        {
            get => (Thickness)GetValue(IconMarginProperty);
            set => SetValue(IconMarginProperty, value);
        }

        public Viewbox StatusIcon
        {
            get => (Viewbox)GetValue(StatusIconProperty);
            set => SetValue(StatusIconProperty, value);
        }
    }
}
