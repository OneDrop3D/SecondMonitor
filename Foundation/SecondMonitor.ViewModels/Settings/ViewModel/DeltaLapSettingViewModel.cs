namespace SecondMonitor.ViewModels.Settings.ViewModel
{
    using System.Collections.Generic;
    using System.Linq;

    using SecondMonitor.Contracts;
    using SecondMonitor.ViewModels.Settings.Model;

    public class DeltaLapSettingViewModel : AbstractViewModel<DeltaLapSetting>
    {
        private HumanReadableItem<LapReferenceKind> _practiceTopLapReferenceKind;
        private HumanReadableItem<LapReferenceKind> _practiceBottomLapReferenceKind;
        private HumanReadableItem<LapReferenceKind> _qualificationTopLapReferenceKind;
        private HumanReadableItem<LapReferenceKind> _qualificationBottomLapReferenceKind;
        private HumanReadableItem<LapReferenceKind> _raceTopLapReferenceKind;
        private HumanReadableItem<LapReferenceKind> _raceBottomLapReferenceKind;

        public DeltaLapSettingViewModel()
        {
            AllReferenceKindValues = new LapReferenceKindMap().GetAllItems().ToList();
        }

        public List<HumanReadableItem<LapReferenceKind>> AllReferenceKindValues { get; }

        public HumanReadableItem<LapReferenceKind> PracticeTopLapReferenceKind
        {
            get => _practiceTopLapReferenceKind;
            set => SetProperty(ref _practiceTopLapReferenceKind, value);
        }

        public HumanReadableItem<LapReferenceKind> PracticeBottomLapReferenceKind
        {
            get => _practiceBottomLapReferenceKind;
            set => SetProperty(ref _practiceBottomLapReferenceKind, value);
        }

        public HumanReadableItem<LapReferenceKind> QualificationTopLapReferenceKind
        {
            get => _qualificationTopLapReferenceKind;
            set => SetProperty(ref _qualificationTopLapReferenceKind, value);
        }

        public HumanReadableItem<LapReferenceKind> QualificationBottomLapReferenceKind
        {
            get => _qualificationBottomLapReferenceKind;
            set => SetProperty(ref _qualificationBottomLapReferenceKind, value);
        }

        public HumanReadableItem<LapReferenceKind> RaceTopLapReferenceKind
        {
            get => _raceTopLapReferenceKind;
            set => SetProperty(ref _raceTopLapReferenceKind, value);
        }

        public HumanReadableItem<LapReferenceKind> RaceBottomLapReferenceKind
        {
            get => _raceBottomLapReferenceKind;
            set => SetProperty(ref _raceBottomLapReferenceKind, value);
        }

        protected override void ApplyModel(DeltaLapSetting model)
        {
            PracticeTopLapReferenceKind = AllReferenceKindValues.Single(x => x.Value == model.PracticeTopLapReferenceKind);
            PracticeBottomLapReferenceKind = AllReferenceKindValues.Single(x => x.Value == model.PracticeBottomLapReferenceKind);

            QualificationTopLapReferenceKind = AllReferenceKindValues.Single(x => x.Value == model.QualificationTopLapReferenceKind);
            QualificationBottomLapReferenceKind = AllReferenceKindValues.Single(x => x.Value == model.QualificationBottomLapReferenceKind);

            RaceTopLapReferenceKind = AllReferenceKindValues.Single(x => x.Value == model.RaceTopLapReferenceKind);
            RaceBottomLapReferenceKind = AllReferenceKindValues.Single(x => x.Value == model.RaceBottomLapReferenceKind);
        }

        public override DeltaLapSetting SaveToNewModel()
        {
            return new DeltaLapSetting()
            {
                PracticeTopLapReferenceKind = PracticeTopLapReferenceKind.Value,
                PracticeBottomLapReferenceKind = PracticeBottomLapReferenceKind.Value,

                QualificationTopLapReferenceKind = QualificationTopLapReferenceKind.Value,
                QualificationBottomLapReferenceKind = QualificationBottomLapReferenceKind.Value,

                RaceTopLapReferenceKind = RaceTopLapReferenceKind.Value,
                RaceBottomLapReferenceKind = RaceBottomLapReferenceKind.Value,
            };
        }
    }
}
