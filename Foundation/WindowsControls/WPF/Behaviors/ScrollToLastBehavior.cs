namespace SecondMonitor.WindowsControls.WPF.Behaviors
{
    using System;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Windows.Controls;
    using Microsoft.Xaml.Behaviors;

    public class ScrollToLastBehavior : Behavior<DataGrid>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            Subscribe();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (AssociatedObject == null)
            {
                return;
            }

            DependencyPropertyDescriptor pd = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ItemsControl));
            pd.AddValueChanged(AssociatedObject, Handler);
            SubscribeToItemsSource();
        }

        private void SubscribeToItemsSource()
        {
            if (AssociatedObject.Items is INotifyCollectionChanged observableCollection)
            {
                observableCollection.CollectionChanged += ObservableCollectionOnCollectionChanged;
            }
        }

        private void ObservableCollectionOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (AssociatedObject.Items.Count > 0)
            {
                AssociatedObject.ScrollIntoView(AssociatedObject.Items[AssociatedObject.Items.Count - 1]);
            }
        }

        private void Unsubscribe()
        {
            if (AssociatedObject == null)
            {
                return;
            }

            DependencyPropertyDescriptor pd = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ItemsControl));
            pd.RemoveValueChanged(AssociatedObject, Handler);
            if (AssociatedObject.Items is INotifyCollectionChanged observableCollection)
            {
                observableCollection.CollectionChanged -= ObservableCollectionOnCollectionChanged;
            }
        }

        private void Handler(object sender, EventArgs e)
        {
            SubscribeToItemsSource();
        }
    }
}