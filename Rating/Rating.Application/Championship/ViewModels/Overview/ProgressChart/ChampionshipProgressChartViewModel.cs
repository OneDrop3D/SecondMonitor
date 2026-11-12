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

    public class ChampionshipProgressChartViewModel : AbstractViewModel<ChampionshipDto>
    {
        private string _header;
        public ChampionshipProgressChartViewModel()
        {
            PlotModel = new PlotModel();
        }

        public string Header
        {
            get => _header;
            set => SetProperty(ref _header, value);
        }

        public PlotModel PlotModel { get; private set; }
        protected override void ApplyModel(ChampionshipDto model)
        {
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

            int maxPoints = model.Drivers.Count != 0 ? model.Drivers.Max(x => x.TotalPoints) + 10 : 100;
            int majorStep = model.Scoring.SelectMany(x => x.Scoring).Max().RoundOff();
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
                Maximum = model.GetAllSessions().Count(),
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

            Dictionary<Guid, LineSeries> driversSeries = model.Drivers.OrderBy(x => x.Position).ToDictionary(x => x.GlobalKey, x => new LineSeries()
            {
                Title = x.LastUsedName,
                CanTrackerInterpolatePoints = false,
                TextColor = OxyColor.Parse("#FFF1E9DB"),
                MarkerType = MarkerType.Circle,
                MarkerSize = 5,
                StrokeThickness = 3,
                TrackerFormatString = "{0}\nEvent: {2}, PTS: {4}"
            });

            driversSeries.Values.ForEach(x => x.Points.Add(new DataPoint(0, 0)));

            int session = 1;
            foreach (SessionResultDto sessionResultDto in model.GetAllResults())
            {
                foreach (DriverSessionResultDto driverSessionResultDto in sessionResultDto.DriverSessionResult)
                {
                    driversSeries[driverSessionResultDto.DriverGuid].Points.Add(new DataPoint(session, driverSessionResultDto.TotalPoints));
                }

                session++;
            }

            driversSeries.Values.ForEach(PlotModel.Series.Add);
        }

        public override ChampionshipDto SaveToNewModel() => OriginalModel;
    }
}
