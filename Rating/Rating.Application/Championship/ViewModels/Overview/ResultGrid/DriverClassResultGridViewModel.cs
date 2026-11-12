namespace SecondMonitor.Rating.Application.Championship.ViewModels.Overview.ResultGrid
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.Rating.Common.DataModel.Championship.Events;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;

    public class DriverClassResultGridViewModel : AbstractViewModel<(ClassDriverDto Driver, ClassIdentifier classIdentifier, IEnumerable<EventDto> EventDtos)>
    {
        private readonly IViewModelFactory _viewModelFactory;
        private string _name;
        private string _position;
        private int _totalPoints;
        private bool _isPlayer;

        public DriverClassResultGridViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Position
        {
            get => _position;
            set => SetProperty(ref _position, value);
        }

        public int TotalPoints
        {
            get => _totalPoints;
            set => SetProperty(ref _totalPoints, value);
        }

        public bool IsPlayer
        {
            get => _isPlayer;
            set => SetProperty(ref _isPlayer, value);
        }

        public ObservableCollection<DriverClassSessionResultViewModel> EventResults { get; } = new ObservableCollection<DriverClassSessionResultViewModel>();

        protected override void ApplyModel((ClassDriverDto Driver, ClassIdentifier classIdentifier, IEnumerable<EventDto> EventDtos) model)
        {
            ClassDriverDto driver = model.Driver;
            ClassIdentifier classIdentifier = model.classIdentifier;
            IEnumerable<EventDto> events = model.EventDtos;
            Name = driver.DriverName.Replace(", ", "\n");
            Position = driver.Position + ".";
            TotalPoints = driver.TotalPoints;
            IsPlayer = driver.IsPlayer;

            EventResults.Clear();
            IEnumerable<DriverClassSessionResultViewModel> sessionResults = events.SelectMany(x => x.Sessions).Select(x => CreateResultViewModel(driver, classIdentifier, x.SessionResult));
            sessionResults.ForEach(EventResults.Add);
        }

        public override (ClassDriverDto Driver, ClassIdentifier classIdentifier, IEnumerable<EventDto> EventDtos) SaveToNewModel()
        {
            return OriginalModel;
        }

        private DriverClassSessionResultViewModel CreateResultViewModel(ClassDriverDto driver, ClassIdentifier classIdentifier, SessionResultDto sessionResultDto)
        {
            DriverClassSessionResultViewModel sessionResultViewModel = _viewModelFactory.Create<DriverClassSessionResultViewModel>();
            ClassDriverResultDto driverResult = sessionResultDto?.GetOrCreateClassSessionResult(classIdentifier)?.ClassSessionResult.FirstOrDefault(x => x.DriverGlobalKey == driver.DriverGlobalKey);
            if (sessionResultDto != null && driverResult != null)
            {
                sessionResultViewModel.FromModel(driverResult);
            }

            return sessionResultViewModel;
        }
    }
}
