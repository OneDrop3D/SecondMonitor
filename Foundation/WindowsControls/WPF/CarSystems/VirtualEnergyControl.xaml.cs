namespace SecondMonitor.WindowsControls.WPF.CarSystems
{
    using System.Windows;
    using System.Windows.Controls;

    public partial class VirtualEnergyControl : UserControl
    {
        public static readonly DependencyProperty VirtualEnergyPercentageProperty =
            DependencyProperty.Register(nameof(VirtualEnergyPercentage), typeof(double), typeof(VirtualEnergyControl));

        public double VirtualEnergyPercentage
        {
            get => (double)GetValue(VirtualEnergyPercentageProperty);
            set => SetValue(VirtualEnergyPercentageProperty, value);
        }

        public VirtualEnergyControl()
        {
            InitializeComponent();
        }
    }
}
