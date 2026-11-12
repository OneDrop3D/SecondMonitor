namespace SecondMonitor.WindowsControls.WPF.Converters
{
    using System;
    using System.ComponentModel;
    using System.Windows;

    using Microsoft.Xaml.Behaviors;

    public class BottomMarginPercentageBehavior : Behavior<FrameworkElement>
    {
        public static readonly DependencyProperty PercentageProperty = DependencyProperty.Register(
            nameof(Percentage), typeof(double), typeof(BottomMarginPercentageBehavior), new FrameworkPropertyMetadata() { PropertyChangedCallback = OnPercentagesPropertyChanged });

        public static readonly DependencyProperty SubtractAssociatedObjectHeightProperty = DependencyProperty.Register(
            nameof(SubtractAssociatedObjectHeight), typeof(bool), typeof(BottomMarginPercentageBehavior), new PropertyMetadata(default(bool)));

        public bool SubtractAssociatedObjectHeight
        {
            get => (bool)GetValue(SubtractAssociatedObjectHeightProperty);
            set => SetValue(SubtractAssociatedObjectHeightProperty, value);
        }

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
            UpdateMarginPosition();
        }

        private void ParentObjectOnActualHeightChanged(object sender, EventArgs e)
        {
            UpdateMarginPosition();
        }

        private static void OnPercentagesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BottomMarginPercentageBehavior xPositionInversePercentageBehavior)
            {
                xPositionInversePercentageBehavior.UpdateMarginPosition();
            }
        }

        private void UpdateMarginPosition()
        {
            if (AssociatedObject == null || _parentElement == null)
            {
                return;
            }

            double yPosition = Math.Max(_parentElement.ActualHeight * (Percentage / 100), 0);

            if (SubtractAssociatedObjectHeight)
            {
                yPosition = Math.Max(0, yPosition - AssociatedObject.ActualHeight);
            }

            if (yPosition + AssociatedObject.ActualHeight >= _parentElement.ActualHeight)
            {
                yPosition = _parentElement.ActualHeight - AssociatedObject.ActualHeight;
            }

            AssociatedObject.Margin = new Thickness(AssociatedObject.Margin.Left, 0, AssociatedObject.Margin.Right, yPosition);
        }
    }
}