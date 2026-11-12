namespace SecondMonitor.ViewModels.PitBoard
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DataModel.BasicProperties;
    using DataModel.Snapshot.Drivers;
    using Factory;
    using Settings;

    using DistanceQuantity = DataModel.BasicProperties.Distance;

    public class ClearToRejoinBoardViewModel : AbstractViewModel<IReadOnlyList<IDriverInfo>>
    {
        private const int NumberOfOtherDrivers = 5;
        private readonly ISettingsProvider _settingsProvider;
        private string _driverName;
        private string _distance;
        private bool _isClearToRejoin;

        public ClearToRejoinBoardViewModel(ISettingsProvider settingsProvider, IViewModelFactory viewModelFactory)
        {
            _settingsProvider = settingsProvider;
            DriverName = "Fedor Predkozisko";
            Distance = "200 m";
            IsClearToRejoin = false;
            DriverDistances = Enumerable.Range(0, NumberOfOtherDrivers).Select(x => viewModelFactory.Create<DriverDistanceViewModel>())
                .ToList();
        }

        public bool IsClearToRejoin
        {
            get => _isClearToRejoin;
            set => SetProperty(ref _isClearToRejoin, value);
        }

        public string DriverName
        {
            get => _driverName;
            set => SetProperty(ref _driverName, value);
        }

        public string Distance
        {
            get => _distance;
            set => SetProperty(ref _distance, value);
        }

        public List<DriverDistanceViewModel> DriverDistances { get; }

        protected override void ApplyModel(IReadOnlyList<IDriverInfo> model)
        {
            if (model.Count == 0)
            {
                DriverDistances.ForEach(x => x.IsVisible = false);
                IsClearToRejoin = true;
                DriverName = string.Empty;
                Distance = string.Empty;
                return;
            }

            DistanceUnits distanceUnit = _settingsProvider.DisplaySettingsViewModel.DistanceUnitsSmall;

            IDriverInfo firstDriverAffected = model[0];
            IsClearToRejoin = false;
            DriverName = firstDriverAffected.DriverShortName;
            Distance = DistanceQuantity.FromMeters(-firstDriverAffected.DistanceToPlayer)
                           .GetByUnit(distanceUnit).ToString("N0") +
                       DistanceQuantity.GetUnitsSymbol(_settingsProvider.DisplaySettingsViewModel.DistanceUnitsSmall);

            List<IDriverInfo> otherDrivers = model.Skip(1).Take(NumberOfOtherDrivers).ToList();
            for (int i = 0; i < NumberOfOtherDrivers; i++)
            {
                DriverDistanceViewModel currentDistanceViewModel = DriverDistances[i];
                if (i >= otherDrivers.Count)
                {
                    currentDistanceViewModel.IsVisible = false;
                    continue;
                }

                currentDistanceViewModel.Distance = DistanceQuantity.FromMeters(-otherDrivers[i].DistanceToPlayer)
                    .GetByUnit(distanceUnit).ToString("N0");
                currentDistanceViewModel.DriverName = otherDrivers[i].DriverShortName;
                currentDistanceViewModel.IsVisible = true;
            }
        }

        public override IReadOnlyList<IDriverInfo> SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}