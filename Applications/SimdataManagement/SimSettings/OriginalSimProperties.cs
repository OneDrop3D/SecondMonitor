namespace SecondMonitor.SimdataManagement.SimSettings
{
    internal class OriginalSimProperties
    {
        public double IdealBrakeTemperature { get; set; }
        public double IdealBrakeTemperatureWindow { get; set; }

        public double FrontIdealTyreTemp { get; set; }
        public double FrontIdealTyreTempWindow { get; set; }
        public double RearIdealTyreTempWindow { get; set; }
        public double RearIdealTyreTemp { get; set; }

        public double FrontIdealTyrePressure { get; set; }
        public double FrontIdealTyrePressureWindow { get; set; }
        public double RearIdealTyrePressureWindow { get; set; }
        public double RearIdealTyrePressure { get; set; }
    }
}