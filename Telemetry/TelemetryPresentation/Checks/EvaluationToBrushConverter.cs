namespace SecondMonitor.TelemetryPresentation.Checks
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;
    using Telemetry.TelemetryApplication.Checks.Evaluators;

    public class EvaluationToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush EvaluationNoneBrush;
        private static readonly SolidColorBrush EvaluationInfoBrush;
        private static readonly SolidColorBrush EvaluationOkBrush;
        private static readonly SolidColorBrush EvaluationWarningBrush;
        private static readonly SolidColorBrush EvaluationErrorBrush;

        /*<SolidColorBrush x:Key="EvaluationNoneBrush" Brush="{StaticResource LightGrey01Color}" />
    <SolidColorBrush x:Key="EvaluationInfoBrush" Brush="{StaticResource LightBlue01Color}" />
    <SolidColorBrush x:Key="EvaluationOkBrush" Brush="{StaticResource Green01Color}" />
    <SolidColorBrush x:Key="EvaluationWarningBrush" Brush="{StaticResource Orange01Color}" />
    <SolidColorBrush x:Key="EvaluationErrorBrush" Brush="{StaticResource LightRed02Color}" />*/

        static EvaluationToBrushConverter()
        {
            EvaluationNoneBrush = (SolidColorBrush)Application.Current.FindResource("EvaluationNoneBrush");
            EvaluationInfoBrush = (SolidColorBrush)Application.Current.FindResource("EvaluationInfoBrush");
            EvaluationOkBrush = (SolidColorBrush)Application.Current.FindResource("EvaluationOkBrush");
            EvaluationWarningBrush = (SolidColorBrush)Application.Current.FindResource("EvaluationWarningBrush");
            EvaluationErrorBrush = (SolidColorBrush)Application.Current.FindResource("EvaluationErrorBrush");
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is EvaluationResultKind evaluationResult))
            {
                return EvaluationNoneBrush;
            }

            switch (evaluationResult)
            {
                case EvaluationResultKind.Ok:
                    return EvaluationOkBrush;
                case EvaluationResultKind.Information:
                    return EvaluationInfoBrush;
                case EvaluationResultKind.Warning:
                    return EvaluationWarningBrush;
                case EvaluationResultKind.Error:
                    return EvaluationErrorBrush;
                case EvaluationResultKind.None:
                default:
                    return EvaluationNoneBrush;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}