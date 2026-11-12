namespace SecondMonitor.Timing.Api.Data
{
    using SecondMonitor.DataModel.Snapshot.Systems;

    public class WheelData
    {
        public WheelData(WheelInfo wheelInfo)
        {
            Rps = wheelInfo.Rps;
            SuspensionTravelInMm = wheelInfo.SuspensionTravel.InMillimeter;
            RideHeightInMm = wheelInfo.RideHeight.InMillimeter;
            BrakeTemperatureCelsius = wheelInfo.BrakeTemperature.ActualQuantity.InCelsius;
            TyrePressureKpa = wheelInfo.TyrePressure.ActualQuantity.InKpa;
            SuspensionVelocityMmPerSecond = wheelInfo.SuspensionVelocity.InMillimeterPerSecond;
            TyreType = wheelInfo.TyreType;
            TyreWear = wheelInfo.TyreWear.ActualWear;
            IsDetached = wheelInfo.Detached;
            DirtLevel = wheelInfo.DirtLevel;
            LeftTyreTempCelsius = wheelInfo.LeftTyreTemp.ActualQuantity.InCelsius;
            RightTyreTempCelsius = wheelInfo.RightTyreTemp.ActualQuantity.InCelsius;
            CenterTyreTempCelsius = wheelInfo.CenterTyreTemp.ActualQuantity.InCelsius;
            TyreCoreTemperatureCelsius = wheelInfo.TyreCoreTemperature.ActualQuantity.InCelsius;
            CamberAngle = wheelInfo.Camber.InDegrees;
            TyreLoadNewton = wheelInfo.TyreLoad.InNewtons;
            Slip = wheelInfo.Slip;
            BrakesDamage = wheelInfo.BrakesDamage.Damage;
            TyreSet = wheelInfo.TyreSet;
        }

        public double Rps { get; } //Currently in Radians / s
        public double SuspensionTravelInMm { get; }

        public double RideHeightInMm { get; }

        public double BrakeTemperatureCelsius { get; }

        public double TyrePressureKpa { get; }

        public string TyreType { get; }

        public double TyreWear { get; }

        public bool IsDetached { get; }

        public double DirtLevel { get; }

        public double LeftTyreTempCelsius { get; }

        public double RightTyreTempCelsius { get; }

        public double CenterTyreTempCelsius { get; }

        public double TyreCoreTemperatureCelsius { get; }

        public double SuspensionVelocityMmPerSecond { get; }

        public double CamberAngle { get; }

        public double TyreLoadNewton { get; }

        public double Slip { get; }

        public double BrakesDamage { get; }

        public int TyreSet { get; }
    }
}
