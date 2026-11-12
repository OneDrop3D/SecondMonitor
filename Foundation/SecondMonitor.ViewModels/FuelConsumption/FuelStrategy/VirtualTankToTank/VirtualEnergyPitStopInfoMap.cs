namespace SecondMonitor.ViewModels.FuelConsumption.FuelStrategy.VirtualTankToTank
{
    using SecondMonitor.Contracts;
    using SecondMonitor.DataModel.Summary.FuelStrategy.VirtualEnergyStrategy;

    public class VirtualEnergyPitStopInfoMap : AbstractHumanReadableMap<VirtualEnergyPitStopInfo>
    {
        public VirtualEnergyPitStopInfoMap()
        {
            Translations.Add(VirtualEnergyPitStopInfo.TotalFuelEndOfLap,  "Total Fuel, Current Lap");
            Translations.Add(VirtualEnergyPitStopInfo.TotalFuelPitLap,  "Total Fuel, Pit Lap");
            Translations.Add(VirtualEnergyPitStopInfo.FuelToAdd,  "Fuel to Add during Pit Stop");
        }
    }
}
