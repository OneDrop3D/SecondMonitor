namespace SecondMonitor.Rating.Application.Championship.ViewModels.Overview.ResultGrid
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.Rating.Common.DataModel.Championship.Events;
    using SecondMonitor.ViewModels;
    using SecondMonitor.ViewModels.DataGrid;
    using SecondMonitor.ViewModels.Factory;
    using SecondMonitor.ViewModels.Settings.Model.Layout;

    public class DriversResultGridViewModel : AbstractViewModel<ChampionshipDto>
    {
        private readonly IViewModelFactory _viewModelFactory;
        private string _header;

        public DriversResultGridViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
        }

        public string Header
        {
            get => _header;
            set => SetProperty(ref _header, value);
        }

        public ObservableCollection<DriverResultGridViewModel> DriverResults { get; } = new ObservableCollection<DriverResultGridViewModel>();

        public ObservableCollection<ColumnDescriptorViewModel> ColumnDescriptors { get; } = new ObservableCollection<ColumnDescriptorViewModel>();

        protected override void ApplyModel(ChampionshipDto model)
        {
            DriverResults.Clear();
            foreach (DriverDto driverDto in model.Drivers.Where(x => !x.IsInactive).OrderBy(x => x.Position))
            {
                DriverResultGridViewModel driverResults = _viewModelFactory.Create<DriverResultGridViewModel>();
                driverResults.FromModel((driverDto, model.Events));
                DriverResults.Add(driverResults);
            }

            ColumnDescriptors.Clear();
            CreateColumnDescriptors(model).Select(x => _viewModelFactory.CreateAndApply<ColumnDescriptorViewModel, ColumnDescriptor>(x)).ForEach(x => ColumnDescriptors.Add(x));
        }

        private IEnumerable<ColumnDescriptor> CreateColumnDescriptors(ChampionshipDto model)
        {
            yield return new TextColumnDescriptor()
            {
                BindingPropertyName = nameof(DriverResultGridViewModel.Position),
                Title = string.Empty,
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 50)
            };

            yield return new TextColumnDescriptor()
            {
                BindingPropertyName = nameof(DriverResultGridViewModel.Name),
                Title = "Driver",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Automatic, 100)
            };

            int eventIndex = 1;
            foreach (EventDto modelEvent in model.Events)
            {
                bool hasMultipleSession = modelEvent.Sessions.Count > 1;
                int sessionIndex = 1;

                foreach (SessionDto sessionDto in modelEvent.Sessions)
                {
                    yield return new BindableDataColumnDescriptor()
                    {
                        BindingPropertyName = $"{nameof(DriverResultGridViewModel.EventResults)}[{eventIndex - 1}]",
                        Title = hasMultipleSession ? $"{eventIndex}-{sessionIndex}" : $"{eventIndex}",
                        HorizontalAlignment = HorizontalAlignmentKind.Center,
                        ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 80),
                    };

                    sessionIndex++;
                }

                eventIndex++;
            }

            yield return new TextColumnDescriptor()
            {
                BindingPropertyName = nameof(DriverResultGridViewModel.TotalPoints),
                Title = "Pts",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 100),
                CustomElementStyle = "DriverPointsCellStyle"
            };

            yield break;
        }

        public override ChampionshipDto SaveToNewModel()
        {
            return OriginalModel;
        }
    }
}
