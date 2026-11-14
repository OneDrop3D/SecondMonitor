namespace SecondMonitor.Hub.Core.Track
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Temps perdu par un passage dans la voie des stands, circuit par circuit (Track/Library/pitlane.json) :
    /// entrée, limiteur et sortie comparés au même trajet en piste, SANS l'arrêt lui-même (ce que coûte un drive-through).
    /// Valeurs publiées (Pirelli, FIA, analyses, mesures dans les jeux) ou estimées à partir de la longueur de la voie
    /// (géométrie OpenStreetMap, tools/circuits_stands.py), avec leur vitesse limite, leur confiance et leurs sources.
    /// Sert à l'ingénieur tant qu'aucun arrêt n'a été vu en course (voir Strategy/PitPlanner.cs).
    /// </summary>
    public static class PitLaneLibrary
    {
        private static readonly Lazy<PitLaneData> Data = new(Load);

        public static IReadOnlyList<PitLaneEntry> Circuits => Data.Value.Circuits;

        public static IReadOnlyList<StopTimeEntry> StopTimes => Data.Value.StopTimes;

        public static IReadOnlyList<GamePitRules> Games => Data.Value.Games;

        /// <summary>Le circuit de la bibliothèque des tracés (même identifiant), ou null. Le sens inverse garde la voie des stands.</summary>
        public static PitLaneEntry Get(string trackId)
        {
            if (string.IsNullOrEmpty(trackId))
            {
                return null;
            }

            string id = trackId.EndsWith("-inverse", StringComparison.Ordinal) ? trackId[..^"-inverse".Length] : trackId;
            return Data.Value.Circuits.FirstOrDefault(c => c.Id == id);
        }

        /// <summary>Règles du jeu (catégorie de référence, vitesse limite dans la voie), sinon les règles génériques « * », sinon null.</summary>
        public static GamePitRules Rules(string game) => Data.Value.Games.FirstOrDefault(g => string.Equals(g.Game, game, StringComparison.OrdinalIgnoreCase))
            ?? Data.Value.Games.FirstOrDefault(g => g.Game == "*");

        /// <summary>
        /// Temps immobile estimé pour ce jeu et cette catégorie de voiture : d'abord le couple exact, puis le jeu seul,
        /// puis la catégorie seule, puis la valeur générique (« * »).
        /// </summary>
        public static StopTimeEntry StopTime(string game, string carClass)
        {
            var all = Data.Value.StopTimes;
            bool Is(string value, string wanted) => string.Equals(value, wanted, StringComparison.OrdinalIgnoreCase);
            return all.FirstOrDefault(s => Is(s.Game, game) && Is(s.CarClass, carClass))
                ?? all.FirstOrDefault(s => Is(s.Game, game) && s.CarClass == "*")
                ?? all.FirstOrDefault(s => s.Game == "*" && Is(s.CarClass, carClass))
                ?? all.FirstOrDefault(s => s.Game == "*" && s.CarClass == "*");
        }

        private static PitLaneData Load()
        {
            try
            {
                using Stream stream = typeof(PitLaneLibrary).Assembly.GetManifestResourceStream("SecondMonitor.Hub.Core.Track.pitlane.json");
                if (stream == null)
                {
                    return new PitLaneData();
                }

                return JsonSerializer.Deserialize<PitLaneData>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip }) ?? new PitLaneData();
            }
            catch (Exception)
            {
                return new PitLaneData();
            }
        }
    }

    public sealed class PitLaneData
    {
        [JsonPropertyName("circuits")]
        public List<PitLaneEntry> Circuits { get; set; } = new();

        [JsonPropertyName("stopTimes")]
        public List<StopTimeEntry> StopTimes { get; set; } = new();

        [JsonPropertyName("games")]
        public List<GamePitRules> Games { get; set; } = new();
    }

    /// <summary>Temps perdu dans la voie des stands pour une catégorie (F1, WEC, GT3…) : valeur, conditions, origine.</summary>
    public class PitLaneValue
    {
        /// <summary>Catégorie pour laquelle la valeur vaut (« F1 », « WEC », « GT3 », « IMSA »…).</summary>
        public string Category { get; set; }

        /// <summary>Secondes perdues par le passage dans la voie, sans l'arrêt ; null si rien n'est publié ni estimable.</summary>
        public double? PitLaneLossSeconds { get; set; }

        /// <summary>Longueur de la voie des stands (m), zone limitée si elle est connue, sinon voie complète.</summary>
        public double? PitLaneLength { get; set; }

        /// <summary>Vitesse limite dans la voie (km/h) pour cette valeur.</summary>
        public double? SpeedLimitKph { get; set; }

        /// <summary>0-1 : 0,85-0,95 publié par une source primaire, 0,7-0,8 sources concordantes, 0,5-0,65 mesure isolée, 0,3-0,45 estimation géométrique.</summary>
        public double Confidence { get; set; }

        /// <summary>« publié », « dérivé d'un drive-through », « mesuré par la communauté », « recalculé », « estimé ».</summary>
        public string Method { get; set; }

        public List<string> Sources { get; set; } = new();

        public string Notes { get; set; }

        /// <summary>Valeur estimée (géométrie) plutôt que publiée ou mesurée.</summary>
        [JsonIgnore]
        public bool IsEstimate => Method != null && Method.StartsWith("estim", StringComparison.OrdinalIgnoreCase);
    }

    public sealed class PitLaneEntry : PitLaneValue
    {
        /// <summary>Identifiant du circuit dans circuits.json (TrackLibrary).</summary>
        public string Id { get; set; }

        /// <summary>Nom lisible (pour relire le fichier).</summary>
        public string Name { get; set; }

        /// <summary>Pourquoi il n'y a pas de valeur, quand PitLaneLossSeconds est null.</summary>
        public string Reason { get; set; }

        /// <summary>Géométrie OpenStreetMap : longueur de la voie entre ses deux jonctions avec la piste, et piste contournée (m).</summary>
        public double? OsmPitLength { get; set; }

        public double? OsmBypassedLength { get; set; }

        /// <summary>Valeurs pour d'autres catégories (une F1 et une WEC sur le même circuit, par exemple).</summary>
        public List<PitLaneValue> Variants { get; set; } = new();

        /// <summary>
        /// La valeur pour cette catégorie : la variante de la catégorie si elle existe, sinon la valeur de référence
        /// recalculée pour la vitesse limite demandée (la longueur de la voie le permet), sinon la valeur de référence telle quelle.
        /// Null si le circuit n'a aucune valeur.
        /// </summary>
        public PitLaneValue For(string category, double? speedLimitKph = null)
        {
            var all = new List<PitLaneValue> { this }.Concat(Variants ?? new List<PitLaneValue>()).Where(v => v.PitLaneLossSeconds.HasValue).ToList();
            if (all.Count == 0)
            {
                return null;
            }

            PitLaneValue same = all.Where(v => category != null && string.Equals(v.Category, category, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(v => v.Confidence).FirstOrDefault();
            if (same != null)
            {
                return same;
            }

            // Une autre catégorie : la plus sûre, ramenée à la vitesse limite du jeu quand on connaît la longueur de la voie.
            PitLaneValue best = all.OrderByDescending(v => speedLimitKph.HasValue && v.SpeedLimitKph == speedLimitKph).ThenByDescending(v => v.Confidence).First();
            if (speedLimitKph is double limit && limit > 0 && best.SpeedLimitKph is double from && from > 0 && Math.Abs(limit - from) >= 1 && best.PitLaneLength is double length && length > 0)
            {
                double extra = length * 3.6 * ((1 / limit) - (1 / from));
                return new PitLaneValue
                {
                    Category = category,
                    PitLaneLossSeconds = Math.Round(best.PitLaneLossSeconds.Value + extra, 1),
                    PitLaneLength = length,
                    SpeedLimitKph = limit,
                    Confidence = Math.Round(Math.Max(0.3, best.Confidence - 0.15), 2),
                    Method = "recalculé",
                    Sources = best.Sources,
                    Notes = $"Recalculé de {from:0} à {limit:0} km/h sur {length:0} m de voie, à partir de la valeur {best.Category}.",
                };
            }

            return best;
        }
    }

    /// <summary>Temps immobile à l'arrêt pour un jeu et une catégorie (« * » = tous).</summary>
    public sealed class StopTimeEntry
    {
        public string Game { get; set; } = "*";

        public string CarClass { get; set; } = "*";

        /// <summary>Durée retenue pour un arrêt habituel de la catégorie (secondes).</summary>
        public double Seconds { get; set; }

        /// <summary>Ce que comprend l'arrêt retenu, dit à la radio (« ravitaillement et quatre pneus »).</summary>
        public string Detail { get; set; }

        public List<string> Sources { get; set; } = new();

        public string Notes { get; set; }
    }

    /// <summary>Catégorie de référence et vitesse limite d'un jeu (LMU : WEC à 60 km/h).</summary>
    public sealed class GamePitRules
    {
        /// <summary>Code du jeu (CarContext.Game : « LMU », « F1 », « ACC »…).</summary>
        public string Game { get; set; }

        public string Category { get; set; }

        /// <summary>Vitesse limite appliquée par le jeu sur tous les circuits, null si elle dépend du circuit.</summary>
        public double? SpeedLimitKph { get; set; }

        public List<string> Sources { get; set; } = new();

        public string Notes { get; set; }
    }
}
