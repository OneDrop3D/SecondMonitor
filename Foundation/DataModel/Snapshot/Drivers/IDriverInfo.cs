namespace SecondMonitor.DataModel.Snapshot.Drivers
{
    using BasicProperties;
    using Systems;

    public interface IDriverInfo
    {
        string DriverSessionId { get; }

        string DriverShortName { get; }

        string DriverLongName { get; }

        string CarName { get;  }

        string CarClassName { get;  }

        string CarClassId { get; }

        bool IsSafetyCar { get; }

        string TeamName { get; }

        int CarRaceNumber { get; }

        bool IsCarRaceNumberFilled { get; }

        int CompletedLaps { get;  }

        bool InPits { get;  }

        bool IsPlayer { get;  }

        int Position { get;  }

        int PositionInClass { get;  }

        bool CurrentLapValid { get;  }

        double LapDistance { get; }

        double TotalDistance { get;  }

        double DistanceToPlayer { get;  }

        bool IsBeingLappedByPlayer { get;  }

        bool IsLappingPlayer { get;  }

        DriverFinishStatus FinishStatus { get;  }

        CarInfo CarInfo { get;  }

        DriverTimingInfo Timing { get;  }

        Point3D WorldPosition { get;  }

        DriverDebugInfo DriverDebugInfo { get; }

        Velocity Speed { get;  }

        bool IsCausingYellow { get; }
    }
}