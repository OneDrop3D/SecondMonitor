namespace SecondMonitor.F12024Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CarMotionData
    {
        public float WorldPositionX; // World space X position
        public float WorldPositionY; // World space Y position
        public float WorldPositionZ; // World space Z position
        public float WorldVelocityX; // Velocity in world space X
        public float WorldVelocityY; // Velocity in world space Y
        public float WorldVelocityZ; // Velocity in world space Z
        public short WorldForwardDirX; // World space forward X direction (normalised)
        public short WorldForwardDirY; // World space forward Y direction (normalised)
        public short WorldForwardDirZ; // World space forward Z direction (normalised)
        public short WorldRightDirX; // World space right X direction (normalised)
        public short WorldRightDirY; // World space right Y direction (normalised)
        public short WorldRightDirZ; // World space right Z direction (normalised)
        public float GForceLateral; // Lateral G-Force component
        public float GForceLongitudinal; // Longitudinal G-Force component
        public float GForceVertical; // Vertical G-Force component
        public float Yaw; // Yaw angle in radians
        public float Pitch; // Pitch angle in radians
        public float Roll; // Roll angle in radians
    }
}