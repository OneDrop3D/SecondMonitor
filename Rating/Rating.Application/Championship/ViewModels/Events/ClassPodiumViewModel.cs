namespace SecondMonitor.Rating.Application.Championship.ViewModels.Events
{
    using System;
    using System.Linq;
    using Common.DataModel.Championship.Events;
    using SecondMonitor.ViewModels;

    public class ClassPodiumViewModel : AbstractViewModel<ClassSessionResultDto>
    {
        public string First { get; private set; }
        public string Second { get; private set; }
        public string Third { get; private set; }

        public string ClassName { get; private set; }

        protected override void ApplyModel(ClassSessionResultDto result)
        {
            if (result == null)
            {
                return;
            }

            ClassName = result.ClassName;

            First = result.ClassSessionResult.FirstOrDefault(x => x.FinishPosition == 1)?.DriverName.Replace(", ", "\n") ?? string.Empty;
            Second = result.ClassSessionResult.FirstOrDefault(x => x.FinishPosition == 2)?.DriverName.Replace(", ", "\n") ?? string.Empty;
            Third = result.ClassSessionResult.FirstOrDefault(x => x.FinishPosition == 3)?.DriverName.Replace(", ", "\n") ?? string.Empty;
        }

        public override ClassSessionResultDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}