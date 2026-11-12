namespace SecondMonitor.WindowsControls.WPF.DataGrid
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    public class BlankColumn : DataGridColumn
    {
        private readonly Brush _backgroundBrush;

        public BlankColumn(Brush backgroundBrush)
        {
            _backgroundBrush = backgroundBrush;
        }

        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            Grid backgroundGrid = new Grid()
            {
                Background = _backgroundBrush,
                Margin = new Thickness(0, -2, 0, -2),
            };
            return backgroundGrid;
        }

        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem) => GenerateElement(cell, dataItem);
    }
}