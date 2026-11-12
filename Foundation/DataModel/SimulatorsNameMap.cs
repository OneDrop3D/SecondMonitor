namespace SecondMonitor.DataModel
{
    using System;

    public static class SimulatorsNameMap
    {
        public const string NotConnected = "Not connected";

        public const string R3ESimName = "R3E";
        public const string AcSimName = "Assetto Corsa";
        public const string AccSimName = "ACC";
        public const string ForzaMotorsportSimName = "Forza Motorsport";
        public const string PCars2SimName = "PCars 2";
        public const string Ams2SimName = "AMS 2";
        public const string AmsSimName = "AMS";
        public const string Gtr2SimName = "GTR2";
        public const string Rf2SimName = "RFactor 2";
        public const string PCarsSimName = "Pcars";
        public const string F12019 = "F1 2019";
        public const string F12020 = "F1 2020";
        public const string F12021 = "F1 2021";
        public const string F12024 = "F1 2024";
        public const string LmUSimName = "LM Ultimate";

        public const string LmUHyperCarClassId = "Hyper";
        public const string LmUGT3CarClassId = "GT3";

        public static bool IsR3ESimulator(string simulatorName)
        {
            return simulatorName.Equals(R3ESimName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsNotConnected(string simulatorName)
        {
            return simulatorName.Equals(NotConnected, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}