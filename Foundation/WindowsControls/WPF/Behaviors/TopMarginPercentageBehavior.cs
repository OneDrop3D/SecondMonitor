namespace SecondMonitor.WindowsControls.WPF.Behaviors
{
    using System;
    using System.Windows;
    using Microsoft.Xaml.Behaviors;

    public class TopMarginPercentageBehavior : Behavior<FrameworkElement>
    {
        public static readonly DependencyProperty PercentageProperty = DependencyProperty.Register("Percentage", typeof(double), typeof(TopMarginPercentageBehavior), new FrameworkPropertyMetadata() { PropertyChangedCallback = OnPercentagesPropertyChanged });

        private FrameworkElement _parentElement;

        public double Percentage
        {
            get => (double)GetValue(PercentageProperty);
            set => SetValue(PercentageProperty, value);
        }

        protected override void OnAttached()
        {
            _parentElement = (FrameworkElement)LogicalTreeHelper.GetParent(AssociatedObject);
        }

        private static void OnPercentagesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TopMarginPercentageBehavior topMarginPercentageBehavior)
            {
                topMarginPercentageBehavior.UpdateYPosition();
            }
        }

        private void UpdateYPosition()
        {
            if (AssociatedObject == null || _parentElement == null || Percentage < 0 || Percentage > 100)
            {
                return;
            }

            double yPosition = Math.Max((_parentElement.ActualHeight * (Percentage / 100)) - AssociatedObject.ActualHeight, 0);
            AssociatedObject.Margin = new Thickness(0, yPosition, 0, 0);
        }
    }
}