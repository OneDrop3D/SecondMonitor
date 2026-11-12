namespace SecondMonitor.Rating.Application.Championship.ViewModels.Events
{
    using System;

    using Common.DataModel.Championship.Events;
    using SecondMonitor.ViewModels;

    public class ManufacturerFinishViewModel : AbstractViewModel<ManufacturerSessionResultDto>
    {
        public string ManufacturerName { get; private set; }
        public int FinishPosition { get; private set; }
        public int PointsGain { get; private set; }
        public bool IsPlayer { get; private set; }

        protected override void ApplyModel(ManufacturerSessionResultDto model)
        {
            ManufacturerName = model.ManufacturerName;
            FinishPosition = model.FinishPosition;
            PointsGain = model.PointsGain;
            IsPlayer = model.IsPlayer;
        }

        public override ManufacturerSessionResultDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}