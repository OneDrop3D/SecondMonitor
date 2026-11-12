using SecondMonitor.Hub.Core.Race;

namespace SecondMonitor.Hub;

/// <summary>
/// Données de course compactes pour les petits écrans et les boîtiers (ClickDeck…) : GET /api/deck.
/// Tout est déjà calculé et arrondi ; un champ absent vaut null. Contrat stable : ne retirer aucun champ.
/// </summary>
public static class DeckApi
{
    /// <summary>État de la radio sur le poste qui l'utilise (« idle », « recording », « thinking », « speaking »).</summary>
    public static string RadioPhase { get; set; } = "idle";

    public static object Build(RaceSnapshot s, string? lastRadio)
    {
        if (s == null || !s.HasData)
        {
            return new { ok = true, hasData = false, game = s?.Source, radio = new { phase = RadioPhase, last = lastRadio } };
        }

        DriverRow? me = s.Drivers.FirstOrDefault(d => d.IsPlayer);
        PlayerStrategy? p = s.Player;
        var cls = me == null ? s.Drivers : s.Drivers.Where(d => d.ClassId == me.ClassId).OrderBy(d => d.ClassPosition).ToList();
        int idx = me == null ? -1 : cls.FindIndex(d => d.Id == me.Id);
        DriverRow? ahead = idx > 0 ? cls[idx - 1] : null;
        DriverRow? behind = idx >= 0 && idx + 1 < cls.Count ? cls[idx + 1] : null;
        bool race = s.SessionType == "Race";

        // Classement autour de vous : 10 lignes au plus, vous au milieu quand c'est possible.
        int from = Math.Max(0, Math.Min(Math.Max(0, idx - 4), cls.Count - 10));
        var standings = cls.Skip(from).Take(10).Select(d => new
        {
            pos = s.IsMultiClass ? d.ClassPosition : d.Position,
            num = d.Number,
            name = string.IsNullOrWhiteSpace(d.ShortName) ? d.Name : d.ShortName,
            gap = race ? (d.GapToClassLeaderLaps > 0 ? $"+{d.GapToClassLeaderLaps} T" : d.GapToClassLeader is double g && g > 0.0005 ? $"+{g:0.0}" : "—")
                       : d.BestLap is double b && b > 0 ? Lap(b) : "—",
            me = d.IsPlayer,
            pit = d.InPits,
        }).ToList();

        return new
        {
            ok = true,
            hasData = true,
            game = s.Source,
            session = s.SessionType switch { "Race" => "Course", "Qualification" => "Qualification", "Practice" => "Essais", "WarmUp" => "Warm-up", _ => "Session" },
            track = s.Track.Name,
            position = me == null ? null : new
            {
                @class = me.ClassPosition,
                classCount = cls.Count,
                overall = me.Position,
                overallCount = s.Drivers.Count,
                className = me.ClassName,
                gained = me.PositionsGained,
            },
            laps = me == null ? null : new { done = me.Laps, remaining = Round(p?.LapsRemaining, 1), total = s.TotalLaps > 0 ? s.TotalLaps : (int?)null },
            timeRemaining = Round(s.TimeRemaining, 0),
            lastLap = Round(me?.LastLap, 3),
            bestLap = Round(me?.BestLap, 3),
            currentLap = Round(me?.CurrentLap, 1),
            deltaBest = Round(p?.DeltaBest, 2),
            deltaLast = Round(p?.DeltaLast, 2),
            gapAhead = ahead == null ? null : new { number = ahead.Number, name = ahead.Name, gap = Round(-ahead.GapToPlayer, 1) },
            gapBehind = behind == null ? null : new { number = behind.Number, name = behind.Name, gap = Round(behind.GapToPlayer, 1) },
            fuel = p?.Fuel == null ? null : new { litres = Round(p.Fuel.Remaining, 1), laps = Round(p.Fuel.LapsLeft, 1), perLap = Round(p.Fuel.PerLap, 2), toAdd = Round(p.Fuel.ToAdd, 1), status = p.Fuel.Status },
            energy = p?.Energy == null ? null : new { pct = Round(p.Energy.RemainingPct, 0), laps = Round(p.Energy.LapsLeft, 1) },
            tyres = p?.Wheels.Select(w =>
            {
                TyreThermalDto? t = p.TyreThermal?.FirstOrDefault(x => x.Position == w.Position);
                return new
                {
                    pos = w.Position,
                    temp = Round(t?.Surface ?? w.TempMiddle, 0),
                    core = Round(w.Core, 0),
                    wear = Round(w.WearPct, 0),
                    pressure = Round(w.Pressure, 0),
                    brake = Round(w.Brake, 0),
                    state = t?.State,
                };
            }).ToList(),
            compound = me?.Tyres.Compound,
            stintLaps = me?.Tyres.StintLaps,
            stop = p?.RecommendedStopLap is int lap ? new { lap, reason = p.RecommendedStopReason } : null,
            weather = new { air = Round(s.Weather.Air, 0), track = Round(s.Weather.TrackTemp, 0), rain = s.Weather.Rain, wetness = s.Weather.Wetness, trend = s.Weather.Trend },
            standings,
            alerts = p?.Alerts ?? new List<string>(),
            damage = p?.Damage?.Where(d => d.Percent >= 1).Select(d => new { part = d.Label, pct = Round(d.Percent, 0) }).ToList(),
            radio = new { phase = RadioPhase, last = lastRadio },
        };
    }

    private static double? Round(double? v, int digits) => v is double d && double.IsFinite(d) ? Math.Round(d, digits) : null;

    private static string Lap(double s)
    {
        int m = (int)(s / 60);
        return m > 0 ? $"{m}:{s - (m * 60):00.000}" : $"{s:0.000}";
    }
}
