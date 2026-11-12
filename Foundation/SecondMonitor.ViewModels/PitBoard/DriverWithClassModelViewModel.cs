namespace SecondMonitor.ViewModels.PitBoard
{
    using System;

    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.ViewModels.Colors;

    public class DriverWithClassModelViewModel : AbstractViewModel<DriverWithClassModel>
    {
        private readonly IClassColorProvider _classColorProvider;
        private string _driveName;
        private ColorDto _classRibbonColor;
        private bool _isClassInfoShown;
        private int _position;

        public string DriveName
        {
            get => _driveName;
            set => SetProperty(ref _driveName, value, (_, __) => OnNameChanged());
        }

        public ColorDto ClassRibbonColor
        {
            get => _classRibbonColor;
            set => SetProperty(ref _classRibbonColor, value);
        }

        public bool IsClassInfoShown
        {
            get => _isClassInfoShown;
            set => SetProperty(ref _isClassInfoShown, value);
        }

        public int Position
        {
            get => _position;
            set => SetProperty(ref _position, value);
        }

        public DriverWithClassModelViewModel(IClassColorProvider classColorProvider)
        {
            _classColorProvider = classColorProvider;
        }

        protected override void ApplyModel(DriverWithClassModel model)
        {
            Position = model.Position;
            IsClassInfoShown = model.IsClassInfoShown;
            DriveName = model.DriveName;
        }

        public override DriverWithClassModel SaveToNewModel()
        {
            throw new NotSupportedException();
        }

        private void OnNameChanged()
        {
            ClassRibbonColor = _classColorProvider.GetColorForClass(OriginalModel.ClassId);
        }
    }
}
