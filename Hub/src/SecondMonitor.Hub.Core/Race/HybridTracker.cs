namespace SecondMonitor.Hub.Core.Race
{
    using System;
    using SecondMonitor.DataModel.Snapshot.Drivers;
    using SecondMonitor.DataModel.Snapshot.Systems;

    /// <summary>
    /// Batterie des voitures hybrides (F1 : ERS ; LMU : hypercars) suivie tour par tour : charge au début et à la fin du tour,
    /// minimum, endroit où elle a été vide, et si elle se vide ou se recharge en ce moment. Sert aux conseils de l'ingénieur.
    /// </summary>
    public sealed class HybridTracker
    {
        private int _lap = -1;
        private double? _prev;
        private double? _start;
        private double _min = double.MaxValue;
        private double? _emptyAt;
        private string _change;
        private double _changeAt = double.NegativeInfinity;
        private double _clock;
        private bool _seen;

        public double? LastStart { get; private set; }

        public double? LastEnd { get; private set; }

        public double? LastMin { get; private set; }

        public double? LastEmptyAt { get; private set; }

        public void Reset()
        {
            _lap = -1;
            _prev = null;
            _start = null;
            _min = double.MaxValue;
            _emptyAt = null;
            _change = null;
            _changeAt = double.NegativeInfinity;
            _seen = false;
            LastStart = LastEnd = LastMin = LastEmptyAt = null;
        }

        /// <summary>À chaque paquet du joueur. <paramref name="now"/> en secondes (horloge quelconque, croissante).</summary>
        public void Update(DriverInfo player, double now)
        {
            HybridSystem hs = player?.CarInfo?.HybridSystem;
            if (hs == null || !HasHybrid(hs))
            {
                return;
            }

            _seen = true;
            _clock = now;
            double pct = Math.Clamp(hs.RemainingChargePercentage, 0, 1) * 100;
            if (player.CompletedLaps != _lap)
            {
                if (_lap >= 0 && player.CompletedLaps == _lap + 1 && _start.HasValue)
                {
                    LastStart = _start;
                    LastEnd = pct;
                    LastMin = Math.Min(_min, pct);
                    LastEmptyAt = _emptyAt;
                }

                _lap = player.CompletedLaps;
                _start = pct;
                _min = pct;
                _emptyAt = null;
            }

            _min = Math.Min(_min, pct);
            if (pct < 3 && _emptyAt == null)
            {
                _emptyAt = Math.Round(player.LapDistance, 0);
            }

            // Sens de variation : donné par le jeu (LMU) ou déduit de l'évolution de la charge (F1).
            string change = hs.HybridChargeChange switch
            {
                HybridChargeChange.Discharge => "deploy",
                HybridChargeChange.Recharge => "harvest",
                _ => null,
            };
            if (change == null && _prev.HasValue && Math.Abs(pct - _prev.Value) > 0.01)
            {
                change = pct < _prev ? "deploy" : "harvest";
            }

            if (change != null)
            {
                _change = change;
                _changeAt = now;
            }

            _prev = pct;
        }

        /// <summary>Complète la batterie de l'instantané (crée l'objet si le jeu a donné une batterie, même vide).</summary>
        public HybridDto Fill(HybridDto dto, HybridSystem hs, string source)
        {
            if (hs == null || (!_seen && !HasHybrid(hs)))
            {
                return dto;
            }

            dto ??= new HybridDto { ChargePct = hs.RemainingChargePercentage, Mode = hs.HybridSystemMode.ToString() };
            dto.ChargeChange = _clock - _changeAt <= 1.5 ? _change : null;
            dto.System = (source ?? string.Empty).StartsWith("F1", StringComparison.OrdinalIgnoreCase) ? "F1"
                : (source ?? string.Empty).Contains("LM Ultimate", StringComparison.OrdinalIgnoreCase) || (source ?? string.Empty).Contains("rFactor", StringComparison.OrdinalIgnoreCase) ? "LMU"
                : "autre";
            if (dto.System == "F1")
            {
                dto.DeployedLapPct = hs.RemainingPerLapChargePercentage is double d ? Math.Round(d * 100, 1) : null;
                dto.HarvestedLapPct = hs.HarvestedThisLapPercentage is double h ? Math.Round(h * 100, 1) : null;
            }

            dto.LastLapStartPct = Round(LastStart);
            dto.LastLapEndPct = Round(LastEnd);
            dto.LastLapMinPct = Round(LastMin);
            dto.LastLapEmptyAt = LastEmptyAt;
            return dto;
        }

        private static double? Round(double? v) => v.HasValue ? Math.Round(v.Value, 1) : null;

        private static bool HasHybrid(HybridSystem hs) =>
            hs.RemainingChargePercentage > 0 || hs.HybridSystemMode != HybridSystemMode.UnAvailable || hs.HybridChargeChange != HybridChargeChange.Unavailable;
    }
}
