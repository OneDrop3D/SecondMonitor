namespace SecondMonitor.ViewModels.TrackRecords
{
    using System;
    using DataModel.TrackRecords;

    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.ViewModels.Settings;

    public class CarRecordSummaryViewModel : AbstractViewModel<NamedRecordSet>
    {
        private readonly VelocityUnits _velocityUnits;
        public CarRecordSummaryViewModel(ISettingsProvider settingsProvider)
        {
            _velocityUnits = settingsProvider.DisplaySettingsViewModel.VelocityUnits;
        }

        public string CarName { get; set; }

        public TimeSpan BestOverall { get; private set; }

        public TimeSpan BestPractice { get; private set; }

        public TimeSpan BestQualification { get; private set; }

        public TimeSpan BestRace { get; private set; }

        public string BestOverallSpeed { get; private set; }

        public string BestPracticeSpeed { get; private set; }
        public string BestQualificationSpeed { get; private set; }
        public string BestRaceSpeed { get; private set; }

        protected override void ApplyModel(NamedRecordSet model)
        {
            CarName = model.Name;
            BestOverall = model.GetOverAllBest().LapTime;
            BestPractice = model.BestPracticeRecord?.LapTime ?? TimeSpan.Zero;
            BestQualification = model.BestQualiRecord?.LapTime ?? TimeSpan.Zero;
            BestRace = model.BestRaceRecord?.LapTime ?? TimeSpan.Zero;

            BestOverallSpeed = model.GetOverAllBest().AverageSpeedInKph > 0 ? $"{Velocity.FromKph(model.GetOverAllBest().AverageSpeedInKph).GetValueInUnits(_velocityUnits):F2}{Velocity.GetUnitSymbol(_velocityUnits)}" : "-";
            BestPracticeSpeed = model.BestPracticeRecord?.AverageSpeedInKph > 0 ? $"{Velocity.FromKph(model.BestPracticeRecord.AverageSpeedInKph).GetValueInUnits(_velocityUnits):F2}{Velocity.GetUnitSymbol(_velocityUnits)}" : "-";
            BestQualificationSpeed = model.BestQualiRecord?.AverageSpeedInKph > 0 ? $"{Velocity.FromKph(model.BestQualiRecord.AverageSpeedInKph).GetValueInUnits(_velocityUnits):F2}{Velocity.GetUnitSymbol(_velocityUnits)}" : "-";
            BestRaceSpeed = model.BestRaceRecord?.AverageSpeedInKph > 0 ? $"{Velocity.FromKph(model.BestRaceRecord.AverageSpeedInKph).GetValueInUnits(_velocityUnits):F2}{Velocity.GetUnitSymbol(_velocityUnits)}" : "-";
        }

        public override NamedRecordSet SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}