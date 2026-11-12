namespace SecondMonitor.ViewModels.DataGrid
{
    using System.Collections.Generic;
    using DataModel.BasicProperties;

    using Settings.Model.Layout;

    public class TimingGridDescriptorsTemplateFactory : IColumnDescriptorTemplatesFactory
    {
        public IEnumerable<IColumnDescriptorTemplate> Create()
        {
            yield return new GenericColumnDescriptorTemplate(() => new TemplatedColumnDescriptor()
            {
                Title = string.Empty,
                Name = "Class Ribbon",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Automatic, 0),
                CanResize = true,
                CellTemplateName = "ClassIndication",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            }, "Class Ribbon");

            yield return new GenericColumnDescriptorTemplate(() => new TemplateTextColumnDescriptor()
            {
                Title = "#",
                Name = "Position",
                BindingPropertyName = "PositionInClass",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 64),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
                CellTemplateName = "PositionColumnTemplate",
            }, "Position");

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "Name",
                Name = "Driver Name",
                BindingPropertyName = "DriverShortName",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 172),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
            }, "Driver Name");

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "Car",
                Name = "Car",
                BindingPropertyName = "CarName",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 250),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
            }, "Car");

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "Car#",
                Name = "Car Race Number",
                BindingPropertyName = "CarRaceNumber",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 80),
                CanResize = true,
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
                CustomElementStyle = "TextBoxStyleBestLap",
            }, ColumnNames.CarRaceNumber);

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "Team Name",
                Name = "Team Name",
                BindingPropertyName = "TeamName",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 172),
                CanResize = true,
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
                CustomElementStyle = "TextBoxStyleBestLap",
            }, ColumnNames.TeamName);

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "Class",
                Name = "Class",
                BindingPropertyName = "CarClassName",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 234),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            }, "Class");

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "Laps",
                Name = "Laps",
                BindingPropertyName = "CompletedLaps",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 60),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
            }, "Laps");

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "Last Lap",
                Name = "Last Lap Time",
                BindingPropertyName = "LastLapTime",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 150),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleLastLap",
            }, "Last Lap Time");

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "S1",
                Name = "S1 Time, color by Best",
                BindingPropertyName = "Sector1",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 80),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleSector1",
            }, "S1 Time, color by Best");

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "S2",
                Name = "S2 Time, color by Best",
                BindingPropertyName = "Sector2",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 80),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleSector2",
            }, "S2 Time, color by Best");

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "S3",
                Name = "S3 Time, color by Best",
                BindingPropertyName = "Sector3",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 80),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleSector3",
            }, "S3 Time, color by Best");

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "Best S1",
                Name = "Best S1 Time, color by Best",
                BindingPropertyName = "BestSector1",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 80),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleSector1",
            }, "Best S1 Time, color by Best");

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "Best S2",
                Name = "Best S2 Time, color by Best",
                BindingPropertyName = "BestSector2",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 80),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleSector2",
            }, "Best S2 Time, color by Best");

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "Best S3",
                Name = "Best S3 Time, color by Best",
                BindingPropertyName = "BestSector3",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 80),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleSector3",
            }, "Best S3 Time, color by Best");

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "Pace",
                Name = "Pace",
                BindingPropertyName = "Pace",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 150),
                CanResize = true,
                CustomElementStyle = "TextBoxStylePace",
            }, "Pace");

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "Best Lap",
                Name = "Best Lap Time",
                BindingPropertyName = "BestLap",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 150),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
            }, "Best Lap Time");

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "Current Lap",
                Name = "Current Lap Time",
                BindingPropertyName = "CurrentLapProgressTime",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 150),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
            }, "Current Lap Time");

            yield return new GenericColumnDescriptorTemplate(() => new TemplateTextColumnDescriptor()
            {
                Title = string.Empty,
                Name = "Tyre Kind Ribbon",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 20),
                CanResize = true,
                IsBold = true,
                CellTemplateName = "TyreKindRibbon",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            }, "Tyre Kind Ribbon");

            yield return new GenericColumnDescriptorTemplate(() => new TemplateTextColumnDescriptor()
            {
                Title = "VE",
                Name = "Virtual Energy",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 40),
                CanResize = true,
                IsBold = true,
                CellTemplateName = "VirtualEnergyTemplate",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            }, "Virtual Energy");

            yield return new GenericColumnDescriptorTemplate(() => new TemplateTextColumnDescriptor()
            {
                Title = "Pits",
                Name = "Pit Information",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 180),
                CanResize = true,
                CellTemplateName = "PitStopInformation",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            }, "Pit Information");

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "Gap",
                Name = "Gap",
                BindingPropertyName = "GapToColumnText",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 150),
                CanResize = true,
                IsBold = true,
                CustomElementStyle = "TextBoxStyleBestLap",
            }, "Gap");

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "Gap To Prev",
                Name = "Gap to Prev",
                BindingPropertyName = "GapToPrevious",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 150),
                CanResize = true,
                IsBold = true,
                CustomElementStyle = "TextBoxStyleBestLap",
            }, "Gap to Prev");

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "Top Speed",
                Name = "Top Speed",
                BindingPropertyName = "TopSpeed",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 100),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
            }, "Top Speed");

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "Rating",
                Name = "Rating",
                BindingPropertyName = "RatingFormatted",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 100),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleRating",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            }, "Rating");

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "Points",
                Name = "Points",
                BindingPropertyName = "ChampionshipPoints",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 100),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleBestLap",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            }, "Points");

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "S1",
                Name = "S1 Time, color rel. player",
                BindingPropertyName = "Sector1",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 80),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleSector1PlayerRelative",
            }, "S1 Time, color rel. player");

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "S2",
                Name = "S2 Time, color rel. player",
                BindingPropertyName = "Sector2",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 80),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleSector2PlayerRelative",
            }, "S2 Time, color rel. player");

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "S3",
                Name = "S3 Time, color rel. player",
                BindingPropertyName = "Sector3",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 80),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleSector3PlayerRelative",
            }, "S3 Time, color rel. player");

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "Δ",
                Name = "Δ to Best Laps",
                BindingPropertyName = "LapTimeDeltaViewModel.DeltaToCombined",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 80),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleLapDeltaCombined",
            }, "Δ to Best Laps");

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "Current Lap",
                Name = "Current Lap Time, Δ colored",
                BindingPropertyName = "CurrentLapProgressTime",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 150),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleLapDeltaCombined",
            }, "Current Lap Time, Δ colored");

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "Predicted",
                Name = "Predicted Lap Time",
                BindingPropertyName = "LapTimeDeltaViewModel.PredictedLapTime",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 150),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleLapDeltaCombined",
            }, "Predicted Lap Time");

            yield return new GenericColumnDescriptorTemplate(() => new BlankColumnDescriptor()
            {
                Title = string.Empty,
                Name = "Blank",
                BackgroundColor = ColorDto.WhiteColor,
                CanResize = true,
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 10),
            }, "Blank");

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "Rep.",
                Name = "Reputation",
                BindingPropertyName = "Reputation",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 40),
                CanResize = true,
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            }, "Reputation");

            yield return new GenericColumnDescriptorTemplate(() => new TextColumnDescriptor()
            {
                Title = "P2P",
                Name = "Push to Pass Uses",
                BindingPropertyName = "PushToPass",
                ColumnLength = new LengthDefinitionSetting(SizeKind.Manual, 50),
                CanResize = true,
                CustomElementStyle = "TextBoxStyleLastLap",
                IsAutoHideCapable = true,
                IsAutoHideEnabled = true,
            }, "Push to Pass Uses");
        }
    }
}