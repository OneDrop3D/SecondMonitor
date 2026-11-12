namespace SecondMonitor.Contracts.TrackMap
{
    using System;
    using DataModel.TrackMap;

    public interface IMapManagementController
    {
        event EventHandler<MapEventArgs> NewMapAvailable;
        event EventHandler<MapEventArgs> MapRemoved;

        TimeSpan MapPointsInterval { get; set; }

        bool TryGetMap(string simulator, string trackFullName, out TrackMapDto trackMapDto);

        void RemoveMap(string simulator, string trackFullName);
        TrackMapDto RotateMapRight(string simulator, string trackFullName);
        TrackMapDto RotateMapLeft(string simulator, string trackFullName);
    }
}