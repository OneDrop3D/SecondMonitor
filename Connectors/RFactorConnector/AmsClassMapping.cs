namespace SecondMonitor.RFactorConnector
{
    using System.Collections.Generic;

    public class AmsClassMapping
    {
        private static readonly Dictionary<string, string> ClassMap = new Dictionary<string, string>()
        {
            { "Skane", "Formula Truck" },
            { "Volvo", "Formula Truck" },
            { "Iveco", "Formula Truck" },
            { "FC", "Formula Truck" },
            { "Mannheim-Baden", "Formula Truck" },
            { "MAN Latin America", "Formula Truck" },
        };

        public string MapClass(string amsClassName)
        {
            return ClassMap.TryGetValue(amsClassName, out string mappedClassName) ? mappedClassName : amsClassName;
        }
    }
}