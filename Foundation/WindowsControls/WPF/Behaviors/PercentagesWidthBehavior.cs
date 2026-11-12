namespace SecondMonitor.WindowsControls.WPF.Behaviors
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using Microsoft.Xaml.Behaviors;

    public class PercentagesWidthBehavior : Behavior<FrameworkElement>
    {
        public static readonly DependencyProperty PercentageProperty = DependencyProperty.Register("Percentage", typeof(double), typeof(PercentagesWidthBehavior), new FrameworkPropertyMetadata() { PropertyChangedCallback = OnPercentagesPropertyChanged });

        private FrameworkElement _parentElement;

        public double Percentage
        {
            get => (double)GetValue(PercentageProperty);
            set => SetValue(PercentageProperty, value);
        }

        protected override void OnAttached()
        {
            _parentElement = (FrameworkElement)LogicalTreeHelper.GetParent(AssociatedObject);
            DependencyPropertyDescriptor.FromProperty(FrameworkElement.ActualWidthProperty, typeof(FrameworkElement)).AddValueChanged(_parentElement, ParentObjectOnActualWidthChanged);
            UpdateWidth();
        }

        private void ParentObjectOnActualWidthChanged(object sender, EventArgs e)
        {
            UpdateWidth();
        }

        private static void OnPercentagesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PercentagesWidthBehavior percentagesWidthBehavior)
            {
                percentagesWidthBehavior.UpdateWidth();
            }
        }

        private void UpdateWidth()
        {
            if (AssociatedObject == null || _parentElement == null || Percentage < 0 || Percentage > 100)
            {
                return;
            }

            double computedWith = _parentElement.ActualWidth * (Percentage / 100);
            if (computedWith > 0)
            {
                AssociatedObject.Width = computedWith;
            }
        }
    }
}