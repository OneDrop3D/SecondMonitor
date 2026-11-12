namespace SecondMonitor.ViewModels.Settings.Model
{
    public class PitEstimationSettings
    {
        public PitEstimationSettings()
        {
            AlwaysEnabled = new PitEstimationVisualizationSettings();
            PitRequested = new PitEstimationVisualizationSettings(true, true);
            InPitWindow = new PitEstimationVisualizationSettings(true, true);
            FuelLow = new PitEstimationVisualizationSettings(true, true);
            LapsRemaining = 5;
            TyreWorn = new PitEstimationVisualizationSettings(true, true);
            StintEnding = new PitEstimationVisualizationSettings(true, true);
            TyreWear = 50;
            ExtraTime = 3;
            IsOverridePitStallTimeEnabled = false;
            OverridePitStallTime = 0;
            IsOverridePitLaneTimeEnabled = false;
            OverridePitLaneTime = 0;
            ShowPitBoardAtEntry = true;
        }

        public PitEstimationVisualizationSettings AlwaysEnabled { get; set; }

        public PitEstimationVisualizationSettings PitRequested { get; set; }

        public PitEstimationVisualizationSettings InPitWindow { get; set; }

        public PitEstimationVisualizationSettings FuelLow { get; set; }

        public int LapsRemaining { get; set; }

        public PitEstimationVisualizationSettings TyreWorn { get; set; }

        public int TyreWear { get; set; }

        public double ExtraTime { get; set; }

        public bool IsOverridePitStallTimeEnabled { get; set; }

        public double OverridePitStallTime { get; set; }

        public bool IsOverridePitLaneTimeEnabled { get; set; }

        public double OverridePitLaneTime { get; set; }

        public bool ShowPitBoardAtEntry { get; set; }
        public PitEstimationVisualizationSettings StintEnding { get; set; }
    }
}
