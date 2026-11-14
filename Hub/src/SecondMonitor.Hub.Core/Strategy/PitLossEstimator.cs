namespace SecondMonitor.Hub.Core.Strategy
{
    using System;
    using System.Globalization;
    using SecondMonitor.Hub.Core.Ai;
    using SecondMonitor.Hub.Core.Race;
    using SecondMonitor.Hub.Core.Track;

    /// <summary>
    /// Temps perdu par un arrêt AVANT qu'un arrêt soit vu en course : voie des stands du circuit reconnu
    /// (Track/Library/pitlane.json, pour la catégorie du jeu) plus le temps immobile habituel de la catégorie
    /// (ravitaillement et pneus en endurance, pneus seuls en F1). Null si le circuit n'est pas reconnu ou n'a pas de valeur.
    /// </summary>
    public static class PitLossEstimator
    {
        private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");
        private static Recognized _lastTrack; // remplacé d'un bloc : jamais une clé avec le circuit d'une autre

        public static PitLossEstimate For(RaceSnapshot snap)
        {
            if (snap?.Track == null)
            {
                return null;
            }

            LibraryTrack track = Recognize(snap.Track);
            PitLaneEntry circuit = PitLaneLibrary.Get(track?.Id);
            if (circuit == null)
            {
                return null;
            }

            CarContext car = CarContext.From(snap);
            GamePitRules rules = PitLaneLibrary.Rules(car.Game);
            PitLaneValue lane = circuit.For(rules?.Category, rules?.SpeedLimitKph);
            if (lane?.PitLaneLossSeconds is not double laneLoss)
            {
                return null;
            }

            StopTimeEntry stop = PitLaneLibrary.StopTime(car.Game, car.CarClass);
            double stopTime = stop?.Seconds ?? 0;
            string laneText = lane.IsEstimate ? $"{N(laneLoss)} s de voie des stands, estimées sur sa longueur," : $"{N(laneLoss)} s de voie des stands";
            return new PitLossEstimate
            {
                TrackId = circuit.Id,
                TrackName = track.Name,
                LaneLoss = Math.Round(laneLoss, 1),
                LaneCategory = lane.Category,
                SpeedLimitKph = lane.SpeedLimitKph,
                LaneConfidence = lane.Confidence,
                LaneIsEstimate = lane.IsEstimate,
                StopTime = stopTime,
                StopDetail = stop?.Detail,
                Total = Math.Round(laneLoss + stopTime, 1),
                Source = stopTime > 0
                    ? $"d'après le circuit : {laneText} et environ {N(stopTime)} s d'arrêt"
                    : $"d'après le circuit : {laneText}, sans compter l'arrêt",
            };
        }

        /// <summary>Le circuit de la bibliothèque des tracés, retenu tant que le jeu annonce le même (une recherche par circuit, pas à chaque image).</summary>
        private static LibraryTrack Recognize(TrackDto t)
        {
            string key = $"{t.Name}|{t.Layout}|{Math.Round(t.Length)}";
            Recognized last = _lastTrack;
            if (last?.Key == key)
            {
                return last.Track;
            }

            LibraryTrack found = TrackLibrary.Find(t.Name, t.Layout, t.Length);
            _lastTrack = new Recognized(key, found);
            return found;
        }

        private sealed record Recognized(string Key, LibraryTrack Track);

        private static string N(double v) => v.ToString("F0", Fr);
    }

    public sealed class PitLossEstimate
    {
        public string TrackId { get; set; }

        public string TrackName { get; set; }

        /// <summary>Passage dans la voie des stands, sans l'arrêt (s).</summary>
        public double LaneLoss { get; set; }

        public string LaneCategory { get; set; }

        public double? SpeedLimitKph { get; set; }

        public double LaneConfidence { get; set; }

        public bool LaneIsEstimate { get; set; }

        /// <summary>Temps immobile retenu pour la catégorie (s).</summary>
        public double StopTime { get; set; }

        public string StopDetail { get; set; }

        public double Total { get; set; }

        /// <summary>Origine dite à la radio : « d'après le circuit : 35 s de voie des stands et environ 25 s d'arrêt ».</summary>
        public string Source { get; set; }
    }
}
