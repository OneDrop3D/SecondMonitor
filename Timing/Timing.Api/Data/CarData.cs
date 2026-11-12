namespace SecondMonitor.Timing.Api.Data
{
    using SecondMonitor.DataModel.Snapshot.Systems;
    using SecondMonitor.Timing.Api.Data.Telemetry;

    public class CarData
    {
        public CarData(CarInfo carInfo)
        {
            WheelsData = new WheelsData(carInfo.WheelsInfo);
            FuelSystemData = new FuelTankData(carInfo.FuelSystemInfo);
            AccelerationData = new AccelerationData(carInfo.Acceleration);
            FrontHeightMm = carInfo.FrontHeight.InMillimeter;
            RearHeightMm = carInfo.RearHeight.InMillimeter;
            CurrentGear = carInfo.CurrentGear;
            EngineRpm = carInfo.EngineRpm;
            TurboPressureKpa = carInfo.TurboPressure.InKpa;
            IsSpeedLimiterEngaged = carInfo.SpeedLimiterEngaged;
            DrsSystem = new DrsData(carInfo.DrsSystem);
            P2PBoostData = new P2PBoostData(carInfo.BoostSystem);
            OverallDownforceN = carInfo.OverallDownForce.InNewtons;
            FrontDownforceN = carInfo.FrontDownForce.InNewtons;
            RearDownforceN = carInfo.RearDownForce.InNewtons;
            FrontRollAngleDegrees = carInfo.FrontRollAngle.InDegrees;
            RearRollAngleDegrees = carInfo.RearRollAngle.InDegrees;
            WorldOrientation = new OrientationData(carInfo.WorldOrientation);
            EnginePowerKw = carInfo.EnginePower.InKw;
            EngineTorqueNm = carInfo.EngineTorque.InNm;
            AbsInfo = new SafetySystemData(carInfo.AbsInfo);
            TcInfo = new SafetySystemData(carInfo.TcInfo);
            HybridSystem = new HybridSystemData(carInfo.HybridSystem);
        }

        public WheelsData WheelsData { get; }

        public FuelTankData FuelSystemData { get; }

        public AccelerationData AccelerationData { get; }

        public double FrontHeightMm { get; }

        public double RearHeightMm { get; }

        public string CurrentGear { get; }

        public int EngineRpm { get; } = 0;

        public double TurboPressureKpa { get; }

        public bool IsSpeedLimiterEngaged { get; }

        public DrsData DrsSystem { get; }

        public P2PBoostData P2PBoostData { get; }

        public double OverallDownforceN { get; }

        public double FrontDownforceN { get; }

        public double RearDownforceN { get; }

        public double FrontRollAngleDegrees { get; }

        public double RearRollAngleDegrees { get; }

        public OrientationData WorldOrientation { get; set; }

        public double EnginePowerKw { get; set; }

        public double EngineTorqueNm { get; set; }

        public SafetySystemData AbsInfo { get; set; }

        public SafetySystemData TcInfo { get; set; }

        public HybridSystemData HybridSystem { get; set; }
    }
}
