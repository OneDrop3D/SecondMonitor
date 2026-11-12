namespace SecondMonitor.ViewModels.PitBoard
{
    public class DriverWithClassModel
    {
        public DriverWithClassModel(int position, string driveName, string classId, bool isClassInfoShown)
        {
            DriveName = driveName;
            ClassId = classId;
            IsClassInfoShown = isClassInfoShown;
            Position = position;
        }

        public int Position { get; set; }
        public string DriveName { get; set; }
        public string ClassId { get; set; }
        public bool IsClassInfoShown { get; set; }
    }
}
