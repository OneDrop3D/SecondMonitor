namespace SecondMonitor.R3EConnector.Properties
{
    using System.Collections.Generic;

    using Foundation.Connectors.WheelInformation;

    public class R3ETyresPressuresFiller : AbstractCodeDefinedIdealWheelQuantitiesByClass
    {
        private static readonly Dictionary<string, TyresProperties> TyreMap = new Dictionary<string, TyresProperties>()
        {
            {
                "FR X-90 Cup", new TyresProperties(new TyreProperties(0, 0, 140, 5),
                    new TyreProperties(0, 0, 145, 5))
            },
        };

        protected override Dictionary<string, TyresProperties> ClassTyrePropertiesMap => TyreMap;
    }
}
