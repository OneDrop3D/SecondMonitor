namespace SecondMonitor.F12024Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketMotionExData
    {
        public PacketHeader Header; // Header
        // Extra player car ONLY data
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] SuspensionPosition; // Note: All wheel arrays have the following order:

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] SuspensionVelocity; // RL, RR, FL, FR

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] SuspensionAcceleration; // RL, RR, FL, FR

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] WheelSpeed; // Speed of each wheel

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] WheelSlipRatio; // Slip ratio for each wheel

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] WheelSlipAngle; // Slip angles for each wheel

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] WheelLatForce; // Lateral forces for each wheel

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] WheelLongForce; // Longitudinal forces for each wheel

        public float HeightOfCOGAboveGround; // Height of centre of gravity above ground
        public float LocalVelocityX; // Velocity in local space – metres/s
        public float LocalVelocityY; // Velocity in local space
        public float LocalVelocityZ; // Velocity in local space
        public float AngularVelocityX; // Angular velocity x-component – radians/s
        public float AngularVelocityY; // Angular velocity y-component
        public float AngularVelocityZ; // Angular velocity z-component
        public float AngularAccelerationX; // Angular acceleration x-component – radians/s/s
        public float AngularAccelerationY; // Angular acceleration y-component
        public float AngularAccelerationZ; // Angular acceleration z-component
        public float FrontWheelsAngle; // Current front wheels angle in radians
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] WheelVertForce; // Vertical forces for each wheel
        public float FrontAeroHeight; // Front plank edge height above road surface
        public float RearAeroHeight; // Rear plank edge height above road surface
        public float FrontRollAngle; // Roll angle of the front suspension
        public float RearRollAngle; // Roll angle of the rear suspension
        public float ChassisYaw; // Yaw angle of the chassis relative to the direction
        // of motion - radians
    }
}
