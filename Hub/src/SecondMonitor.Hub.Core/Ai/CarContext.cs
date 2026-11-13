namespace SecondMonitor.Hub.Core.Ai
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SecondMonitor.Hub.Core.Race;

    /// <summary>
    /// Jeu, voiture et conditions du moment : ce qui est réglable en roulant (répartition, ABS, TC, cartographie, ERS…),
    /// pluie, pneus neufs ou usés, froids ou trop chauds. Sert à adapter les conseils de réglage.
    /// </summary>
    public sealed class CarContext
    {
        /// <summary>« ACC », « LMU », « F1 », « AMS2 », « AC », « rF2 », « R3E », « Demo », « autre ».</summary>
        public string Game { get; set; } = "autre";

        /// <summary>« GT3 », « GT4 », « Hypercar », « LMP2 », « F1 », « F2 », « autre ».</summary>
        public string CarClass { get; set; } = "autre";

        public string CarName { get; set; }

        public bool HasAbs { get; set; }

        public bool HasTc { get; set; }

        public bool HasEngineMap { get; set; }

        /// <summary>F1 : différentiel à l'accélération réglable en roulant.</summary>
        public bool HasDiffInRace { get; set; }

        public bool IsHybrid { get; set; }

        public bool Wet { get; set; }

        public int WetnessPct { get; set; }

        public int RainPct { get; set; }

        public int StintLaps { get; set; }

        public double? WorstWearPct { get; set; }

        public string WorstWearWheel { get; set; }

        public bool TyresCold { get; set; }

        /// <summary>Roues les plus chaudes au-dessus de la plage (avant / arrière) et leur température.</summary>
        public bool FrontHot { get; set; }

        public bool RearHot { get; set; }

        public double? HottestTemp { get; set; }

        public string SessionType { get; set; }

        public static CarContext From(RaceSnapshot snap, int wetThresholdPct = 25)
        {
            var c = new CarContext();
            if (snap == null)
            {
                return c;
            }

            DriverRow me = snap.Drivers?.FirstOrDefault(d => d.IsPlayer);
            string source = snap.Source ?? string.Empty;
            string cls = ((me?.ClassName ?? string.Empty) + " " + (me?.ClassId ?? string.Empty)).ToLowerInvariant();
            string car = (me?.Car ?? string.Empty).ToLowerInvariant();
            c.CarName = me?.Car;
            c.SessionType = snap.SessionType;
            c.Game = source.StartsWith("ACC", StringComparison.OrdinalIgnoreCase) ? "ACC"
                : source.Contains("LM Ultimate", StringComparison.OrdinalIgnoreCase) ? "LMU"
                : source.StartsWith("F1", StringComparison.OrdinalIgnoreCase) ? "F1"
                : source.Contains("AMS 2", StringComparison.OrdinalIgnoreCase) ? "AMS2"
                : source.Contains("Assetto", StringComparison.OrdinalIgnoreCase) ? "AC"
                : source.Contains("RFactor", StringComparison.OrdinalIgnoreCase) ? "rF2"
                : source.Contains("R3E", StringComparison.OrdinalIgnoreCase) ? "R3E"
                : source.Equals("Demo", StringComparison.OrdinalIgnoreCase) ? "Demo"
                : "autre";

            c.CarClass = cls.Contains("hyper") || car.Contains("963") || car.Contains("499p") || car.Contains("gr010") || car.Contains("9x8") ? "Hypercar"
                : cls.Contains("lmp2") || cls.Contains("p2") ? "LMP2"
                : cls.Contains("gt3") || cls.Contains("lmgt3") ? "GT3"
                : cls.Contains("gt4") ? "GT4"
                : c.Game == "F1" && (cls.Contains("f2") || car.Contains("f2")) ? "F2"
                : c.Game == "F1" ? "F1"
                : c.Game == "ACC" ? "GT3"
                : "autre";

            bool proto = c.CarClass is "Hypercar" or "LMP2" or "F1" or "F2";
            c.HasAbs = !proto;
            c.HasTc = c.CarClass != "F1" && c.CarClass != "F2";
            c.HasEngineMap = c.Game is "ACC" or "LMU" or "rF2" or "AMS2" or "Demo" || c.CarClass is "GT3" or "GT4";
            c.HasDiffInRace = c.Game == "F1";
            c.IsHybrid = snap.Player?.Hybrid != null || c.CarClass == "Hypercar" || c.CarClass == "F1";

            c.WetnessPct = snap.Weather?.Wetness ?? 0;
            c.RainPct = snap.Weather?.Rain ?? 0;
            c.Wet = c.WetnessPct >= Math.Max(5, wetThresholdPct / 2) || c.RainPct >= 20;

            PlayerStrategy p = snap.Player;
            c.StintLaps = p?.StintLaps ?? me?.Tyres?.StintLaps ?? 0;
            List<WheelStatus> wheels = p?.Wheels ?? new List<WheelStatus>();
            if (wheels.Count > 0)
            {
                WheelStatus worst = wheels.OrderByDescending(w => w.WearPct).First();
                c.WorstWearPct = worst.WearPct;
                c.WorstWearWheel = worst.Position;
                c.TyresCold = wheels.Any(w => w.TempMiddle.HasValue && w.IdealTemp.HasValue && w.TempMiddle < w.IdealTemp - Math.Max(10, w.TempWindow ?? 0));
                var hot = wheels.Where(w => w.TempMiddle.HasValue && w.IdealTemp.HasValue && w.TempMiddle > w.IdealTemp + Math.Max(8, w.TempWindow ?? 0)).ToList();
                c.FrontHot = hot.Any(w => w.Position is "FL" or "FR");
                c.RearHot = hot.Any(w => w.Position is "RL" or "RR");
                c.HottestTemp = hot.Select(w => w.TempMiddle).DefaultIfEmpty(null).Max();
            }

            return c;
        }

        public bool NewTyres => StintLaps <= 1;

        public bool WornTyres => WorstWearPct >= 55;

        /// <summary>Ce qui se règle en roulant sur cette voiture (pour la fiche de faits et les conseils).</summary>
        public string InRaceControls()
        {
            var list = new List<string> { "répartition de freinage" };
            if (HasAbs)
            {
                list.Add("ABS");
            }

            if (HasTc)
            {
                list.Add("TC");
            }

            if (HasEngineMap)
            {
                list.Add("cartographie moteur");
            }

            if (HasDiffInRace)
            {
                list.Add("différentiel à l'accélération");
            }

            if (Game == "F1" && CarClass == "F1")
            {
                list.Add("mode de déploiement ERS");
            }
            else if (CarClass == "Hypercar")
            {
                list.Add("récupération d'énergie (regen) selon la voiture");
            }

            return string.Join(", ", list);
        }

        public string Describe() =>
            $"Réglable en roulant sur cette voiture ({(CarClass == "autre" ? CarName ?? "voiture inconnue" : CarClass)}, {Game}) : {InRaceControls()}. " +
            "Au stand seulement : barres antiroulis, ressorts, amortisseurs, hauteurs, carrossage, pressions des pneus, ailerons, différentiel (précharge), écopes de freins.";
    }
}
