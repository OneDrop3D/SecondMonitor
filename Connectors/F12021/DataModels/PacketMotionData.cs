namespace SecondMonitor.F12021Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketMotionData
    {
        public PacketHeader MHeader; // Header

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22)]
        public CarMotionData[] CarMotionData; // Data for all cars on track

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
        public float[] WheelSlip; // Slip ratio for each wheel

        public float LocalVelocityX; // Velocity in local space
        public float LocalVelocityY; // Velocity in local space
        public float LocalVelocityZ; // Velocity in local space
        public float AngularVelocityX; // Angular velocity x-component
        public float AngularVelocityY; // Angular velocity y-component
        public float AngularVelocityZ; // Angular velocity z-component
        public float AngularAccelerationX; // Angular velocity x-component
        public float AngularAccelerationY; // Angular velocity y-component
        public float AngularAccelerationZ; // Angular velocity z-component
        public float FrontWheelsAngle; // Current front wheels angle in radians
    }
}