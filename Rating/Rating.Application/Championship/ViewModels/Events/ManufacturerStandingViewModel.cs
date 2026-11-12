namespace SecondMonitor.Rating.Application.Championship.ViewModels.Events
{
    using Common.DataModel.Championship;
    using SecondMonitor.ViewModels;

    public class ManufacturerStandingViewModel : AbstractViewModel<ManufacturerDto>
    {
        public string ManufacturerName { get; private set; }
        public int TotalPoints { get; set; }

        public bool IsFirst => Position == 1;
        public int Position { get; private set; }
        public bool IsPlayer { get; private set; }
        public int GapToPrevious { get; set; }
        public int GapToLeader { get; set; }

        protected override void ApplyModel(ManufacturerDto model)
        {
            ManufacturerName = model.ManufacturerName;
            TotalPoints = model.TotalPoints;
            Position = model.Position;
            IsPlayer = model.IsPlayer;
        }

        public override ManufacturerDto SaveToNewModel()
        {
            throw new System.NotSupportedException();
        }
    }
}