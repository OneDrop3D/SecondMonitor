namespace SecondMonitor.ViewModels.Settings.Model
{
    using SecondMonitor.DataModel.BasicProperties;

    public class ChampionshipSettings
    {
        public bool IsOutlineLeaderEnabled { get; set; } = true;

        public ColorDto LeaderOutLine { get; set; } = ColorDto.GreenColor;

        public bool IsOutLineDriverMorePointsEnabled { get; set; } = true;

        public int MaxOutlinedDriversMorePoints { get; set; } = 5;

        public bool IsOutLineDriveLessPointsEnabled { get; set; } = true;

        public int MaxOutlinedDriveLessPoints { get; set; } = 5;

        public int MaxPointsDifferenceToOutline { get; set; } = 40;

        public ColorDto DriverMorePointsOutlineFrom { get; set; } = ColorDto.Orange01Color;

        public ColorDto DriverMorePointsOutlineTo { get; set; } = ColorDto.Orange02Color;

        public ColorDto DriverLessPointsOutlineFrom { get; set; } = ColorDto.Blue01Color;

        public ColorDto DriverLessPointsOutlineTo { get; set; } = ColorDto.DarkBlueColor;

        public bool IsMultiplayerEnabled { get; set; } = false;
        
        public ChampionshipOptions LastChampionshipOptions { get; set; } = new ChampionshipOptions();
    }
}
