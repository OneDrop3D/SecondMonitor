namespace SecondMonitor.ViewModels.Settings.Model
{
    public class DeltaLapSetting
    {
        public LapReferenceKind PracticeTopLapReferenceKind { get; set; } = LapReferenceKind.PlayerBest;
        public LapReferenceKind PracticeBottomLapReferenceKind { get; set; } = LapReferenceKind.LastLap;

        public LapReferenceKind QualificationTopLapReferenceKind { get; set; } = LapReferenceKind.PlayerBest;
        public LapReferenceKind QualificationBottomLapReferenceKind { get; set; } = LapReferenceKind.LastLap;

        public LapReferenceKind RaceTopLapReferenceKind { get; set; } = LapReferenceKind.PlayerBest;
        public LapReferenceKind RaceBottomLapReferenceKind { get; set; } = LapReferenceKind.LastLap;
    }
}
