namespace SecondMonitor.Rating.Application.Championship.ViewModels.Creation.Session.SessionLength
{
    using System;
    using DataModel.BasicProperties;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Settings;
    using SecondMonitor.ViewModels.Settings.ViewModel;

    public class DistanceLengthDefinitionViewModel : AbstractViewModel, ISessionLengthDefinitionViewModel
    {
        public static string DistanceLengthKind => "Distance";
        private readonly DisplaySettingsViewModel _displaySettingsViewModel;
        private int _length;

        public DistanceLengthDefinitionViewModel(ISettingsProvider settingsProvider)
        {
            _displaySettingsViewModel = settingsProvider.DisplaySettingsViewModel;
            Length = 100;
            UnitsFormatted = Distance.GetUnitsSymbol(settingsProvider.DisplaySettingsViewModel.DistanceUnits);
        }

        public string UnitsFormatted { get; }

        public string LengthKind => DistanceLengthKind;

        public int Length
        {
            get => _length;
            set => SetProperty(ref _length, value);
        }

        public Distance GetDistance()
        {
            return Distance.CreateByUnits(Length, _displaySettingsViewModel.DistanceUnits);
        }

        public string GetTextualDescription(double layoutLength)
        {
            if (layoutLength == 0)
            {
                return $"{Length}{UnitsFormatted}";
            }

            Distance requiredDistance = Distance.CreateByUnits(Length, _displaySettingsViewModel.DistanceUnits);
            double distanceInMeters = requiredDistance.InMeters;
            int lapsRequired = (int)Math.Round(distanceInMeters / layoutLength);
            return $"{lapsRequired} Laps / {requiredDistance.GetByUnit(_displaySettingsViewModel.DistanceUnits)}{Distance.GetUnitsSymbol(_displaySettingsViewModel.DistanceUnits)}";
        }
    }
}