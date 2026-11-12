namespace SecondMonitor.Rating.Application.Championship.ViewModels.Creation.Session
{
    using SecondMonitor.ViewModels.Factory;

    public class SessionDefinitionViewModelFactory : ISessionDefinitionViewModelFactory
    {
        private readonly IViewModelFactory _viewModelFactory;

        public SessionDefinitionViewModelFactory(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
        }

        public SessionDefinitionViewModel CreateBase()
        {
            return new SessionDefinitionViewModel(_viewModelFactory)
            {
                PointsForFastestLap = 1,
                MaxPositionForFastestLap = 10,
            };
        }
    }
}