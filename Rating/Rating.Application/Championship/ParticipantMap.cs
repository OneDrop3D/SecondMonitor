namespace SecondMonitor.Rating.Application.Championship
{
    using System.Collections.Generic;

    public class ParticipantMap<TParticipant, TResult>
    {
        public ParticipantMap(Dictionary<TParticipant, List<TResult>> map, List<TResult> entitiesWithUnknownParticipants)
        {
            Map = map;
            EntitiesWithUnknownParticipants = entitiesWithUnknownParticipants;
        }

        public Dictionary<TParticipant, List<TResult>> Map { get; }

        public List<TResult> EntitiesWithUnknownParticipants { get; }

        public int TotalParticipants => Map.Keys.Count;
    }
}