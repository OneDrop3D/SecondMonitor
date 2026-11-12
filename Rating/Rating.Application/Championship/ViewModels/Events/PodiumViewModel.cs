namespace SecondMonitor.Rating.Application.Championship.ViewModels.Events
{
    using System;
    using System.Linq;
    using Common.DataModel.Championship.Events;
    using SecondMonitor.ViewModels;

    public class PodiumViewModel : AbstractViewModel<SessionResultDto>
    {
        public string First { get; private set; }
        public string Second { get; private set; }
        public string Third { get; private set; }

        protected override void ApplyModel(SessionResultDto result)
        {
            if (result == null)
            {
                return;
            }

            First = result.DriverSessionResult.First(x => x.FinishPosition == 1).DriverName.Replace(", ", "\n");
            Second = result.DriverSessionResult.First(x => x.FinishPosition == 2).DriverName.Replace(", ", "\n");
            Third = result.DriverSessionResult.First(x => x.FinishPosition == 3).DriverName.Replace(", ", "\n");
        }

        public override SessionResultDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}