namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.MainWindow.GraphPanel
{
    public static class SeriesChartNames
    {
        public const string SeriesNamePrefix = "(L) ";
        public const string LapTimeChartName = SeriesNamePrefix + "Lap Time";
        public const string SteeringAngleChartName = SeriesNamePrefix + "Steering Angle";
        public const string SteeringInputChartName = SeriesNamePrefix + "Steering Input";
        public const string ThrottleChartName = SeriesNamePrefix + "Throttle";
        public const string BrakeChartName = SeriesNamePrefix + "Brake";
        public const string ClutchChartName = SeriesNamePrefix + "Clutch";
        public const string EngineRpmChartName = SeriesNamePrefix + "Engine RPM";
        public const string SpeedChartName = SeriesNamePrefix + "Speed";
        public const string GearChartName = SeriesNamePrefix + "Gear";
        public const string LatGChartName = SeriesNamePrefix + "Lateral G";
        public const string TotalGChartName = SeriesNamePrefix + "Total G";
        public const string LongGChartName = SeriesNamePrefix + "Longitudinal G";
        public const string BrakeTemperatureChartName = SeriesNamePrefix + "Brake Temperature";
        public const string TyrePressuresChartName = SeriesNamePrefix + "Tyre Pressure";
        public const string LeftFrontTyreTempChartName = SeriesNamePrefix + "LF Tyre Temps";
        public const string RightFrontTyreTempChartName = SeriesNamePrefix + "LR Tyre Temps";
        public const string LeftRearTyreTempChartName = SeriesNamePrefix + "RF Tyre Temps";
        public const string RightRearTyreTempChartName = SeriesNamePrefix + "RR Tyre Temps";
        public const string WheelRpsChartName = SeriesNamePrefix + "Wheel RPS";
        public const string SuspensionTravelChartName = SeriesNamePrefix + "Suspension Travel";
        public const string RideHeightChartName = SeriesNamePrefix + "Ride Height";
        public const string ChassisRideChartName = SeriesNamePrefix + "Chassis Ride Height";
        public const string SuspensionVelocityChartName = SeriesNamePrefix + "Suspension Velocity";
        public const string DownforceOverallChartName = SeriesNamePrefix + "Down Force (Overall)";
        public const string TurboBoostChartName = SeriesNamePrefix + "Turbo Boost";
        public const string DownforceFrontRearChartName = SeriesNamePrefix + "Down Force (Front/Rear)";
        public const string CamberChartName = SeriesNamePrefix + "Camber";
        public const string RakeChartName = SeriesNamePrefix + "Rake";
        public const string YawChartName = SeriesNamePrefix + "Yaw";
        public const string TyreLoadChartName = SeriesNamePrefix + "Tyre Load";
        public const string WheelSlipChartName = SeriesNamePrefix + "Wheel Slip";
        public const string CombinedPedalsChartName = SeriesNamePrefix + "Combined Pedals";
        public const string BrakesTemperatureFrontRearChartName = SeriesNamePrefix + "Brakes Temperature (Front / Rear)";
        public const string OutsideInsideTyresTempChartName = SeriesNamePrefix + "Inside - Outside Tyre Temp";
        public const string FrontRearTyreLoads = SeriesNamePrefix + "Tyre Load (Front / Rear)";
        public const string CrossTyreLoad = SeriesNamePrefix + "Cross Tyre Load (Front Left + Rear Right / Front Right + Rear Left)";
        public const string FrontTyresRps = SeriesNamePrefix + "Front Tyres RPS";
        public const string FrontTyresSlip = SeriesNamePrefix + "Front Tyres Slip";
        public const string FrontTyresRpsDiff = SeriesNamePrefix + "Front Tyres RPS Difference";
        public const string RearTyresRps = SeriesNamePrefix + "Rear Tyres RPS";
        public const string RearTyresSlip = SeriesNamePrefix + "Rear Tyres Slip";
        public const string RearTyresRpsDiff = SeriesNamePrefix + "Rear Tyres RPS Difference";
        public const string OilTemperature = SeriesNamePrefix + "Oil Temperature";
        public const string CoolantTemperature = SeriesNamePrefix + "Coolant Temperature";

        public static readonly string[] AllSeriesCharts = new[]
        {
            LapTimeChartName, SteeringAngleChartName, ThrottleChartName, BrakeChartName, EngineRpmChartName, SpeedChartName, GearChartName, LatGChartName, TotalGChartName, LongGChartName, BrakeTemperatureChartName, TyrePressuresChartName,
            LeftFrontTyreTempChartName, LeftRearTyreTempChartName, RightFrontTyreTempChartName, RightRearTyreTempChartName, WheelRpsChartName, SuspensionTravelChartName, RideHeightChartName, ChassisRideChartName, SuspensionVelocityChartName,
            DownforceOverallChartName, TurboBoostChartName, DownforceFrontRearChartName, CamberChartName, RakeChartName, YawChartName, TyreLoadChartName, WheelSlipChartName, CombinedPedalsChartName, BrakesTemperatureFrontRearChartName, OutsideInsideTyresTempChartName,
            FrontRearTyreLoads, SteeringInputChartName, CrossTyreLoad, FrontTyresRps, FrontTyresSlip, FrontTyresRpsDiff, RearTyresRps, RearTyresSlip, RearTyresRpsDiff, OilTemperature, CoolantTemperature
        };
    }
}