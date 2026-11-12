namespace SecondMonitor.F12024Connector.DataModels
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct CarSetupData
    {
        public byte FrontWing;                // Front wing aero

        public byte RearWing;                 // Rear wing aero

        public byte OnThrottle;               // Differential adjustment on throttle (percentage)

        public byte OffThrottle;              // Differential adjustment off throttle (percentage)

        public float FrontCamber;              // Front camber angle (suspension geometry)

        public float RearCamber;               // Rear camber angle (suspension geometry)

        public float FrontToe;                 // Front toe angle (suspension geometry)

        public float RearToe;                  // Rear toe angle (suspension geometry)

        public byte FrontSuspension;          // Front suspension

        public byte RearSuspension;           // Rear suspension

        public byte FrontAntiRollBar;         // Front anti-roll bar

        public byte RearAntiRollBar;          // Front anti-roll bar

        public byte FrontSuspensionHeight;    // Front ride height

        public byte RearSuspensionHeight;     // Rear ride height

        public byte BrakePressure;            // Brake pressure (percentage)

        public byte BrakeBias;                // Brake bias (percentage)
        
        public byte EngineBraking;                // Engine braking (percentage)

        public float RearLeftTyrePressure;         // Rear tyre pressure (PSI)
        public float RearRightTyrePressure;        // Rear tyre pressure (PSI)
        public float FrontLeftTyrePressure;         // Front tyre pressure (PSI)
        public float FrontRightTyrePressure;        // Front tyre pressure (PSI)

        public byte Ballast;                  // Ballast

        public float FuelLoad;                 // Fuel load
    }
}