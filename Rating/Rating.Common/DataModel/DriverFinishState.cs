namespace SecondMonitor.Rating.Common.DataModel
{
    public class DriverFinishState
    {
        public DriverFinishState(string driverId, bool isPlayer, string driverName, string carName, string carClass, int finishPosition, double averagePosition)
        {
            DriverId = driverId;
            IsPlayer = isPlayer;
            DriverName = driverName;
            CarName = carName;
            CarClass = carClass;
            FinishPosition = finishPosition;
            AveragePosition = averagePosition;
        }

        public double AveragePosition { get; }

        public string DriverId { get; }

        public bool IsPlayer { get;  }
        public string DriverName { get;  }

        public string CarClass { get; }

        public string CarName { get; }
        public int FinishPosition { get;  }
    }
}