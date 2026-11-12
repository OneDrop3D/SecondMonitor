namespace SecondMonitor.ViewModels.DataGrid
{
    using System.Collections.Generic;
    using System.Linq;
    using DataModel.BasicProperties;
    using Settings;
    using Settings.Model.Layout;

    public class TimingGridColumnsFactory : IDefaultColumnsFactory
    {
        private readonly ISettingsProvider _settingsProvider;

        public TimingGridColumnsFactory(ISettingsProvider settingsProvider)
        {
            _settingsProvider = settingsProvider;
        }

        public IEnumerable<ColumnDescriptor> CreateColumns(SessionType sessionType)
        {
            switch (sessionType)
            {
                case SessionType.Practice:
                    return _settingsProvider.DisplaySettingsViewModel.SessionOptionsViewModel.PracticeSessionDisplayOptionsView.ColumnsSettingsViewModel.ColumnDescriptors;
                case SessionType.Qualification:
                    return _settingsProvider.DisplaySettingsViewModel.SessionOptionsViewModel.QualificationSessionDisplayOptionsView.ColumnsSettingsViewModel.ColumnDescriptors;
                case SessionType.Race:
                    return _settingsProvider.DisplaySettingsViewModel.SessionOptionsViewModel.RaceSessionDisplayOptionsView.ColumnsSettingsViewModel.ColumnDescriptors;
                default:
                    return Enumerable.Empty<ColumnDescriptor>();
            }
        }

        private IEnumerable<ColumnDescriptor> CreateAllColumns()
        {
            yield return new TemplatedColumnDescriptor()
            {
                Title = string.Empty,
                Name = "Class Ribbon",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Automatic, 0),
                CanResize = true,
                CellTemplateName = "ClassIndication",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            };

            yield return new TemplateTextColumnDescriptor()
            {
                Title = "#",
                Name = "Position",
                BindingPropertyName = "PositionInClass",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 64),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
                CellTemplateName = "PositionColumnTemplate"
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Name",
                Name = "Driver Name",
                BindingPropertyName = "DriverShortName",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 172),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Car",
                Name = "Car",
                BindingPropertyName = "CarName",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 250),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
            };

            yield return new TemplatedColumnDescriptor()
            {
                Title = string.Empty,
                Name = "Class Ribbon",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Automatic, 0),
                CanResize = true,
                CellTemplateName = "ClassIndication",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Class",
                Name = "Class",
                BindingPropertyName = "CarClassName",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 234),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Laps",
                Name = "Laps",
                BindingPropertyName = "CompletedLaps",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 60),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Last Lap",
                Name = "Last Lap Time",
                BindingPropertyName = "LastLapTime",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 150),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleLastLap",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "S1",
                Name = "S1 Time",
                BindingPropertyName = "Sector1",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 80),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleSector1",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "S2",
                Name = "S2 Time",
                BindingPropertyName = "Sector2",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 80),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleSector2",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "S3",
                Name = "S3 Time",
                BindingPropertyName = "Sector3",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 80),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleSector3",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Pace",
                Name = "Pace",
                BindingPropertyName = "Pace",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 150),
                CanResize = true,
                CustomElementStyle = "TextBoxStylePace",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Best Lap",
                Name = "Best Lap Time",
                BindingPropertyName = "BestLap",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 150),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Current Lap",
                Name = "Current Lap Time",
                BindingPropertyName = "CurrentLapProgressTime",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 150),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
            };

            yield return new TemplateTextColumnDescriptor()
            {
                Title = string.Empty,
                Name = "Tyre Kind Ribbon",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 20),
                CanResize = true,
                IsBold = true,
                CellTemplateName = "TyreKindRibbon",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            };

            yield return new TemplateTextColumnDescriptor()
            {
                Title = "VE",
                Name = "Virtual Energy",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 40),
                CanResize = true,
                IsBold = true,
                CellTemplateName = "VirtualEnergyTemplate",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            };

            yield return new TemplateTextColumnDescriptor()
            {
                Title = "Pits",
                Name = "Pit Information",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 180),
                CanResize = true,
                CellTemplateName = "PitStopInformation",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Gap",
                Name = "Gap",
                BindingPropertyName = "GapToColumnText",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 150),
                CanResize = true,
                IsBold = true,
                CustomElementStyle = "TextBoxStyleBestLap",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Top Speed",
                Name = "Top Speed",
                BindingPropertyName = "TopSpeed",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 100),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Rating",
                Name = "Rating",
                BindingPropertyName = "RatingFormatted",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 100),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleRating",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Rep.",
                Name = "Reputation",
                BindingPropertyName = "Reputation",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 40),
                CanResize = true,
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Points",
                Name = "Points",
                BindingPropertyName = "ChampionshipPoints",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 100),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            };

            yield return new TextColumnDescriptor()
            {
                Title = "P2P",
                Name = "Push to Pass Uses",
                BindingPropertyName = "PushToPass",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 50),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleLastLap",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            };
        }

        private IEnumerable<ColumnDescriptor> CreatePracticeColumns()
        {
            yield return new TemplatedColumnDescriptor()
            {
                Title = string.Empty,
                Name = "Class Ribbon",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Automatic, 0),
                CanResize = true,
                CellTemplateName = "ClassIndication",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            };

            yield return new TemplateTextColumnDescriptor()
            {
                Title = "#",
                Name = "Position",
                BindingPropertyName = "PositionInClass",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 64),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
                CellTemplateName = "PositionColumnTemplate"
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Name",
                Name = "Driver Name",
                BindingPropertyName = "DriverShortName",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 172),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Car",
                Name = "Car",
                BindingPropertyName = "CarName",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 250),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
            };

            yield return new TemplatedColumnDescriptor()
            {
                Title = string.Empty,
                Name = "Class Ribbon",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Automatic, 0),
                CanResize = true,
                CellTemplateName = "ClassIndication",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Class",
                Name = "Class",
                BindingPropertyName = "CarClassName",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 234),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Laps",
                Name = "Laps",
                BindingPropertyName = "CompletedLaps",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 60),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Last Lap",
                Name = "Last Lap Time",
                BindingPropertyName = "LastLapTime",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 150),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleLastLap",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "S1",
                Name = "S1 Time",
                BindingPropertyName = "Sector1",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 80),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleSector1",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "S2",
                Name = "S2 Time",
                BindingPropertyName = "Sector2",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 80),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleSector2",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "S3",
                Name = "S3 Time",
                BindingPropertyName = "Sector3",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 80),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleSector3",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Best Lap",
                Name = "Best Lap Time",
                BindingPropertyName = "BestLap",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 150),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Predicted",
                Name = "Predicted Lap Time",
                BindingPropertyName = "LapTimeDeltaViewModel.PredictedLapTime",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 150),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleLapDeltaCombined",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Δ",
                Name = "Δ to Best Laps",
                BindingPropertyName = "LapTimeDeltaViewModel.DeltaToCombined",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 80),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleLapDeltaCombined",
            };

            yield return new TemplateTextColumnDescriptor()
            {
                Title = string.Empty,
                Name = "Tyre Kind Ribbon",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 20),
                CanResize = true,
                IsBold = true,
                CellTemplateName = "TyreKindRibbon",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            };
            
            yield return new TemplateTextColumnDescriptor()
            {
                Title = "VE",
                Name = "Virtual Energy",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 40),
                CanResize = true,
                IsBold = true,
                CellTemplateName = "VirtualEnergyTemplate",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Gap",
                Name = "Gap",
                BindingPropertyName = "GapToColumnText",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 150),
                CanResize = true,
                IsBold = true,
                CustomElementStyle = "TextBoxStyleBestLap",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Top Speed",
                Name = "Top Speed",
                BindingPropertyName = "TopSpeed",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 100),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Rating",
                Name = "Rating",
                BindingPropertyName = "RatingFormatted",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 100),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleRating",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Rep.",
                Name = "Reputation",
                BindingPropertyName = "Reputation",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 40),
                CanResize = true,
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            };

            yield return new TextColumnDescriptor()
            {
                Title = "P2P",
                Name = "Push to Pass Uses",
                BindingPropertyName = "PushToPass",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 50),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleLastLap",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            };
        }

        private IEnumerable<ColumnDescriptor> CreateQualificationColumns() => CreatePracticeColumns();

        public IEnumerable<ColumnDescriptor> GetColumnDescriptors(SessionType sessionType)
        {
            switch (sessionType)
            {
                case SessionType.Practice:
                    return CreatePracticeColumns();
                case SessionType.Qualification:
                    return CreateQualificationColumns();
                case SessionType.Race:
                    return CreateRaceColumns();
                case SessionType.WarmUp:
                case SessionType.Na:
                default:
                    return CreateAllColumns();
            }
        }

        private static IEnumerable<ColumnDescriptor> CreateRaceColumns()
        {
            yield return new TemplatedColumnDescriptor()
            {
                Title = string.Empty,
                Name = "Class Ribbon",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Automatic, 0),
                CanResize = true,
                CellTemplateName = "ClassIndication",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            };

            yield return new TemplateTextColumnDescriptor()
            {
                Title = "#",
                Name = "Position",
                BindingPropertyName = "PositionInClass",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 64),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
                CellTemplateName = "PositionColumnTemplate"
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Name",
                Name = "Driver Name",
                BindingPropertyName = "DriverShortName",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 172),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Car",
                Name = "Car",
                BindingPropertyName = "CarName",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 250),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Laps",
                Name = "Laps",
                BindingPropertyName = "CompletedLaps",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 60),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Last Lap",
                Name = "Last Lap Time",
                BindingPropertyName = "LastLapTime",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 150),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleLastLap",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "S1",
                Name = "S1 Time, color rel. player",
                BindingPropertyName = "Sector1",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 80),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleSector1PlayerRelative",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "S2",
                Name = "S2 Time, color rel. player",
                BindingPropertyName = "Sector2",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 80),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleSector2PlayerRelative",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "S3",
                Name = "S3 Time, color rel. player",
                BindingPropertyName = "Sector3",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 80),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleSector3PlayerRelative",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Pace",
                Name = "Pace",
                BindingPropertyName = "Pace",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 150),
                CanResize = true,
                CustomElementStyle = "TextBoxStylePace",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Predicted",
                Name = "Predicted Lap Time",
                BindingPropertyName = "LapTimeDeltaViewModel.PredictedLapTime",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 150),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleLapDeltaCombined",
            };

            yield return new TemplateTextColumnDescriptor()
            {
                Title = string.Empty,
                Name = "Tyre Kind Ribbon",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 20),
                CanResize = true,
                IsBold = true,
                CellTemplateName = "TyreKindRibbon",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            };

            yield return new TemplateTextColumnDescriptor()
            {
                Title = "VE",
                Name = "Virtual Energy",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 40),
                CanResize = true,
                IsBold = true,
                CellTemplateName = "VirtualEnergyTemplate",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            };

            yield return new TemplateTextColumnDescriptor()
            {
                Title = "Pits",
                Name = "Pit Information",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 180),
                CanResize = true,
                CellTemplateName = "PitStopInformation",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Gap",
                Name = "Gap",
                BindingPropertyName = "GapToColumnText",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 150),
                CanResize = true,
                IsBold = true,
                CustomElementStyle = "TextBoxStyleBestLap",
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Rating",
                Name = "Rating",
                BindingPropertyName = "RatingFormatted",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 100),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleRating",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Rep.",
                Name = "Reputation",
                BindingPropertyName = "Reputation",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 40),
                CanResize = true,
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            };

            yield return new TextColumnDescriptor()
            {
                Title = "Points",
                Name = "Points",
                BindingPropertyName = "ChampionshipPoints",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 100),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            };

            yield return new TextColumnDescriptor()
            {
                Title = "P2P",
                Name = "Push to Pass Uses",
                BindingPropertyName = "PushToPass",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 50),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleLastLap",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            };
        }
    }
}