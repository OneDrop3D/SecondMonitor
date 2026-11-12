namespace SecondMonitor.Telemetry.TelemetryApplication.ViewModels.GraphPanel.Wheels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using DataModel.BasicProperties;
    using DataModel.Snapshot.Systems;
    using DataModel.Telemetry;
    using OxyPlot;
    using OxyPlot.Annotations;
    using OxyPlot.Series;

    using SecondMonitor.DataModel.Extensions;

    using TelemetryManagement.DTO;
    using TelemetryManagement.Settings;

    public abstract class AbstractTyreTemperaturesViewModel : AbstractGraphViewModel
    {
        private const string LeftTemperatureName = "Left";
        private const string RightTemperatureName = "Right";
        private const string MiddleTemperatureName = "Middle";
        private const string CoreTemperatureName = "Core";

        private const LineStyle LeftSideLineStyle = LineStyle.Solid;
        private const LineStyle RightSideLineStyle = LineStyle.Solid;
        private const LineStyle CenterSideLineStyle = LineStyle.Solid;
        private const LineStyle CoreSideLineStyle = LineStyle.Solid;

        private LineAnnotation _hotTempAnnotation;
        private LineAnnotation _coldTempAnnotation;

        private bool _leftTemperatureVisible;
        private bool _middleTemperatureVisible;
        private bool _rightTemperatureVisible;
        private bool _coreTemperatureVisible;

        protected AbstractTyreTemperaturesViewModel()
        {
            _leftTemperatureVisible = true;
            _middleTemperatureVisible = true;
            _rightTemperatureVisible = true;
            _coreTemperatureVisible = true;
        }

        public override string Title => TyrePrefix + " Tyre Temperatures";
        protected abstract string TyrePrefix { get; }
        protected override string YUnits => Temperature.GetUnitSymbol(UnitsCollection.TemperatureUnits);
        protected override double YTickInterval => 20;
        protected override bool CanYZoom => true;

        public bool LeftTemperatureVisible
        {
            get => _leftTemperatureVisible;
            set
            {
                SetProperty(ref _leftTemperatureVisible, value);
                NotifyTyreTempVisibilityChanged();
                UpdateSeriesVisibility();
            }
        }

        public bool MiddleTemperatureVisible
        {
            get => _middleTemperatureVisible;
            set
            {
                SetProperty(ref _middleTemperatureVisible, value);
                NotifyTyreTempVisibilityChanged();
                UpdateSeriesVisibility();
            }
        }

        public bool RightTemperatureVisible
        {
            get => _rightTemperatureVisible;
            set
            {
                SetProperty(ref _rightTemperatureVisible, value);
                NotifyTyreTempVisibilityChanged();
                UpdateSeriesVisibility();
            }
        }

        public bool CoreTemperatureVisible
        {
            get => _coreTemperatureVisible;
            set
            {
                SetProperty(ref _coreTemperatureVisible, value);
                NotifyTyreTempVisibilityChanged();
                UpdateSeriesVisibility();
            }
        }

        protected abstract Func<Wheels, WheelInfo> WheelSelectionFunction { get; }

        /*protected override void UpdateYMaximum(LapTelemetryDto lapTelemetry)
        {

        }*/

        protected override void SubscribeGraphViewSync()
        {
            base.SubscribeGraphViewSync();
            if (GraphViewSynchronization == null)
            {
                return;
            }

            GraphViewSynchronization.TyreTempVisibilityChanged += GraphViewSynchronization_TyreTempVisibilityChanged;
        }

        protected override void UnsubscribeGraphViewSync()
        {
            base.UnsubscribeGraphViewSync();
            if (GraphViewSynchronization == null)
            {
                return;
            }

            GraphViewSynchronization.TyreTempVisibilityChanged -= GraphViewSynchronization_TyreTempVisibilityChanged;
        }

        protected override List<LineSeries> GetLineSeries(LapSummaryDto lapSummary, CarPropertiesDto carPropertiesDto, List<TimedTelemetrySnapshot> dataPoints, OxyColor color)
        {
            LineSeries[] lineSeries = new LineSeries[4];

            string baseTitle = $"Lap {lapSummary.CustomDisplayName}";
            lineSeries[0] = CreateLineSeries(baseTitle + " " + LeftTemperatureName, color.ChangeSaturation(0.5), LeftSideLineStyle);
            lineSeries[0].IsVisible = LeftTemperatureVisible;

            lineSeries[1] = CreateLineSeries(baseTitle + " " + RightTemperatureName, color.ChangeSaturation(1.5), RightSideLineStyle);
            lineSeries[1].IsVisible = RightTemperatureVisible;

            lineSeries[2] = CreateLineSeries(baseTitle + " " + MiddleTemperatureName, color, CenterSideLineStyle);
            lineSeries[2].IsVisible = MiddleTemperatureVisible;

            lineSeries[3] = CreateLineSeries(baseTitle + " " + CoreTemperatureName, color.ChangeIntensity(0.5), CoreSideLineStyle);
            lineSeries[3].IsVisible = CoreTemperatureVisible;

            lineSeries[0].Points.AddRange(dataPoints.Select(x => new DataPoint(GetXValue(x), WheelSelectionFunction(x.PlayerData.CarInfo.WheelsInfo).LeftTyreTemp.ActualQuantity.GetValueInUnits(UnitsCollection.TemperatureUnits))));
            lineSeries[1].Points.AddRange(dataPoints.Select(x => new DataPoint(GetXValue(x), WheelSelectionFunction(x.PlayerData.CarInfo.WheelsInfo).RightTyreTemp.ActualQuantity.GetValueInUnits(UnitsCollection.TemperatureUnits))));
            lineSeries[2].Points.AddRange(dataPoints.Select(x => new DataPoint(GetXValue(x), WheelSelectionFunction(x.PlayerData.CarInfo.WheelsInfo).CenterTyreTemp.ActualQuantity.GetValueInUnits(UnitsCollection.TemperatureUnits))));
            lineSeries[3].Points.AddRange(dataPoints.Select(x => new DataPoint(GetXValue(x), WheelSelectionFunction(x.PlayerData.CarInfo.WheelsInfo).TyreCoreTemperature.ActualQuantity.GetValueInUnits(UnitsCollection.TemperatureUnits))));

            double newMax = Math.Max(Math.Max(Math.Max(lineSeries[0].Points.Max(x => x.Y), lineSeries[1].Points.Max(x => x.Y)), lineSeries[2].Points.Max(x => x.Y)), lineSeries[3].Points.Max(x => x.Y)) * 1.1;
            if (newMax > YMaximum)
            {
                YMaximum = newMax;
            }

            return lineSeries.ToList();
        }

        protected override string GetDataInfo(TimedTelemetrySnapshot timedTelemetrySnapshot, CarPropertiesDto carPropertiesDto)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"L: {WheelSelectionFunction(timedTelemetrySnapshot.PlayerData.CarInfo.WheelsInfo).LeftTyreTemp.ActualQuantity.GetValueInUnits(UnitsCollection.TemperatureUnits).ToStringScalableDecimals()}");
            sb.AppendLine($"M: {WheelSelectionFunction(timedTelemetrySnapshot.PlayerData.CarInfo.WheelsInfo).CenterTyreTemp.ActualQuantity.GetValueInUnits(UnitsCollection.TemperatureUnits).ToStringScalableDecimals()}");
            sb.AppendLine($"R: {WheelSelectionFunction(timedTelemetrySnapshot.PlayerData.CarInfo.WheelsInfo).RightTyreTemp.ActualQuantity.GetValueInUnits(UnitsCollection.TemperatureUnits).ToStringScalableDecimals()}");
            sb.AppendLine($"Core:  {WheelSelectionFunction(timedTelemetrySnapshot.PlayerData.CarInfo.WheelsInfo).TyreCoreTemperature.ActualQuantity.GetValueInUnits(UnitsCollection.TemperatureUnits).ToStringScalableDecimals()}");
            return sb.ToString();
        }

        protected override void ApplyNewLineColor(List<LineSeries> series, OxyColor newColor)
        {
            series.First(x => x.Title.Contains(LeftTemperatureName)).Color = newColor.ChangeSaturation(0.5);
            series.First(x => x.Title.Contains(RightTemperatureName)).Color = newColor.ChangeSaturation(1.5);
            series.First(x => x.Title.Contains(MiddleTemperatureName)).Color = newColor;
            series.First(x => x.Title.Contains(CoreTemperatureName)).Color = newColor.ChangeIntensity(0.5);
        }

        private void GraphViewSynchronization_TyreTempVisibilityChanged(object sender, Controllers.Synchronization.Graphs.TyreTempVisibilityArgs e)
        {
            if (!SyncWithOtherGraphs)
            {
                return;
            }

            _leftTemperatureVisible = e.LeftTempVisible;
            _rightTemperatureVisible = e.RightTempVisible;
            _coreTemperatureVisible = e.CoreTempVisible;
            _middleTemperatureVisible = e.MiddleTempVisible;

            NotifyPropertyChanged(nameof(LeftTemperatureVisible));
            NotifyPropertyChanged(nameof(MiddleTemperatureVisible));
            NotifyPropertyChanged(nameof(RightTemperatureVisible));
            NotifyPropertyChanged(nameof(CoreTemperatureVisible));

            UpdateSeriesVisibility();
        }

        private void UpdateSeriesVisibility()
        {
            foreach (List<LineSeries> lineSeries in LoadedSeries.Values.Select(x => x.LineSeries))
            {
                foreach (LineSeries series in lineSeries)
                {
                    if (series.Title.Contains(LeftTemperatureName))
                    {
                        series.IsVisible = LeftTemperatureVisible;
                    }

                    if (series.Title.Contains(MiddleTemperatureName))
                    {
                        series.IsVisible = MiddleTemperatureVisible;
                    }

                    if (series.Title.Contains(RightTemperatureName))
                    {
                        series.IsVisible = RightTemperatureVisible;
                    }

                    if (series.Title.Contains(CoreTemperatureName))
                    {
                        series.IsVisible = CoreTemperatureVisible;
                    }
                }
            }

            InvalidatePlot();
        }

        protected override void PreviewOnLapLoaded(LapTelemetryDto lapTelemetryDto, CarPropertiesDto carPropertiesDto)
        {
            base.PreviewOnLapLoaded(lapTelemetryDto, carPropertiesDto);
            var telemetrySnapshot = lapTelemetryDto.DataPoints.FirstOrDefault(x =>
                x.PlayerData != null &&
                !WheelSelectionFunction(x.PlayerData.CarInfo.WheelsInfo).LeftTyreTemp.IdealQuantity.IsZero &&
                !WheelSelectionFunction(x.PlayerData.CarInfo.WheelsInfo).LeftTyreTemp.IdealQuantityWindow.IsZero);
            if (telemetrySnapshot == null)
            {
                RemoveLineAnnotations();
                return;
            }

            AddLineAnnotations();
            OptimalQuantity<Temperature> tyreTemperature = WheelSelectionFunction(telemetrySnapshot.PlayerData.CarInfo.WheelsInfo).LeftTyreTemp;
            _hotTempAnnotation.Y = Temperature.FromCelsius(tyreTemperature.IdealQuantity.InCelsius + tyreTemperature.IdealQuantityWindow.InCelsius).GetValueInUnits(UnitsCollection.TemperatureUnits);
            _coldTempAnnotation.Y = Temperature.FromCelsius(tyreTemperature.IdealQuantity.InCelsius - tyreTemperature.IdealQuantityWindow.InCelsius).GetValueInUnits(UnitsCollection.TemperatureUnits);
        }

        private void AddLineAnnotations()
        {
            if (_hotTempAnnotation != null && _coldTempAnnotation != null)
            {
                return;
            }

            _hotTempAnnotation = new LineAnnotation() { Color = OxyColor.Parse("#ff0000"), StrokeThickness = 2, Type = LineAnnotationType.Horizontal, LineStyle = LineStyle.Solid };
            _coldTempAnnotation = new LineAnnotation() { Color = OxyColor.Parse("#668cff"), StrokeThickness = 2, Type = LineAnnotationType.Horizontal, LineStyle = LineStyle.Solid };
            AddLineAnnotations(new List<Annotation>() { _hotTempAnnotation, _coldTempAnnotation });
        }

        private void RemoveLineAnnotations()
        {
            if (_hotTempAnnotation == null || _coldTempAnnotation == null)
            {
                return;
            }

            RemoveLineAnnotations(new List<Annotation>() { _hotTempAnnotation, _coldTempAnnotation });
        }

        private void NotifyTyreTempVisibilityChanged()
        {
            if (!SyncWithOtherGraphs)
            {
                return;
            }

            GraphViewSynchronization.NotifyTyreTempVisibilityChanged(this, LeftTemperatureVisible, MiddleTemperatureVisible, RightTemperatureVisible, CoreTemperatureVisible);
        }
    }
}