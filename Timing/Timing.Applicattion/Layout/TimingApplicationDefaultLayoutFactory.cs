namespace SecondMonitor.Timing.Application.Layout
{
    using SessionTiming.Drivers.ViewModel;
    using TimingGrid;
    using ViewModel;
    using ViewModels.CarStatus;
    using ViewModels.CarStatus.FuelStatus;
    using ViewModels.Layouts;
    using ViewModels.Layouts.Factory;
    using ViewModels.Settings.Model.Layout;
    using ViewModels.Track.MapView;
    using ViewModels.TrackInfo;

    public class TimingApplicationDefaultLayoutFactory : IDefaultLayoutFactory
    {
        public LayoutDescription CreateDefaultLayout()
        {
            GenericContentSetting carStatusTopSettings = new ColumnsDefinitionSetting()
            {
                ColumnsCount = 2,
                ColumnsContent = new GenericContentSetting[]
                {
                    new NamedContentSetting()
                    {
                        ContentName = CarWheelsViewModel.ViewModelLayoutName,
                    },
                    new NamedContentSetting()
                    {
                        ContentName = FuelOverviewViewModel.ViewModelLayoutName,
                    }
                },

                ColumnsSize = new[]
                {
                    new LengthDefinitionSetting(SizeKind.Remaining, 100),
                    new LengthDefinitionSetting(SizeKind.Automatic, 100),
                }
            };

            GenericContentSetting carSystemsSettings = new RowsDefinitionSetting()
            {
                RowCount = 2,
                RowsContent = new GenericContentSetting[]
                {
                    new NamedContentSetting()
                    {
                        ContentName = CarSystemsViewModel.ViewModelLayoutName,
                    },
                    new NamedContentSetting()
                    {
                        ContentName = DashboardViewModel.ViewModelLayoutName,
                    }
                },

                RowsSize = new[]
                {
                    new LengthDefinitionSetting(SizeKind.Automatic, 100),
                    new LengthDefinitionSetting(SizeKind.Remaining, 100),
                }
            };

            GenericContentSetting carStatusBottomSetting = new ColumnsDefinitionSetting()
            {
                ColumnsCount = 2,
                ColumnsContent = new GenericContentSetting[]
                {
                    new NamedContentSetting()
                    {
                        ContentName = PedalsAndGearViewModel.ViewModelLayoutName,
                    },
                    carSystemsSettings
                },

                ColumnsSize = new[]
                {
                    new LengthDefinitionSetting(SizeKind.Remaining, 100),
                    new LengthDefinitionSetting(SizeKind.Automatic, 100),
                }
            };

            GenericContentSetting carStatusSetting = new RowsDefinitionSetting()
            {
                RowCount = 2,
                RowsContent = new GenericContentSetting[]
                {
                   carStatusTopSettings,
                   carStatusBottomSetting,
                },

                RowsSize = new[]
                {
                    new LengthDefinitionSetting(SizeKind.Automatic, 100),
                    new LengthDefinitionSetting(SizeKind.Remaining, 100),
                }
            };

            GenericContentSetting topViewSettings = new ColumnsDefinitionSetting()
            {
                ColumnsCount = 4,
                ColumnsContent = new GenericContentSetting[]
                {
                    new NamedContentSetting()
                    {
                        ContentName = TrackInfoViewModel.ViewModelLayoutName,
                    },
                    new NamedContentSetting()
                    {
                        ContentName = LapDeltaViewModel.ViewModelLayoutName,
                    },
                    new NamedContentSetting()
                    {
                        ContentName = ViewModels.TrackRecords.TrackRecordsViewModel.ViewModelLayoutName,
                    },
                    new NamedContentSetting()
                    {
                        ContentName = SessionInfoViewModel.ViewModelLayoutName,
                    },
                },

                ColumnsSize = new[]
                {
                    new LengthDefinitionSetting(SizeKind.Automatic, 0),
                    new LengthDefinitionSetting(SizeKind.Remaining, 100),
                    new LengthDefinitionSetting(SizeKind.Automatic, 0),
                    new LengthDefinitionSetting(SizeKind.Automatic, 0),
                },
                IsExpanderEnabled = false,
                IsExpanderExpanded = true,
                ExpandDirection = ExpandDirection.Down
            };

            GenericContentSetting bottomViewSettings = new ColumnsDefinitionSetting()
            {
                ColumnsCount = 4,
                ColumnsContent = new GenericContentSetting[]
                {
                    carStatusSetting,
                    new NamedContentSetting()
                    {
                        ContentName = DefaultSituationOverviewViewModel.ViewModelLayoutName,
                        CustomHeight = 350,
                        IsCustomHeight = true,
                    },
                    new NamedContentSetting()
                    {
                        ContentName = Rating.Application.Rating.ViewModels.RatingApplicationViewModel.ViewModelLayoutName,
                    },
                    new NamedContentSetting()
                    {
                        ContentName = SidebarViewModel.ViewModelLayoutName,
                    },
                },

                ColumnsSize = new[]
                {
                    new LengthDefinitionSetting(SizeKind.Automatic, 0),
                    new LengthDefinitionSetting(SizeKind.Remaining, 100),
                    new LengthDefinitionSetting(SizeKind.Automatic, 0),
                    new LengthDefinitionSetting(SizeKind.Manual, 50),
                },
                IsExpanderEnabled = false,
                IsExpanderExpanded = true,
                ExpandDirection = ExpandDirection.Up
            };

            GenericContentSetting mainViewSettings = new RowsDefinitionSetting()
            {
                RowCount = 3,
                RowsContent = new GenericContentSetting[]
                {
                    topViewSettings,
                    new NamedContentSetting()
                    {
                        ContentName = TimingDataGridViewModel.ViewModelLayoutName,
                    },
                    bottomViewSettings,
                },

                RowsSize = new[]
                {
                    new LengthDefinitionSetting(SizeKind.Automatic, 100),
                    new LengthDefinitionSetting(SizeKind.Remaining, 100),
                    new LengthDefinitionSetting(SizeKind.Automatic, 100),
                }
            };

            return new LayoutDescription()
            {
                Name = "Default Layout",
                RootElement = mainViewSettings,
            };
        }
    }
}