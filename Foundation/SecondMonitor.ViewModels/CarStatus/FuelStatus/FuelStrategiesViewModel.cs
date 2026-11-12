namespace SecondMonitor.ViewModels.CarStatus.FuelStatus
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    using SecondMonitor.Contracts;
    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.ViewModels.FuelConsumption.FuelStrategy;

    public class FuelStrategiesViewModel : AbstractViewModel
    {
        private HumanReadableItem<IFuelStrategy> _selectedStrategy;

        public ObservableCollection<HumanReadableItem<IFuelStrategy>> SelectableStrategies { get; } = new();

        public HumanReadableItem<IFuelStrategy> SelectedStrategy
        {
            get => _selectedStrategy;
            set => SetProperty(ref _selectedStrategy, value);
        }

        public void SetSelectableStrategies(IEnumerable<IFuelStrategy> fuelStrategies)
        {
            string previouslySelected = SelectedStrategy?.HumanReadableValue ?? string.Empty;
            SelectableStrategies.Clear();
            fuelStrategies.ForEach(x => SelectableStrategies.Add(new HumanReadableItem<IFuelStrategy>(x, x.StrategyName)));
            if (!string.IsNullOrEmpty(previouslySelected))
            {
                SelectedStrategy = SelectableStrategies.FirstOrDefault(x => x.HumanReadableValue == previouslySelected);
            }

            SelectedStrategy ??= SelectableStrategies.First();
        }

        public bool TrySelectStrategy(string lastSelectedStrategy)
        {
            HumanReadableItem<IFuelStrategy> strategyToSelect = SelectableStrategies.SingleOrDefault(x => x.HumanReadableValue == lastSelectedStrategy);
            if (strategyToSelect != null)
            {
                SelectedStrategy = strategyToSelect;
            }

            return strategyToSelect != null;
        }
    }
}
