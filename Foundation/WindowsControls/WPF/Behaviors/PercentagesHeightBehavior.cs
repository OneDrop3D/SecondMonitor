namespace SecondMonitor.WindowsControls.WPF.Behaviors
{
    using System;
    using System.ComponentModel;
    using System.Windows;

    using Microsoft.Xaml.Behaviors;

    public class PercentagesHeightBehavior : Behavior<FrameworkElement>
    { 
        public static readonly DependencyProperty PercentageProperty = DependencyProperty.Register(nameof(Percentage), typeof(double), typeof(PercentagesHeightBehavior), new FrameworkPropertyMetadata() { PropertyChangedCallback = OnPercentagesPropertyChanged });

        private FrameworkElement _parentElement;

        public double Percentage
        {
            get => (double)GetValue(PercentageProperty);
            set => SetValue(PercentageProperty, value);
        }

        protected override void OnAttached()
        {
            _parentElement = (FrameworkElement)LogicalTreeHelper.GetParent(AssociatedObject);
            if (_parentElement == null)
            {
                return;
            }

            DependencyPropertyDescriptor.FromProperty(FrameworkElement.ActualHeightProperty, typeof(FrameworkElement)).AddValueChanged(_parentElement, ParentObjectOnActualHeightChanged);
            UpdateHeight();
        }

        private void ParentObjectOnActualHeightChanged(object sender, EventArgs e)
        {
            UpdateHeight();
        }

        private static void OnPercentagesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PercentagesHeightBehavior percentagesHeightBehavior)
            {
                percentagesHeightBehavior.UpdateHeight();
            }
        }

        private void UpdateHeight()
        {
            if (AssociatedObject == null || _parentElement == null || _parentElement.ActualHeight > 2000 || Percentage < 0 || Percentage > 100)
            {
                return;
            }

            AssociatedObject.Height = _parentElement.ActualHeight * (Percentage / 100);
        }
    }
}