namespace SecondMonitor.WindowsControls.WPF.DataGrid
{
    using System.Windows;
    using System.Windows.Controls;

    public class TextCellItem : Grid
    {
        public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register("FontSize", typeof(int), typeof(TextCellItem), new PropertyMetadata(default(int)));
        public static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register("FontWeight", typeof(FontWeight), typeof(TextCellItem), new PropertyMetadata(default(FontWeight)));
        public static readonly DependencyProperty FontStyleProperty = DependencyProperty.Register("FontStyle", typeof(FontStyle), typeof(TextCellItem), new PropertyMetadata(default(FontStyle)));

        public FontStyle FontStyle
        {
            get => (FontStyle)GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        public FontWeight FontWeight
        {
            get => (FontWeight)GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        public int FontSize
        {
            get => (int)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }
    }
}