namespace SecondMonitor.ForzaMotorsportConnector.DataModels
{
    internal class ForzaMotorsportCarInfo
    {
        public int CarOrdinal { get; set; }

        public int Year { get; set; }
        
        public string Make { get; set; }
        
        public string Model { get; set; }
    }

    internal enum ForzaMotorsportCarClass
    {
        X = 7,
        P = 6,
        R = 5,
        S = 4,
        A = 3,
        B = 2,
        C = 1,
        D = 0
    }
}
