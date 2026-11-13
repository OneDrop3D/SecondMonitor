namespace SecondMonitor.Hub.Core.Race
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Compléments pour l'accueil (tableau de bord) : temps / tours restants et journal des événements de course.
    /// </summary>
    public sealed partial class RaceEngine
    {
        private readonly RaceEventLog _events = new();

        /// <summary>Événements de course postérieurs à <paramref name="afterId"/> (0 = tous), avec l'identifiant de session du journal.</summary>
        public (long SessionId, IReadOnlyList<RaceEventDto> Events) RaceEvents(long afterId)
        {
            lock (_lock)
            {
                return (_events.SessionId, _events.Since(afterId));
            }
        }

        /// <summary>Appelé sous le verrou, à la fin de chaque instantané.</summary>
        private void DecorateSnapshot(RaceSnapshot snap)
        {
            if (snap == null || !snap.HasData || _last?.SessionInfo == null)
            {
                return;
            }

            snap.RaceLength = BuildRaceLength(snap);
            _events.Observe(snap, _sessionKey);
            snap.LastEventId = _events.LastId;
            snap.EventSessionId = _events.SessionId;
        }

        private RaceLengthDto BuildRaceLength(RaceSnapshot snap)
        {
            DriverRow leader = snap.Drivers.FirstOrDefault(d => d.Position == 1) ?? snap.Drivers.FirstOrDefault();
            DriverRow me = snap.Drivers.FirstOrDefault(d => d.IsPlayer);
            if (leader == null)
            {
                return null;
            }

            var input = new RaceLengthInput
            {
                IsRace = snap.SessionType == "Race",
                LengthType = snap.LengthType,
                TotalLaps = snap.TotalLaps,
                TimeRemaining = _last.SessionInfo.SessionTimeRemaining,
                SessionTime = snap.SessionTime,
                LeaderDistance = leader.Laps + leader.LapFraction,
                LeaderPace = Pace(leader),
                LeaderFinished = leader.FinishStatus == "Finished",
                PlayerDistance = me != null ? me.Laps + me.LapFraction : null,
                PlayerPace = me != null ? snap.Player?.AverageLap ?? Pace(me) : null,
                PlayerIsLeader = me != null && me.Id == leader.Id,
                PlayerFinished = me?.FinishStatus == "Finished",
            };
            return RaceLength.Estimate(input);
        }

        /// <summary>Rythme réel : moyenne des derniers tours propres, sinon les derniers tours, sinon le meilleur tour un peu majoré.</summary>
        private double? Pace(DriverRow d)
        {
            if (_trackers.TryGetValue(d.Id, out DriverTracker t) && t.AverageOfLastCleanLaps(5) is double avg)
            {
                return avg;
            }

            return d.PaceLast3 ?? d.LastLap ?? (d.BestLap is double b ? b * 1.02 : null);
        }
    }
}
