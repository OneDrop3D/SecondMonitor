namespace SecondMonitor.WindowsControls.WPF.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;

    using SecondMonitor.Contracts.PitStatistics;

    public class PitInfoBriefDescriptionKindToBrushConverter : IValueConverter
    {
        public Brush DefaultBrush { get; set; }

        public Brush FasterThanPlayerBrush { get; set; }

        public Brush SlowerThanPlayerBrush { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not PitInfoBriefDescriptionKind kind)
            {
                return DefaultBrush;
            }

            switch (kind)
            {
                case PitInfoBriefDescriptionKind.PitStopFasterThanPlayer:
                    return FasterThanPlayerBrush;
                case PitInfoBriefDescriptionKind.PitStopSlowerThanPlayer:
                    return SlowerThanPlayerBrush;
                case PitInfoBriefDescriptionKind.None:
                case PitInfoBriefDescriptionKind.TyresOlderThanPlayer:
                case PitInfoBriefDescriptionKind.TyresYoungerThanPlayers:
                case PitInfoBriefDescriptionKind.PitEntry:
                case PitInfoBriefDescriptionKind.InPits:
                case PitInfoBriefDescriptionKind.PitExit:
                default:
                    return DefaultBrush;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
