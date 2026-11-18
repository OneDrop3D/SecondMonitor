namespace SecondMonitor.Hub.Core.Race
{
    using System.Collections.Generic;

    /// <summary>
    /// Vue « prête à afficher » envoyée à l'interface web (JSON camelCase).
    /// Toutes les durées sont en secondes, distances en mètres, volumes en litres, pressions en kPa, températures en °C.
    /// </summary>
    public sealed class RaceSnapshot
    {
        public long Sequence { get; set; }

        public bool HasData { get; set; }

        public string Source { get; set; }

        public string SessionType { get; set; }

        public string SessionPhase { get; set; }

        public string LengthType { get; set; }

        public double SessionTime { get; set; }

        public double? TimeRemaining { get; set; }

        public int TotalLaps { get; set; }

        public int LeaderLap { get; set; }

        public bool IsMultiClass { get; set; }

        public bool IsMultiplayer { get; set; }

        public List<string> Flags { get; set; } = new();

        public string SafetyCar { get; set; }

        public TrackDto Track { get; set; } = new();

        public WeatherDto Weather { get; set; } = new();

        public PitWindowDto PitWindow { get; set; }

        public List<DriverRow> Drivers { get; set; } = new();

        public PlayerStrategy Player { get; set; }

        public List<string> Capabilities { get; set; } = new();

        /// <summary>Fréquence des données reçues (Hz) : joueur et médiane des adversaires.</summary>
        public DataRateDto DataRate { get; set; } = new();

        /// <summary>Plages de température personnalisées appliquées (« LMU · 963 »), null = valeurs du jeu.</summary>
        public string ThermalProfile { get; set; }

        /// <summary>Accueil : temps / tours restants (estimés en course au temps) et dernier événement de course (voir RaceEngine.Dashboard.cs).</summary>
        public RaceLengthDto RaceLength { get; set; }

        public long LastEventId { get; set; }

        public long EventSessionId { get; set; }
    }

    public sealed class DataRateDto
    {
        public double PlayerHz { get; set; }

        public double OpponentsHz { get; set; }
    }

    public sealed class TrackDto
    {
        public string Name { get; set; }

        public string Layout { get; set; }

        public double Length { get; set; }

        /// <summary>Tracé appris (x, z) échantillonné selon la distance de tour ; null tant qu'il n'est pas connu.</summary>
        public List<double[]> Outline { get; set; }

        public bool WorldPositionsValid { get; set; }

        /// <summary>« jeu » (appris en roulant), « bibliothèque » (tracé réel, avant le premier tour) ou null (cercle).</summary>
        public string OutlineSource { get; set; }

        /// <summary>Nom du tracé de bibliothèque utilisé, et sa source (crédit).</summary>
        public string OutlineCredit { get; set; }

        /// <summary>Tracé de bibliothèque : « calé » (départ et sens vérifiés sur les positions du jeu) ou « estimé ».</summary>
        public string OutlineCalibration { get; set; }
    }

    public sealed class WeatherDto
    {
        public double? Air { get; set; }

        public double? TrackTemp { get; set; }

        public int Rain { get; set; }

        public int Wetness { get; set; }

        /// <summary>Vent (km/h) et direction d'où il vient (°), si le jeu les fournit.</summary>
        public double? Wind { get; set; }

        public double? WindFrom { get; set; }

        /// <summary>Tendance météo calculée par le hub (pas une prévision officielle du jeu).</summary>
        public string Trend { get; set; }
    }

    public sealed class PitWindowDto
    {
        public string State { get; set; }

        public double Start { get; set; }

        public double End { get; set; }
    }

    public sealed class DriverRow
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string ShortName { get; set; }

        public int? Number { get; set; }

        public string Team { get; set; }

        public string Car { get; set; }

        public string ClassId { get; set; }

        public string ClassName { get; set; }

        public int Position { get; set; }

        public int ClassPosition { get; set; }

        public bool IsPlayer { get; set; }

        public bool InPits { get; set; }

        public bool PitRequested { get; set; }

        public string FinishStatus { get; set; }

        public int Laps { get; set; }

        public double LapFraction { get; set; }

        /// <summary>Écart au leader (s). En course : temps réel sur la piste ; en essais/qualif : écart de meilleur tour.</summary>
        public double? GapToLeader { get; set; }

        public int GapToLeaderLaps { get; set; }

        /// <summary>Écart à la voiture devant (s).</summary>
        public double? Interval { get; set; }

        public int IntervalLaps { get; set; }

        /// <summary>Écart au leader de la catégorie.</summary>
        public double? GapToClassLeader { get; set; }

        public int GapToClassLeaderLaps { get; set; }

        /// <summary>Écart au joueur (s, positif = derrière le joueur).</summary>
        public double? GapToPlayer { get; set; }

        public double? LastLap { get; set; }

        public double? BestLap { get; set; }

        public double? CurrentLap { get; set; }

        public bool LastLapIsPersonalBest { get; set; }

        public bool HasOverallBest { get; set; }

        public bool HasClassBest { get; set; }

        public int Sector { get; set; }

        public double?[] LastSectors { get; set; }

        /// <summary>Meilleurs secteurs de la session (S1, S2, S3) et le tour où chacun a été fait.</summary>
        public double?[] BestSectors { get; set; }

        public int?[] BestSectorLaps { get; set; }

        /// <summary>Tour du meilleur temps (affiché « L3/1:27.951 » comme dans SecondMonitor).</summary>
        public int? BestLapNumber { get; set; }

        public double Speed { get; set; }

        public double? X { get; set; }

        public double? Z { get; set; }

        public bool CurrentLapValid { get; set; }

        public TyreSummary Tyres { get; set; } = new();

        public int PitStops { get; set; }

        public int? LastPitLap { get; set; }

        public double? LastPitDuration { get; set; }

        public List<StintDto> Stints { get; set; } = new();

        public double? PaceLast3 { get; set; }

        public double? Degradation { get; set; }

        public PitEstimate NextStop { get; set; }

        public double? FuelLitres { get; set; }

        public int PositionsGained { get; set; }
    }

    public sealed class TyreSummary
    {
        public string Compound { get; set; }

        /// <summary>Usure par roue en % (0 = neuf) FL, FR, RL, RR — null si le jeu ne la fournit pas pour ce pilote.</summary>
        public double[] Wear { get; set; }

        public int StintLaps { get; set; }
    }

    public sealed class StintDto
    {
        public int Index { get; set; }

        public string Compound { get; set; }

        public int StartLap { get; set; }

        public int? EndLap { get; set; }

        public int Laps { get; set; }

        public double? AverageLap { get; set; }

        public double? BestLap { get; set; }

        public bool Current { get; set; }
    }

    public sealed class PitEstimate
    {
        public int Lap { get; set; }

        public int LapsUntil { get; set; }

        /// <summary>high : basé sur ses propres relais ; medium : relais typiques du plateau ; low : basé sur la conso du joueur.</summary>
        public string Confidence { get; set; }

        public string Reason { get; set; }
    }

    public sealed class PlayerStrategy
    {
        public FuelStrategy Fuel { get; set; }

        public EnergyStrategy Energy { get; set; }

        public List<WheelStatus> Wheels { get; set; } = new();

        public double? LapsRemaining { get; set; }

        public int? RecommendedStopLap { get; set; }

        public string RecommendedStopReason { get; set; }

        public double? AverageLap { get; set; }

        public double? Degradation { get; set; }

        public int StintLaps { get; set; }

        public HybridDto Hybrid { get; set; }

        /// <summary>Batterie, énergie virtuelle et essence en direct sur le tour en cours (comparées au tour précédent au même endroit).</summary>
        public SecondMonitor.Hub.Core.Strategy.LiveEnergyDto Live { get; set; }

        public EngineDto Engine { get; set; }

        public List<BattleDto> Battles { get; set; } = new();

        public List<LapDto> RecentLaps { get; set; } = new();

        /// <summary>Delta en direct par rapport au meilleur tour personnel (s, négatif = plus rapide).</summary>
        public double? DeltaBest { get; set; }

        /// <summary>Delta en direct par rapport au tour précédent.</summary>
        public double? DeltaLast { get; set; }

        public double Speed { get; set; }

        /// <summary>Températures des pneus dans la durée (moyenne du tour, surchauffe prolongée).</summary>
        public List<TyreThermalDto> TyreThermal { get; set; } = new();

        /// <summary>Alertes à afficher (pneus trop chauds sur un tour, surchauffe qui dure, nouveau dégât…).</summary>
        public List<string> Alerts { get; set; } = new();

        /// <summary>Dégâts pièce par pièce (0 à 100).</summary>
        public List<DamagePartDto> Damage { get; set; } = new();

        /// <summary>Dernier dégât apparu et, après deux tours, ce qu'il coûte au tour.</summary>
        public DamageEvent DamageEvent { get; set; }

        /// <summary>Simulations d'arrêt : perte au stand, position à la sortie, undercut / overcut.</summary>
        public SecondMonitor.Hub.Core.Strategy.PitPlanDto PitPlan { get; set; }

        /// <summary>Plan de course jusqu'au drapeau : arrêts restants, fenêtre du prochain, ±1 tour, ratio, un arrêt de moins.</summary>
        public SecondMonitor.Hub.Core.Strategy.RacePlanDto Plan { get; set; }

        /// <summary>Réglages en roulant (répartition, TC, ABS, cartographie…), hybride, limites de piste et pénalités, si le jeu les donne.</summary>
        public ControlsDto Controls { get; set; }

        /// <summary>Pédales et volant à l'instant.</summary>
        public InputsDto Inputs { get; set; }
    }

    public sealed class FuelStrategy
    {
        public double Remaining { get; set; }

        public double Capacity { get; set; }

        public double? PerLap { get; set; }

        public double? LapsLeft { get; set; }

        /// <summary>Carburant nécessaire pour finir (marge incluse) moins le carburant à bord ; ≤ 0 = assez.</summary>
        public double? ToAdd { get; set; }

        public int? StopsNeeded { get; set; }

        /// <summary>Quantité à mettre au prochain arrêt (plafonnée au plein).</summary>
        public double? NextStopAdd { get; set; }

        /// <summary>Consommation au tour estimée sur le tour en cours (aucun tour bouclé utilisable pour l'instant).</summary>
        public bool PerLapEstimated { get; set; }

        public string Status { get; set; }
    }

    public sealed class EnergyStrategy
    {
        public double RemainingPct { get; set; }

        public double? PerLapPct { get; set; }

        public double? LapsLeft { get; set; }

        public double? ToAddPct { get; set; }

        /// <summary>Énergie à recharger au prochain arrêt (plafonnée à 100 %).</summary>
        public double? NextStopAddPct { get; set; }

        /// <summary>Consommation au tour estimée sur le tour en cours (aucun tour bouclé utilisable pour l'instant).</summary>
        public bool PerLapEstimated { get; set; }

        public string Status { get; set; }
    }

    public sealed class WheelStatus
    {
        public string Position { get; set; }

        public string Compound { get; set; }

        public double WearPct { get; set; }

        public double? WearPerLapPct { get; set; }

        public double? LapsToHeavyWear { get; set; }

        public double HeavyWearLimitPct { get; set; }

        public double? Pressure { get; set; }

        public double? IdealPressure { get; set; }

        /// <summary>Côté gauche du pneu (vu de la voiture) — intérieur pour une roue droite, extérieur pour une roue gauche.</summary>
        public double? TempInner { get; set; }

        public double? TempMiddle { get; set; }

        /// <summary>Côté droit du pneu (vu de la voiture).</summary>
        public double? TempOuter { get; set; }

        public double? Core { get; set; }

        public double? IdealTemp { get; set; }

        /// <summary>Demi-largeur de la plage idéale de température des pneus (°C).</summary>
        public double? TempWindow { get; set; }

        /// <summary>Plage idéale de la carcasse (par défaut celle de la surface).</summary>
        public double? CoreIdeal { get; set; }

        public double? CoreWindow { get; set; }

        public double? Brake { get; set; }

        public double? BrakeIdeal { get; set; }

        public double? BrakeWindow { get; set; }
    }

    /// <summary>Moteur et état de la voiture du joueur.</summary>
    public sealed class EngineDto
    {
        public double? WaterTemp { get; set; }

        public double? WaterIdeal { get; set; }

        public double? WaterWindow { get; set; }

        public double? OilTemp { get; set; }

        public double? OilIdeal { get; set; }

        public double? OilWindow { get; set; }

        /// <summary>Pression d'huile (kPa).</summary>
        public double? OilPressure { get; set; }

        public int Rpm { get; set; }

        public string Gear { get; set; }

        /// <summary>Dégâts 0–100 % (null si le jeu ne les fournit pas).</summary>
        public double? DamageEngine { get; set; }

        public double? DamageTransmission { get; set; }

        public double? DamageSuspension { get; set; }

        public double? DamageBodywork { get; set; }
    }

    /// <summary>Duel avec la voiture devant / derrière dans la catégorie, et projection du rapprochement.</summary>
    public sealed class BattleDto
    {
        public string Id { get; set; }

        /// <summary>« ahead » ou « behind ».</summary>
        public string Side { get; set; }

        public string Name { get; set; }

        public int? Number { get; set; }

        /// <summary>Écart actuel (s, toujours positif).</summary>
        public double? Gap { get; set; }

        /// <summary>Secondes par tour gagnées par le joueur (négatif = le joueur perd du temps).</summary>
        public double? PaceDelta { get; set; }

        /// <summary>Tours avant le contact (écart &lt; 1 s) si les rythmes se rapprochent ; null sinon.</summary>
        public double? LapsToContact { get; set; }

        public string Compound { get; set; }

        public int TyreLaps { get; set; }

        public int? NextStopLap { get; set; }
    }

    public sealed class HybridDto
    {
        public double ChargePct { get; set; }

        public string Mode { get; set; }

        // ── Suivi tour par tour (HybridTracker) : conseils batterie de l'ingénieur ──

        /// <summary>« deploy » (la batterie se vide), « harvest » (elle se recharge) ou null.</summary>
        public string ChargeChange { get; set; }

        /// <summary>Énergie déployée / récupérée sur le tour en cours (% de la limite par tour, F1 uniquement).</summary>
        public double? DeployedLapPct { get; set; }

        public double? HarvestedLapPct { get; set; }

        /// <summary>Dernier tour complet : charge au début, à la fin, minimum, et distance (m) où la batterie a été vide.</summary>
        public double? LastLapStartPct { get; set; }

        public double? LastLapEndPct { get; set; }

        public double? LastLapMinPct { get; set; }

        public double? LastLapEmptyAt { get; set; }

        /// <summary>« F1 » (ERS, modes de déploiement), « LMU » (hypercar : batterie + énergie virtuelle) ou « autre ».</summary>
        public string System { get; set; }
    }

    public sealed class LapDto
    {
        public int Lap { get; set; }

        public double? Time { get; set; }

        public double? Fuel { get; set; }

        public double? Energy { get; set; }

        public bool Pit { get; set; }

        public bool Valid { get; set; }

        /// <summary>Usure prise sur ce tour (points de %) FL, FR, RL, RR.</summary>
        public double[] WearDelta { get; set; }

        public string Compound { get; set; }

        /// <summary>Relais (1 = premier) dans lequel le tour a été fait.</summary>
        public int? Stint { get; set; }
    }
}
