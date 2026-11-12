namespace SecondMonitor.ViewModels.SimulatorContent
{
    using System.Collections.Generic;
    using DataModel.SimulatorContent;

    public interface ISimulatorContentProvider
    {
        IReadOnlyList<string> GetSimulatorsWithContent();
        IReadOnlyList<CarClass> GetAllCarClassesForSimulator(string simulatorName);
        IReadOnlyList<Track> GetAllTracksForSimulator(string simulatorName);
        IReadOnlyList<Car> GetAllCarsForSimulator(string simulatorName);
        IReadOnlyList<Car> GetAllCarsForSimulator(string simulatorName, string carClass);
        Track GetRandomTrack(string simulatorName);
        Track GetRandomTrack(string simulatorName, List<string> tracksToIgnore);
        CarClass GetRandomClass(string simulatorName);
        bool TryGetTrack(string simulator, string trackName, out Track track);
    }
}