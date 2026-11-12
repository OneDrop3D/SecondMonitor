namespace SecondMonitor.WindowsControls.WPF
{
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Media;

    using SecondMonitor.DataModel.Extensions;

    /// <summary>
    /// The visual helper.
    /// </summary>
    /// Source: https://stackoverflow.com/questions/15641473/how-to-automatically-scale-font-size-for-a-group-of-controls
    public class VisualHelper
    {
        public static List<T> FindVisualChildren<T>(DependencyObject obj) where T : DependencyObject
        {
            List<T> children = new List<T>();
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var o = VisualTreeHelper.GetChild(obj, i);
                if (o is T variable)
                {
                    children.Add(variable);
                }

                children.AddRange(FindVisualChildren<T>(o)); // recursive
            }

            return children;
        }

        public static T FindUpVisualTree<T>(DependencyObject initial) where T : DependencyObject
        {
            DependencyObject current = initial;

            while (current != null && current.GetType() != typeof(T))
            {
                current = VisualTreeHelper.GetParent(current);
            }

            return current as T;
        }

        public static T FindDescendantByName<T>(FrameworkElement initial, string name) where T : FrameworkElement
        {
            Queue<DependencyObject> descendantsToProcess = new Queue<DependencyObject>(GetChildren(initial));

            while (descendantsToProcess.Count > 0)
            {
                var current = descendantsToProcess.Dequeue();
                if (current is T element && element.Name == name)
                {
                    return element;
                }

                GetChildren(current).ForEach(descendantsToProcess.Enqueue);
            }

            return null;
        }

        public static IEnumerable<DependencyObject> GetChildren(DependencyObject initial)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(initial); i++)
            {
                var child = VisualTreeHelper.GetChild(initial, i);
                yield return child;
            }
        }

        public static T FindAncestorByName<T>(FrameworkElement initial, string name) where T : FrameworkElement
        {
            FrameworkElement current = initial;

            while (current != null && current.GetType() != typeof(T) && current.Name != name)
            {
                current = VisualTreeHelper.GetParent(current) as FrameworkElement;
            }

            return current as T;
        }
    }
}