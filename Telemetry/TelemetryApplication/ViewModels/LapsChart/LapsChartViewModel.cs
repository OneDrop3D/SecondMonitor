namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.LapsChart
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using OxyPlot;
    using OxyPlot.Axes;
    using OxyPlot.Legends;
    using OxyPlot.Series;

    using SecondMonitor.Telemetry.TelemetryManagement.DTO;
    using SecondMonitor.ViewModels;

    public class LapsChartViewModel : AbstractViewModel<(IReadOnlyList<LapSummaryDto> laps, TimeSpan averageLapTime, TimeSpan meanLapTime)>
    {
        public PlotModel PlotModel { get; private set; }

        protected override void ApplyModel((IReadOnlyList<LapSummaryDto> laps, TimeSpan averageLapTime, TimeSpan meanLapTime) model)
        {
            if (PlotModel == null)
            {
                InitializePlotModel();
            }

            IReadOnlyList<LapSummaryDto> laps = model.laps;
            TimeSpan averageLap = model.averageLapTime;
            TimeSpan meanLapTime = model.meanLapTime;

            PlotModel.Axes.Clear();
            PlotModel.Series.Clear();

            if (laps.Count == 0)
            {
                PlotModel.InvalidatePlot(true);
                NotifyPropertyChanged(nameof(PlotModel));
                NotifyPropertyChanged(nameof(PlotModel.Series));
                return;
            }

            int maxLapTime = (int)(laps.Max(x => x.LapTimeSeconds) + 15);
            int minLapTime = (int)(laps.Min(x => x.LapTimeSeconds) - 15);

            List<IGrouping<string, LapSummaryDto>> lapsGrouped = laps.GroupBy(x => x.DriverName).ToList();

            LinearAxis yAxis = new LinearAxis
            {
                MinimumPadding = 10,
                Position = AxisPosition.Left,
                Minimum = minLapTime,
                Maximum = maxLapTime,
                TickStyle = TickStyle.Inside,
                AxislineColor = OxyColor.Parse("#FFF1E9DB"),
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Unit = "Seconds",
                AxisTitleDistance = 0,
                MajorStep = 10,
                MinorStep = 5,
                TextColor = OxyColor.Parse("#FFF1E9DB"),
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColors.White,
                TitleColor = OxyColor.Parse("#FFF1E9DB"),
                AxisDistance = 0,
                TicklineColor = OxyColor.Parse("#FFF1E9DB"),
                MajorGridlineThickness = 0.5,
                MinorGridlineThickness = 0.25,
                ExtraGridlineColor = OxyColors.Red,
                ExtraGridlineThickness = 0.5,
            };

            yAxis.ExtraGridlines = [averageLap.TotalSeconds, meanLapTime.TotalSeconds];

            PlotModel.Axes.Add(yAxis);

            PlotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = 0,
                Maximum = lapsGrouped.Max(x => x.Max(y => y.LapNumber)) + 1,
                TickStyle = TickStyle.Inside,
                AxislineColor = OxyColor.Parse("#FFF1E9DB"),
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Unit = "Lap",
                AxisTitleDistance = 0,
                MinorTickSize = 0,
                MajorStep = 1,
                MinorStep = 1,
                TextColor = OxyColor.Parse("#FFF1E9DB"),
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColors.White,
                TitleColor = OxyColor.Parse("#FFF1E9DB"),
                AxisDistance = 0,
                MajorGridlineThickness = 0.5,
                MinorGridlineThickness = 0.25,
                TicklineColor = OxyColor.Parse("#FFF1E9DB"),
            });

            foreach (IGrouping<string, LapSummaryDto> lapSummariesForDriver in lapsGrouped.OrderBy(x => x.Min(y => y.LapTimeSeconds)))
            {
                LineSeries newLineSeries = new LineSeries()
                {
                    Title = lapSummariesForDriver.Key,
                    CanTrackerInterpolatePoints = false,
                    LineStyle = LineStyle.Solid,
                    TextColor = OxyColor.Parse("#FFF1E9DB"),
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 5,
                    StrokeThickness = 2,
                    TrackerFormatString = "{0}\nLap: {2}, Time: {4}",
                };
                newLineSeries.Points.AddRange(lapSummariesForDriver.OrderBy(x => x.LapNumber).Select((y, i) => new DataPoint(y.LapNumber, y.LapTimeSeconds)).ToList());

                PlotModel.Series.Add(newLineSeries);
            }

            PlotModel.InvalidatePlot(true);
            NotifyPropertyChanged(nameof(PlotModel));
            NotifyPropertyChanged(nameof(PlotModel.Series));
        }

        public override (IReadOnlyList<LapSummaryDto> laps, TimeSpan averageLapTime, TimeSpan meanLapTime) SaveToNewModel()
        {
            throw new NotSupportedException();
        }

        private void InitializePlotModel()
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
        }
    }
}
