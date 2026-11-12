namespace SecondMonitor.Timing.Application.ViewModel
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.DataModel.Snapshot;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.Factory;

    public class ClassesOverviewViewModel : AbstractViewModel<SimulatorDataSet>
    {
        private readonly IViewModelFactory _viewModelFactory;
        private bool _isClassOverviewShown;

        public ClassesOverviewViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
            ClassesOverview = new ObservableCollection<ClassOverviewViewModel>();
        }

        public bool IsClassOverviewShown
        {
            get => _isClassOverviewShown;
            set => SetProperty(ref _isClassOverviewShown, value);
        }

        public ObservableCollection<ClassOverviewViewModel> ClassesOverview { get; }

        protected override void ApplyModel(SimulatorDataSet model)
        {
            if (!model.SessionInfo.IsMultiClass)
            {
                IsClassOverviewShown = false;
                return;
            }

            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => ApplyModel(model));
                return;
            }

            IsClassOverviewShown = true;
            var driversGrouped = model.DriversInfo.GroupBy(x => x.CarClassId)
                .Select(group => new { Name = group.Key, Drivers = group.OrderBy(x => x.Position).ToList() })
                .OrderBy(x => x.Drivers.First().Position).ToList();

            ClassesOverview.MatchLength(driversGrouped.Count, () => _viewModelFactory.Create<ClassOverviewViewModel>());

            for (int i = 0; i < driversGrouped.Count; i++)
            {
                ClassesOverview[i].FromModel(driversGrouped[i].Drivers);
            }
        }

        public override SimulatorDataSet SaveToNewModel()
        {
            throw new NotSupportedException();
        }
    }
}
