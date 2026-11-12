namespace SecondMonitor.WindowsControls.WPF.Behaviors
{
    using System;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media.Animation;
    using Microsoft.Xaml.Behaviors;

    public class OpacityByParentMouseOverBehavior : Behavior<FrameworkElement>
    {
        private FrameworkElement _parentElement;

        protected override void OnAttached()
        {
            base.OnAttached();
            _parentElement = (FrameworkElement)LogicalTreeHelper.GetParent(AssociatedObject);
            Subscribe();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (_parentElement == null)
            {
                return;
            }

            _parentElement.MouseEnter -= ParentElementOnMouseEnter;
            _parentElement.MouseLeave -= ParentElementOnMouseLeave;
        }

        private void ParentElementOnMouseLeave(object sender, MouseEventArgs e)
        {
            DoubleAnimation showAnimation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.4));
            AssociatedObject.BeginAnimation(UIElement.OpacityProperty, showAnimation);
        }

        private void ParentElementOnMouseEnter(object sender, MouseEventArgs e)
        {
            DoubleAnimation showAnimation = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.4));
            AssociatedObject.BeginAnimation(UIElement.OpacityProperty, showAnimation);
        }

        private void Subscribe()
        {
            if (_parentElement == null)
            {
                return;
            }

            _parentElement.MouseEnter += ParentElementOnMouseEnter;
            _parentElement.MouseLeave += ParentElementOnMouseLeave;
        }
    }
}