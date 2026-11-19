namespace SecondMonitor.Hub.Core.Ai
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.Json;

    /// <summary>
    /// Plan préparé avant la course (écran « Préparer la course ») : arrêts prévus et valeurs de départ.
    /// Pendant une course sur le même circuit, il sert d'arrêt par défaut tant que le pilote n'a rien décidé à la radio.
    /// </summary>
    public sealed class PreparedPlan
    {
        /// <summary>Circuit visé (nom du jeu) ; vide : n'importe quel circuit.</summary>
        public string Track { get; set; }

        public string Car { get; set; }

        public DateTime SavedAt { get; set; }

        public double? Laps { get; set; }

        public double? LapTime { get; set; }

        public double? FuelPerLap { get; set; }

        public double? EnergyPerLap { get; set; }

        public List<PreparedStop> Stops { get; set; } = new();

        public bool Matches(string track) =>
            string.IsNullOrWhiteSpace(Track) || string.Equals(Track.Trim(), track?.Trim(), StringComparison.OrdinalIgnoreCase);

        /// <summary>Prochain arrêt prévu après <paramref name="lapsDone"/> tours bouclés, sur ce circuit.</summary>
        public int? NextStop(string track, int lapsDone) =>
            Matches(track) ? Stops.Select(s => s.Lap).Where(l => l > lapsDone).OrderBy(l => l).Cast<int?>().FirstOrDefault() : null;

        /// <summary>Ligne de la fiche de faits de l'IA.</summary>
        public string Fact()
        {
            static string N(double v, int d) => v.ToString("F" + d, CultureInfo.GetCultureInfo("fr-FR"));
            string stops = Stops.Count == 0
                ? "aucun arrêt"
                : "arrêts " + string.Join(", ", Stops.OrderBy(s => s.Lap).Select(s =>
                    $"tour {s.Lap} ({(s.Fuel is double f ? $"{N(f, 1)} L" : "?")}{(s.EnergyPct is double e ? $", {N(e, 0)} % d'énergie" : string.Empty)}, {(s.Tyres ? "pneus" : "sans pneus")})"));
            string conso = FuelPerLap is double fpl ? $" ; prévu {N(fpl, 2)} L/tour" + (EnergyPerLap is double epl ? $" et {N(epl, 1)} % d'énergie/tour" : string.Empty) : string.Empty;
            return $"PLAN PRÉPARÉ AVANT LA COURSE par le pilote : {stops}{conso}. Une décision prise à la radio le remplace.";
        }

        /// <summary>Plan reçu de l'interface : bornes de bon sens, null s'il n'y a rien d'utilisable.</summary>
        public static PreparedPlan Sanitize(PreparedPlan p)
        {
            if (p == null)
            {
                return null;
            }

            static double? Pos(double? v) => v is double d && double.IsFinite(d) && d > 0 && d < 100000 ? d : null;
            static string Txt(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim()[..Math.Min(s.Trim().Length, 120)];
            return new PreparedPlan
            {
                Track = Txt(p.Track),
                Car = Txt(p.Car),
                SavedAt = DateTime.Now,
                Laps = Pos(p.Laps),
                LapTime = Pos(p.LapTime),
                FuelPerLap = Pos(p.FuelPerLap),
                EnergyPerLap = Pos(p.EnergyPerLap),
                Stops = (p.Stops ?? new()).Where(s => s != null && s.Lap is > 0 and < 10000).GroupBy(s => s.Lap).Select(g => g.First())
                    .OrderBy(s => s.Lap).Take(30)
                    .Select(s => new PreparedStop { Lap = s.Lap, Fuel = Pos(s.Fuel), EnergyPct = Pos(s.EnergyPct), Tyres = s.Tyres }).ToList(),
            };
        }

        /// <summary>Fichier preparation.json du dossier de données (écriture atomique, fichier abîmé ignoré).</summary>
        public static PreparedPlan Load(string path)
        {
            try
            {
                return File.Exists(path) ? Sanitize(JsonSerializer.Deserialize<PreparedPlan>(File.ReadAllText(path), Json)) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void Save(string path, PreparedPlan plan)
        {
            if (plan == null)
            {
                File.Delete(path);
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(plan, Json));
            File.Move(tmp, path, overwrite: true);
        }

        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    }

    public sealed class PreparedStop
    {
        public int Lap { get; set; }

        /// <summary>Essence à remettre (L).</summary>
        public double? Fuel { get; set; }

        /// <summary>Énergie virtuelle à remettre (%).</summary>
        public double? EnergyPct { get; set; }

        public bool Tyres { get; set; }
    }
}
