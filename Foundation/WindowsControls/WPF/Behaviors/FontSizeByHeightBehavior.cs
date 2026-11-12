namespace SecondMonitor.WindowsControls.WPF.Behaviors
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.Xaml.Behaviors;

    public class FontSizeByHeightBehavior : Behavior<TextBlock>
    {
        private Grid _parentGrid;

        public static readonly DependencyProperty MaximumFontSizeProperty = DependencyProperty.Register("MaximumFontSize", typeof(double), typeof(FontSizeByHeightBehavior), new PropertyMetadata(26.0));

        public double MaximumFontSize
        {
            get => (double)GetValue(MaximumFontSizeProperty);
            set => SetValue(MaximumFontSizeProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            Subscribe();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            UnSubscribe();
        }

        private void Subscribe()
        {
            if (AssociatedObject?.Parent is Grid grid)
            {
                _parentGrid = grid;
                var pd = DependencyPropertyDescriptor.FromProperty(FrameworkElement.HeightProperty, typeof(FrameworkElement));
                pd.AddValueChanged(_parentGrid, AssociatedObjectOnSizeChanged);
                pd = DependencyPropertyDescriptor.FromProperty(FrameworkElement.ActualHeightProperty, typeof(FrameworkElement));
                pd.AddValueChanged(_parentGrid, AssociatedObjectOnSizeChanged);
                UpdateFontSize();
            }
        }

        private void AssociatedObjectOnSizeChanged(object sender, EventArgs e)
        {
            UpdateFontSize();
        }

        private void UpdateFontSize()
        {
            if (AssociatedObject.ActualHeight == 0 && _parentGrid.ActualHeight == 0)
            {
                return;
            }

            double desiredFontSize = Math.Min(Math.Floor(_parentGrid.ActualHeight / AssociatedObject.FontFamily.LineSpacing), MaximumFontSize);
            AssociatedObject.FontSize = desiredFontSize > 0 ? desiredFontSize : 1;
        }

        private void UnSubscribe()
        {
            if (AssociatedObject == null || _parentGrid == null)
            {
                return;
            }

            _parentGrid.SizeChanged -= AssociatedObjectOnSizeChanged;
        }
    }
}