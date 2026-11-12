namespace SecondMonitor.ViewModels.FuelConsumption.FuelStrategy
{
    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.ViewModels.CarStatus.FuelStatus;
    using SecondMonitor.ViewModels.Settings.ViewModel;
    using SecondMonitor.ViewModels.Text;

    public interface IPitStopsViewModel : IViewModel
    {
        DisplaySettingsViewModel DisplaySettingsViewModel { get; }

        FontStyle NextPitStopFontStyle { get; set; }

        bool IsNextPitStopInfoVisible { get; set; }

        string NextPitStopInfo { get; set; }

        RefuelingWindowState NextPitStopState { get; set; }

        bool IsFuelToAddVisible { get; set; }

        bool IsLastStopFuelToAddVisible { get; set; }

        string FuelForNextPitStopLabel { get; set; }

        string FuelToAddLastStopLabel { get; set; }

        Volume FuelForNextPitStop { get; set; }

        Volume FuelToAddLastStop { get; set; }

        bool IsPitStopCountVisible { get; set; }

        int PitStopCount { get; set; }

        RefuelingWindowState RefuelingWindowState { get; set; }

        void Reset();

        Volume GetFuelToAddNow(IFuelOverviewViewModel fuelOverviewViewModel);
    }
}
