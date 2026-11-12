namespace SecondMonitor.Telemetry.TelemetryApplication.AggregatedCharts
{
    public static class AggregatedChartNames
    {
        public const string HistogramNamePrefix = "(H) ";
        public const string ScatterPlotNamePrefix = "(S) ";
        public const string LineSeriesPlotNamePrefix = "(LS) ";

        public const string SuspensionVelocityHistogramChartName = HistogramNamePrefix + "Suspension Velocity Histogram";
        public const string SpeedToRpmChartName = ScatterPlotNamePrefix + "Speed to Rpm";
        public const string SpeedToLongitudinalAccChartName = ScatterPlotNamePrefix + "Speed to Longitudinal Acceleration";
        public const string RpmToLongitudinalAccChartName = ScatterPlotNamePrefix + "Rpm to Longitudinal Acceleration";
        public const string RpmHistogramChartName = HistogramNamePrefix + "Rpm Histogram";
        public const string RideHeightHistogramChartName = HistogramNamePrefix + "Ride Height Histogram";
        public const string RideHeightToLongitudinalAccChartName = ScatterPlotNamePrefix + "Ride Height To Longitudinal Acceleration";
        public const string RideHeightToLateralAccChartName = ScatterPlotNamePrefix + "Ride Height To Lateral Acceleration";
        public const string RideHeightToSpeedChartName = ScatterPlotNamePrefix + "Ride Height To Speed";
        public const string SuspensionTravelToSpeedChartName = ScatterPlotNamePrefix + "Suspension Travel To Speed";
        public const string SpeedToDownforceChartName = ScatterPlotNamePrefix + "Speed to Downforce";
        public const string SpeedToRakeChartName = ScatterPlotNamePrefix + "Speed to Rake";
        public const string RearToFrontRollAngleChartName = ScatterPlotNamePrefix + "Rear to Front Roll Angles";
        public const string CamberToLateralAccelerationChartName = ScatterPlotNamePrefix + "Camber to Lateral Acceleration";
        public const string CamberHistogramChartName = HistogramNamePrefix + "Camber Histogram";
        public const string TyreLoadToCamberChartName = ScatterPlotNamePrefix + "Tyre Load To Camber";
        public const string TyreLoadToLatAccelerationChartName = ScatterPlotNamePrefix + "Tyre Load to Lateral Acceleration";
        public const string TyreLoadToLongAccelerationChartName = ScatterPlotNamePrefix + "Tyre Load to Longitudinal Acceleration";
        public const string TyreLoadHistogramChartName = HistogramNamePrefix + "Tyre Load Histogram";
        public const string PowerCurveChartName = ScatterPlotNamePrefix + "Power Curve";
        public const string TractionCircleChartName = ScatterPlotNamePrefix + "Traction Circle";
        public const string SpeedInTurnsHistogramChartName = HistogramNamePrefix + "Speed in Turns Histogram";
        public const string WheelSlipAccelerationChartName = ScatterPlotNamePrefix + "Wheel Slip to Speed (Acceleration)";
        public const string WheelSlipBrakingChartName = ScatterPlotNamePrefix + "Wheel Slip to Speed (Braking)";
        public const string LatAccelerationToFrontRollAngleChartName = ScatterPlotNamePrefix + "Lateral Acceleration to Front Roll Angle";
        public const string LatAccelerationToRearRollAngleChartName = ScatterPlotNamePrefix + "Lateral Acceleration to Rear Roll Angle";
        public const string LatAccToSteeringInput = ScatterPlotNamePrefix + "Lateral Acceleration to Steering Input";
        public const string SuspensionVelocityLineSeriesChartName = LineSeriesPlotNamePrefix + "Suspension Velocity Line Series";

        public static readonly string[] AllChartNames = new[]
        {
            SuspensionVelocityHistogramChartName, SpeedToRakeChartName, SpeedToRpmChartName, SpeedToLongitudinalAccChartName, RpmToLongitudinalAccChartName, RpmHistogramChartName,
            RideHeightHistogramChartName, RideHeightToLongitudinalAccChartName, RideHeightToLateralAccChartName, RideHeightToSpeedChartName, SpeedToDownforceChartName, RearToFrontRollAngleChartName,
            CamberToLateralAccelerationChartName, CamberHistogramChartName, TyreLoadToCamberChartName, TyreLoadToLatAccelerationChartName, TyreLoadToLongAccelerationChartName, TyreLoadHistogramChartName,
            PowerCurveChartName, TractionCircleChartName, SpeedInTurnsHistogramChartName, WheelSlipAccelerationChartName, WheelSlipBrakingChartName, SuspensionTravelToSpeedChartName,
            LatAccelerationToFrontRollAngleChartName, LatAccelerationToRearRollAngleChartName, LatAccToSteeringInput,
            SuspensionVelocityLineSeriesChartName
        };
    }
}
