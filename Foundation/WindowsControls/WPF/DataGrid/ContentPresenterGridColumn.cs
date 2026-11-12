namespace SecondMonitor.WindowsControls.WPF.DataGrid
{
    using System.Windows;
    using System.Windows.Controls;

    public class ContentPresenterGridColumn : DataGridBoundColumn
    {
        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            ContentPresenter content = new ContentPresenter();
            content.SetBinding(ContentControl.ContentProperty, Binding);
            return content;
        }

        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            return GenerateElement(cell, dataItem);
        }
    }
}
