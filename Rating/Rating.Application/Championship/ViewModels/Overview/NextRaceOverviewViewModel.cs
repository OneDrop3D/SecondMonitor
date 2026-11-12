namespace SecondMonitor.Rating.Application.Championship.ViewModels.Overview
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;
    using Common.DataModel.Championship;
    using DataModel.Snapshot;
    using Filters;
    using SecondMonitor.ViewModels;

    public class NextRaceOverviewViewModel : AbstractViewModel<ChampionshipDto>
    {
        private readonly List<IChampionshipCondition> _championshipConditions;
        private bool _isDnfButtonVisible;
        private List<string> _textualRequirements;
        private ICommand _dnfSessionCommand;
        private ICommand _redFlagSessionCommand;

        public NextRaceOverviewViewModel(IEnumerable<IChampionshipCondition> championshipMatches)
        {
            _championshipConditions = championshipMatches.ToList();
        }

        public List<string> TextualRequirements
        {
            get => _textualRequirements;
            set => SetProperty(ref _textualRequirements, value);
        }

        public bool IsDnfButtonVisible
        {
            get => _isDnfButtonVisible;
            set => SetProperty(ref _isDnfButtonVisible, value);
        }

        public ICommand DnfSessionCommand
        {
            get => _dnfSessionCommand;
            set => SetProperty(ref _dnfSessionCommand, value);
        }

        public ICommand RedFlagSessionCommand
        {
            get => _redFlagSessionCommand;
            set => SetProperty(ref _redFlagSessionCommand, value);
        }

        public void Evaluate(SimulatorDataSet dataSet)
        {
            if (OriginalModel == null)
            {
                return;
            }

            if (OriginalModel?.ChampionshipState == ChampionshipState.Finished)
            {
                TextualRequirements = new List<string>();
                return;
            }

            List<string> newRequirements = new List<string>();
            newRequirements.AddRange(_championshipConditions.Select(x => x.GetDescription(OriginalModel)));
            bool isFirst = true;
            foreach (IChampionshipCondition championshipCondition in _championshipConditions)
            {
                var evaluationResult = championshipCondition.Evaluate(OriginalModel, dataSet);
                if (!string.IsNullOrWhiteSpace(evaluationResult.Notes))
                {
                    if (isFirst)
                    {
                        newRequirements.Add("\nCurrent Issues:");
                        isFirst = false;
                    }

                    newRequirements.AddRange(evaluationResult.Notes.Split('\n'));
                    newRequirements.Add(string.Empty);
                }
            }

            TextualRequirements = newRequirements;
        }

        protected override void ApplyModel(ChampionshipDto model)
        {
            if (model == null)
            {
                TextualRequirements = new List<string>();
                IsDnfButtonVisible = false;
                return;
            }

            IsDnfButtonVisible = model.ChampionshipState == ChampionshipState.LastSessionCanceled;

            if (OriginalModel.ChampionshipState == ChampionshipState.Finished)
            {
                TextualRequirements = new List<string>();
                return;
            }

            TextualRequirements = _championshipConditions.Select(x => x.GetDescription(model)).ToList();
        }

        public override ChampionshipDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}