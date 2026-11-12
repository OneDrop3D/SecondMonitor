namespace SecondMonitor.WindowsControls.WPF.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;

    using SecondMonitor.DataModel.BasicProperties;

    public class AutoContrastConverter : IValueConverter
    {
        public SolidColorBrush Brush1 { get; set; }

        public SolidColorBrush Brush2 { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ColorDto colorDto)
            {
                return Brush1;
            }

            double defaultBrushLuminance = GetLuminance(Brush1.Color.ScR, Brush1.Color.ScG, Brush1.Color.ScB);
            double lowContrastBrushLuminance = GetLuminance(Brush2.Color.ScR, Brush2.Color.ScG, Brush2.Color.ScB);
            double backGroundColorLuminance = GetLuminance(colorDto.RedSc, colorDto.GreenSc, colorDto.BlueSc);

            /*double contrastRatio1 = Math.Abs(backGroundColorLuminance - defaultBrushLuminance);
            double contrastRatio2 = Math.Abs(backGroundColorLuminance - lowContrastBrushLuminance);*/

            double contrastRatio1 = defaultBrushLuminance > backGroundColorLuminance ?
                (defaultBrushLuminance + 0.05) / (backGroundColorLuminance + 0.05) :
                (backGroundColorLuminance + 0.05) / (defaultBrushLuminance + 0.05);

            double contrastRatio2 = lowContrastBrushLuminance > backGroundColorLuminance ?
                (lowContrastBrushLuminance + 0.05) / (backGroundColorLuminance + 0.05) :
                (backGroundColorLuminance + 0.05) / (lowContrastBrushLuminance + 0.05);

            return contrastRatio1 < 3.5 ? Brush2 : Brush1;

            /*return contrastRatio1 > contrastRatio2 ? Brush1 : Brush2;*/
        }

        private static double GetLuminance(double rScaled, double gScaled, double bScaled)
        {
            /*double r = rScaled <= 0.03928 ? rScaled / 12.92 : Math.Pow((rScaled + 0.055) / 1.055, 2.4);
            double g = gScaled <= 0.03928 ? gScaled / 12.92 : Math.Pow((gScaled + 0.055) / 1.055, 2.4);
            double b = bScaled <= 0.03928 ? bScaled / 12.92 : Math.Pow((bScaled + 0.055) / 1.055, 2.4);*/

            return 0.2126 * rScaled + 0.7152 * gScaled + 0.0722 * bScaled;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
