namespace SecondMonitor.Rating.Application.Championship.Manufacturers
{
    using System.Collections.Generic;

    public class ManufacturerMap<T> : ParticipantMap<Manufacturer, T>
    {
        public ManufacturerMap(Dictionary<Manufacturer, List<T>> map, List<T> entitiesWithUnknownParticipants) : base(map, entitiesWithUnknownParticipants)
        {
        }

        public int TotalManufacturers => TotalParticipants;
    }
}