namespace SecondMonitor.ViewModels.CarStatus.FuelStatus
{
    using System;
    using System.Windows.Input;

    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.ViewModels.FuelConsumption.FuelStrategy;
    using SecondMonitor.ViewModels.Settings.ViewModel;

    public interface IFuelOverviewViewModel : IViewModel
    {
        DisplaySettingsViewModel DisplaySettingsViewModel { get; }

        bool IsWetSession { get; }

        bool IsFuelCalculatorButtonEnabled { get; }

        ICommand ResetCommand { get; }

        ICommand ShowFuelCalculatorCommand { get; }

        ICommand HideFuelCalculatorCommand { get; }

        bool IsVisible { get; set; }

        TimeSpan TimeDelta { get; }

        double LapsDelta { get; }

        Volume FuelDelta { get; }

        TimeSpan TimeLeft { get; }

        double LapsLeft { get; }

        string AvgPerLap { get; }

        string AvgPerMinute { get; }

        string CurrentPerLap { get; }

        string CurrentPerMinute { get; }

        double FuelPercentage { get; }

        FuelLevelStatus FuelState { get; }

        Volume MaximumFuel { get; }
        
        FuelPlannerViewModel FuelPlannerViewModel { get; }

        FuelStrategiesViewModel FuelStrategiesViewModel { get; }

        IPitStopsViewModel PitStopsViewModel { get; set; }

        string FuelRemainingLabel { get; }
        
        int FuelMultiplier { get; set; }
    }
}
