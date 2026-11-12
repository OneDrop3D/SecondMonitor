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

    public class DriverResultGridViewModel : AbstractViewModel<(DriverDto Driver, IEnumerable<EventDto> EventDtos)>
    {
        private readonly IViewModelFactory _viewModelFactory;
        private string _name;
        private string _position;
        private int _totalPoints;
        private bool _isPlayer;

        public DriverResultGridViewModel(IViewModelFactory viewModelFactory)
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

        public ObservableCollection<DriverSessionResultViewModel> EventResults { get; } = new ObservableCollection<DriverSessionResultViewModel>();

        protected override void ApplyModel((DriverDto Driver, IEnumerable<EventDto> EventDtos) model)
        {
            DriverDto driver = model.Driver;
            IEnumerable<EventDto> events = model.EventDtos;
            Name = driver.LastUsedName.Replace(", ", "\n");
            Position = driver.Position + ".";
            TotalPoints = driver.TotalPoints;
            IsPlayer = driver.IsPlayer;

            EventResults.Clear();
            IEnumerable<DriverSessionResultViewModel> sessionResults = events.SelectMany(x => x.Sessions).Select(x => CreateResultViewModel(driver, x.SessionResult));
            sessionResults.ForEach(EventResults.Add);
        }

        public override (DriverDto Driver, IEnumerable<EventDto> EventDtos) SaveToNewModel()
        {
            return OriginalModel;
        }

        private DriverSessionResultViewModel CreateResultViewModel(DriverDto driver, SessionResultDto sessionResultDto)
        {
            DriverSessionResultViewModel sessionResultViewModel = _viewModelFactory.Create<DriverSessionResultViewModel>();
            DriverSessionResultDto driverResult = sessionResultDto?.DriverSessionResult?.FirstOrDefault(x => x.DriverGuid == driver.GlobalKey);
            if (sessionResultDto != null && driverResult != null)
            {
                sessionResultViewModel.FromModel(driverResult);
            }

            return sessionResultViewModel;
        }
    }
}
