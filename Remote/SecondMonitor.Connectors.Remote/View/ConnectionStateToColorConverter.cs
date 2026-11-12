namespace SecondMonitor.Connectors.Remote.View
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;
    using ViewModels;

    public class ConnectionStateToColorConverter : IValueConverter
    {
        public Color ConnectedColor
        {
            get;
            set;
        }

        public Color DisconnectedColor
        {
            get;
            set;
        }

        public Color ConnectingColor
        {
            get;
            set;
        }

        public Color DisconnectingColor
        {
            get;
            set;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ConnectionState connectionState)
            {
                switch (connectionState)
                {
                    case ConnectionState.Connected:
                        return ConnectedColor;
                    case ConnectionState.Disconnected:
                        return DisconnectedColor;
                    case ConnectionState.Connecting:
                        return ConnectingColor;
                    case ConnectionState.Disconnecting:
                        return DisconnectingColor;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}