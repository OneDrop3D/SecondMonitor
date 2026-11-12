namespace SecondMonitor.Rating.Application.Championship.ViewModels.Overview.ModifyResults
{
    using System;

    using SecondMonitor.Rating.Common.DataModel.Championship.Events;
    using SecondMonitor.ViewModels;

    public class DriverResultViewModel : AbstractViewModel<DriverSessionResultDto>
    {
        private bool _isCarNameVisible;
        private string _driverName;
        private string _carName;
        private int _finishPosition;
        private int _pointsGain;
        private bool _isPlayer;
        private bool _wasFastestLap;
        private bool _isTeamNameVisible;
        private string _teamName;
        private int _classFinishPosition;
        private string _classId;
        private string _className;
        public string DriverName
        {
            get => _driverName;
            private set => SetProperty(ref _driverName, value);
        }

        public string CarName
        {
            get => _carName;
            private set => SetProperty(ref _carName, value);
        }

        public int FinishPosition
        {
            get => _finishPosition;
            set => SetProperty(ref _finishPosition, value);
        }

        public int ClassFinishPosition
        {
            get => _classFinishPosition;
            set => SetProperty(ref _classFinishPosition, value);
        }

        public int PointsGain
        {
            get => _pointsGain;
            set => SetProperty(ref _pointsGain, value);
        }

        public bool IsPlayer
        {
            get => _isPlayer;
            private set => SetProperty(ref _isPlayer, value);
        }

        public bool IsCarNameVisible
        {
            get => _isCarNameVisible;
            set => SetProperty(ref _isCarNameVisible, value);
        }

        public bool WasFastestLap
        {
            get => _wasFastestLap;
            private set => SetProperty(ref _wasFastestLap, value);
        }

        public bool IsTeamNameVisible
        {
            get => _isTeamNameVisible;
            set => SetProperty(ref _isTeamNameVisible, value);
        }

        public string TeamName
        {
            get => _teamName;
            set => SetProperty(ref _teamName, value);
        }

        public string ClassId
        {
            get => _classId;
            set => SetProperty(ref _classId, value);
        }

        public string ClassName
        {
            get => _className;
            set => SetProperty(ref _className, value);
        }

        protected override void ApplyModel(DriverSessionResultDto model)
        {
            DriverName = model.DriverName;
            CarName = model.CarName;
            FinishPosition = model.FinishPosition;
            ClassFinishPosition = model.ClassFinishPosition;
            PointsGain = model.PointsGain;
            IsPlayer = model.IsPlayer;
            WasFastestLap = model.WasFastestLap;
            TeamName = model.TeamName;
            ClassId = model.ClassId;
            ClassName = model.ClassName;
        }

        public override DriverSessionResultDto SaveToNewModel()
        {
            throw new NotSupportedException();
        }

        public AmendedDriverResult CreateAmendedDriverResult()
        {
            return new AmendedDriverResult(FinishPosition, ClassFinishPosition,  OriginalModel);
        }
    }
}
