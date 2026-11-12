namespace SecondMonitor.WindowsControls.WPF.DataGrid
{
    using System.Windows;
    using System.Windows.Controls;

    public class TemplateTextColumn : DataGridTemplateColumn
    {
        private readonly int _fontSize;
        private readonly FontWeight _fontWeight;
        private readonly FontStyle _fontStyle;

        public TemplateTextColumn(int fontSize, FontWeight fontWeight, FontStyle fontStyle)
        {
            _fontSize = fontSize;
            _fontWeight = fontWeight;
            _fontStyle = fontStyle;
        }

        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            var generatedElement = base.GenerateElement(cell, dataItem);
            if (generatedElement == null)
            {
                return null;
            }

            TextCellItem grid = new TextCellItem() { FontSize = _fontSize, FontWeight = _fontWeight, FontStyle = _fontStyle };
            grid.Children.Add(generatedElement);
            return grid;
        }
    }
}