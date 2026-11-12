namespace SecondMonitor.Rating.Application.Championship.ViewModels.Overview.ProgressChart
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using OxyPlot;
    using OxyPlot.Axes;
    using OxyPlot.Legends;
    using OxyPlot.Series;

    using SecondMonitor.DataModel.Extensions;
    using SecondMonitor.Rating.Common.DataModel.Championship;
    using SecondMonitor.Rating.Common.DataModel.Championship.Events;
    using SecondMonitor.ViewModels;

    public class ChampionshipClassProgressChartViewModel : AbstractViewModel<(ChampionshipDto championshipDto, ClassIdentifier classIdentifier)>
    {
        private string _header;
        public ChampionshipClassProgressChartViewModel()
        {
            PlotModel = new PlotModel();
        }

        public string Header
        {
            get => _header;
            set => SetProperty(ref _header, value);
        }

        public PlotModel PlotModel { get; private set; }
        protected override void ApplyModel((ChampionshipDto championshipDto, ClassIdentifier classIdentifier) model)
        {
            ChampionshipDto championship = model.championshipDto;
            ClassIdentifier classIdentifier = model.classIdentifier;
            PlotModel = new PlotModel()
            {
                Background = OxyColor.Parse("#FF181818"),
                PlotAreaBorderColor = OxyColor.Parse("#FFF1E9DB"),
            };

            PlotModel.Legends.Add(new Legend()
            {
                LegendBorderThickness = 1,
                LegendBorder = OxyColor.Parse("#FFF1E9DB"),
                LegendPlacement = LegendPlacement.Outside,
                LegendTextColor = OxyColor.Parse("#FFF1E9DB"),
                LegendBackground = OxyColors.Black,
            });

            ClassChampionshipDto classChampionshipDto = championship.GetOrCreateClassChampionship(classIdentifier);

            int maxPoints = classChampionshipDto.Drivers.Count != 0 ? classChampionshipDto.Drivers.Max(x => x.TotalPoints) + 10 : 100;
            int majorStep = championship.Scoring.SelectMany(x => x.Scoring).Max().RoundOff();
            int minorStep = majorStep / 10;

            PlotModel.Axes.Add(new LinearAxis
            {
                MinimumPadding = 10,
                Position = AxisPosition.Left,
                Minimum = 0,
                Maximum = maxPoints,
                TickStyle = TickStyle.Inside,
                AxislineColor = OxyColor.Parse("#FFF1E9DB"),
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Unit = "PTS",
                AxisTitleDistance = 0,
                MajorStep = majorStep,
                MinorStep = minorStep,
                TextColor = OxyColor.Parse("#FFF1E9DB"),
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColors.White,
                TitleColor = OxyColor.Parse("#FFF1E9DB"),
                AxisDistance = 0,
                TicklineColor = OxyColor.Parse("#FFF1E9DB"),
                MajorGridlineThickness = 0.5,
            });

            PlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = 0,
                Maximum = championship.GetAllSessions().Count(),
                TickStyle = TickStyle.Inside,
                AxislineColor = OxyColor.Parse("#FFF1E9DB"),
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Unit = "Session",
                AxisTitleDistance = 0,
                MinorTickSize = 0,
                MajorStep = 1,
                MinorStep = 1,
                TextColor = OxyColor.Parse("#FFF1E9DB"),
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColors.White,
                TitleColor = OxyColor.Parse("#FFF1E9DB"),
                AxisDistance = 0,
                TicklineColor = OxyColor.Parse("#FFF1E9DB"),
                MajorGridlineThickness = 0.5,
            });

            Dictionary<Guid, LineSeries> driversSeries = classChampionshipDto.Drivers.OrderBy(x => x.Position).ToDictionary(x => x.DriverGlobalKey, x => new LineSeries()
            {
                Title = x.DriverName,
                CanTrackerInterpolatePoints = false,
                TextColor = OxyColor.Parse("#FFF1E9DB"),
                MarkerType = MarkerType.Circle,
                MarkerSize = 5,
                StrokeThickness = 3,
                TrackerFormatString = "{0}\nEvent: {2}, PTS: {4}"
            });

            driversSeries.Values.ForEach(x => x.Points.Add(new DataPoint(0, 0)));

            int session = 1;
            foreach (SessionResultDto sessionResultDto in championship.GetAllResults())
            {
                ClassSessionResultDto classSessionResult = sessionResultDto.GetOrCreateClassSessionResult(classIdentifier);
                if (classSessionResult.ClassSessionResult.Count == 0)
                {
                    session++;
                    continue;
                }

                foreach (ClassDriverResultDto driverSessionResultDto in classSessionResult.ClassSessionResult)
                {
                    driversSeries[driverSessionResultDto.DriverGlobalKey].Points.Add(new DataPoint(session, driverSessionResultDto.TotalPoints));
                }

                session++;
            }

            driversSeries.Values.ForEach(PlotModel.Series.Add);
        }

        public override (ChampionshipDto championshipDto, ClassIdentifier classIdentifier) SaveToNewModel() => OriginalModel;
    }
}
