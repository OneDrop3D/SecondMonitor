namespace SecondMonitor.Contracts.TrackMap
{
    using DataModel.TrackMap;

    public interface IMapsLoader
    {
        bool TryLoadMap(string simulator, string trackName, out TrackMapDto trackMapDto);
        void SaveMap(string simulator, string trackName, TrackMapDto trackMapDto);
        void RemoveMap(string simulator, string trackName);
    }
}