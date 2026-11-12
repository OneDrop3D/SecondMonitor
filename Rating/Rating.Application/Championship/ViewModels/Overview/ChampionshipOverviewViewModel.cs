namespace SecondMonitor.Rating.Application.Championship.ViewModels.Overview
{
    using System;
    using System.Text;
    using Common.DataModel.Championship;
    using Common.DataModel.Championship.Events;
    using Contracts.Rating;
    using SecondMonitor.ViewModels;

    public class ChampionshipOverviewViewModel : AbstractViewModel<ChampionshipDto>
    {
        private readonly ISimulatorRatingProvider _simulatorRatingProvider;

        public ChampionshipOverviewViewModel(ISimulatorRatingProvider simulatorRatingProvider)
        {
            _simulatorRatingProvider = simulatorRatingProvider;
        }

        public ChampionshipState ChampionshipState { get; set; }

        public string Name { get; private set; }

        public string Progress { get; private set; }

        public string Remark { get; private set; }

        public string Position { get; private set; }

        public string NextTrack { get; set; }

        public string Simulator { get; set; }

        public string ClassName { get; set; }

        public int Difficulty { get; set; }

        public bool IsDifficultyVisible { get; set; }

        protected override void ApplyModel(ChampionshipDto model)
        {
            Name = model.ChampionshipName;
            StringBuilder progressBuilder = new(model.ChampionshipState == ChampionshipState.Finished ? "Completed" : "Next Race: " + model.CompletedRaces + "/" + model.TotalEvents);
            progressBuilder.AppendLine();
            string nextTrack = model.NextTrack;
            var currentEvent = model.GetCurrentOrLastEvent();
            if (currentEvent.IsMysteryTrack)
            {
                nextTrack += " (Mystery Track)";
            }

            if (!string.IsNullOrWhiteSpace(currentEvent.EventName))
            {
                progressBuilder.AppendLine(currentEvent.EventName);
            }

            if (currentEvent.IsEventDateDefined)
            {
                progressBuilder.Append("At: " + currentEvent.EventDate.ToShortDateString());
                progressBuilder.AppendLine(", " + nextTrack);
            }
            else
            {
                progressBuilder.AppendLine("At: " + nextTrack);
            }

            Position = model.Position == 0 ? "-" : "Pos: " + model.Position + "/" + model.TotalDrivers;
            NextTrack = nextTrack;
            Simulator = model.SimulatorName;
            ChampionshipState = model.ChampionshipState;
            ClassName = model.ClassName;
            IsDifficultyVisible = _simulatorRatingProvider.TryGetSuggestedDifficulty(model.SimulatorName, model.ClassName, out int difficulty);
            Difficulty = difficulty;
            if (difficulty != 0)
            {
                progressBuilder.AppendLine("Difficulty: " + difficulty);
            }

            if (currentEvent.EventStatus == EventStatus.InProgress && model.ChampionshipState == ChampionshipState.LastSessionCanceled)
            {
                SessionDto sessionDto = model.GetLastSessionWithResults().SessionDto;
                SessionResultDto resultDto = sessionDto.SessionResult;
                Remark = $"{resultDto.CompletionPercentage * 100:N0}%, {resultDto.Remarks}";
            }

            Progress = progressBuilder.ToString().TrimEnd(Environment.NewLine.ToCharArray());
        }

        public override ChampionshipDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}