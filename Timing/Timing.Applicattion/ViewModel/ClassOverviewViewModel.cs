namespace SecondMonitor.Timing.Application.ViewModel
{
    using System;
    using System.Collections.Generic;

    using SecondMonitor.DataModel.BasicProperties;
    using SecondMonitor.DataModel.Snapshot.Drivers;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Colors;

    public class ClassOverviewViewModel : AbstractViewModel<IList<DriverInfo>>
    {
        private readonly IClassColorProvider _classColorProvider;
        private string _classId;
        private ColorDto _classColor;
        private string _previousClassName;
        private int _previousCount;
        private string _label;

        public ClassOverviewViewModel(IClassColorProvider classColorProvider)
        {
            _previousClassName = string.Empty;
            Label = string.Empty;
            _classColorProvider = classColorProvider;
        }

        public string ClassId
        {
            get => _classId;
            set => SetProperty(ref _classId, value, (_, newClassId) => ClassColor = _classColorProvider.GetColorForClass(newClassId));
        }

        public string Label
        {
            get => _label;
            set => SetProperty(ref _label, value);
        }

        public ColorDto ClassColor
        {
            get => _classColor;
            set => SetProperty(ref _classColor, value);
        }

        protected override void ApplyModel(IList<DriverInfo> model)
        {
            ClassId = model[0].CarClassId;
            if (model[0].CarClassName == _previousClassName && model.Count == _previousCount)
            {
                return;
            }

            _previousClassName = model[0].CarClassName;
            _previousCount = model.Count;
            Label = $"{_previousClassName} ({_previousCount})";
        }

        public override IList<DriverInfo> SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}
