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

    public class DriversClassResultGridViewModel : AbstractViewModel<(ChampionshipDto championshipDto, ClassIdentifier classIdentifier)>
    {
        private readonly IViewModelFactory _viewModelFactory;
        private string _header;

        public DriversClassResultGridViewModel(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
        }

        public string Header
        {
            get => _header;
            set => SetProperty(ref _header, value);
        }

        public ObservableCollection<DriverClassResultGridViewModel> DriverResults { get; } = new ObservableCollection<DriverClassResultGridViewModel>();

        public ObservableCollection<ColumnDescriptorViewModel> ColumnDescriptors { get; } = new ObservableCollection<ColumnDescriptorViewModel>();

        protected override void ApplyModel((ChampionshipDto championshipDto, ClassIdentifier classIdentifier) model)
        {
            ChampionshipDto championship = model.championshipDto;
            ClassIdentifier classIdentifier = model.classIdentifier;
            ClassChampionshipDto classChampionshipDto = championship.GetOrCreateClassChampionship(classIdentifier);

            DriverResults.Clear();
            foreach (ClassDriverDto driverDto in classChampionshipDto.Drivers.OrderBy(x => x.Position))
            {
                DriverClassResultGridViewModel driverResults = _viewModelFactory.Create<DriverClassResultGridViewModel>();
                driverResults.FromModel((driverDto, classIdentifier, championship.Events));
                DriverResults.Add(driverResults);
            }

            ColumnDescriptors.Clear();
            CreateColumnDescriptors(championship).Select(x => _viewModelFactory.CreateAndApply<ColumnDescriptorViewModel, ColumnDescriptor>(x)).ForEach(x => ColumnDescriptors.Add(x));
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
                        BindingPropertyName = $"{nameof(DriverClassResultGridViewModel.EventResults)}[{eventIndex - 1}]",
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
                BindingPropertyName = nameof(DriverClassResultGridViewModel.TotalPoints),
                Title = "Pts",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 100),
                CustomElementStyle = "DriverPointsCellStyle"
            };

            yield break;
        }

        public override (ChampionshipDto championshipDto, ClassIdentifier classIdentifier) SaveToNewModel()
        {
            return OriginalModel;
        }
    }
}
