// Miroir des DTO C# (SecondMonitor.Hub.Core.Race.RaceSnapshot). Durées en s, distances en m, litres, kPa, °C.
import type { AudioStation, VoiceOutput } from './stations';

export interface RaceSnapshot {
  sequence: number;
  hasData: boolean;
  source: string | null;
  sessionType: string | null;
  sessionPhase: string | null;
  lengthType: string | null;
  sessionTime: number;
  timeRemaining: number | null;
  totalLaps: number;
  leaderLap: number;
  isMultiClass: boolean;
  isMultiplayer: boolean;
  flags: string[];
  safetyCar: string | null;
  track: TrackDto;
  weather: WeatherDto;
  pitWindow: { state: string; start: number; end: number } | null;
  drivers: DriverRow[];
  player: PlayerStrategy | null;
  capabilities: string[];
  dataRate: { playerHz: number; opponentsHz: number };
  thermalProfile: string | null;
  /** Accueil : temps / tours restants et dernier événement de course (RaceEngine.Dashboard.cs). */
  raceLength?: RaceLength | null;
  lastEventId?: number;
  eventSessionId?: number;
}

export interface TrackDto {
  name: string | null;
  layout: string | null;
  length: number;
  outline: [number, number][] | null;
  worldPositionsValid: boolean;
  outlineSource: 'jeu' | 'bibliothèque' | null;
  outlineCredit: string | null;
  outlineCalibration?: 'calé' | 'estimé' | null;
}

export interface WeatherDto {
  air: number | null;
  trackTemp: number | null;
  rain: number;
  wetness: number;
  wind: number | null;
  windFrom: number | null;
  trend: string | null;
}

export interface DriverRow {
  id: string;
  name: string;
  shortName: string;
  number: number | null;
  team: string | null;
  car: string | null;
  classId: string;
  className: string | null;
  position: number;
  classPosition: number;
  isPlayer: boolean;
  inPits: boolean;
  pitRequested: boolean;
  finishStatus: string;
  laps: number;
  lapFraction: number;
  gapToLeader: number | null;
  gapToLeaderLaps: number;
  interval: number | null;
  intervalLaps: number;
  gapToClassLeader: number | null;
  gapToClassLeaderLaps: number;
  gapToPlayer: number | null;
  lastLap: number | null;
  bestLap: number | null;
  currentLap: number | null;
  lastLapIsPersonalBest: boolean;
  hasOverallBest: boolean;
  hasClassBest: boolean;
  sector: number;
  lastSectors: (number | null)[];
  bestSectors: (number | null)[] | null;
  bestSectorLaps: (number | null)[] | null;
  bestLapNumber: number | null;
  speed: number;
  x: number | null;
  z: number | null;
  currentLapValid: boolean;
  tyres: { compound: string | null; wear: number[] | null; stintLaps: number };
  pitStops: number;
  lastPitLap: number | null;
  lastPitDuration: number | null;
  stints: StintDto[];
  paceLast3: number | null;
  degradation: number | null;
  nextStop: PitEstimate | null;
  fuelLitres: number | null;
  positionsGained: number;
}

export interface StintDto {
  index: number;
  compound: string | null;
  startLap: number;
  endLap: number | null;
  laps: number;
  averageLap: number | null;
  bestLap: number | null;
  current: boolean;
}

export interface PitEstimate {
  lap: number;
  lapsUntil: number;
  confidence: 'high' | 'medium' | 'low';
  reason: string;
}

export interface PlayerStrategy {
  fuel: FuelStrategy | null;
  energy: EnergyStrategy | null;
  wheels: WheelStatus[];
  lapsRemaining: number | null;
  recommendedStopLap: number | null;
  recommendedStopReason: string | null;
  averageLap: number | null;
  degradation: number | null;
  stintLaps: number;
  hybrid: { chargePct: number; mode: string } | null;
  engine: EngineDto | null;
  battles: BattleDto[];
  recentLaps: PlayerLap[];
  tyreThermal: TyreThermal[];
  alerts: string[];
  damage: DamagePart[];
  damageEvent: { key: string; label: string; percent: number; lap: number; lossPerLap: number | null } | null;
  pitPlan: PitPlan | null;
  deltaBest: number | null;
  deltaLast: number | null;
  speed: number;
  /** Réglages en roulant, hybride, limites de piste et pénalités (si le jeu les donne). */
  controls?: CarControls | null;
  /** Pédales et volant à l'instant (%, volant de -100 à 100). */
  inputs?: DriverInputs | null;
}

export interface CarControls {
  frontBrakeBiasPct: number | null;
  motorMap: number | null; motorMapMax: number | null;
  tc: number | null; tcMax: number | null;
  tcCut: number | null; tcCutMax: number | null;
  tcSlip: number | null; tcSlipMax: number | null;
  abs: number | null; absMax: number | null;
  migration: number | null; migrationMax: number | null;
  frontAntiRoll: number | null; frontAntiRollMax: number | null;
  rearAntiRoll: number | null; rearAntiRollMax: number | null;
  regenKw: number | null;
  electricMotor: string | null;
  batteryPct: number | null;
  virtualEnergyPct: number | null;
  trackLimitsPoints: number | null;
  trackLimitsPointsPerPenalty: number | null;
  penalties: number | null;
  lapInvalidated: boolean;
  engineRunning: boolean;
  pitRequested: boolean;
}

export interface DriverInputs { throttle: number; brake: number; clutch: number; steering: number; gear: string; rpm: number }

/** Trace pédales / volant d'un tour (GET /api/analysis/inputs?lap=last|best|current|n), pas de `step` mètres. */
export interface InputTrace {
  lap: number;
  time: number | null;
  step: number;
  throttle: (number | null)[];
  brake: (number | null)[];
  steering: (number | null)[];
  speed: (number | null)[];
  laps: number[];
}

export interface DamagePart {
  key: string;
  label: string;
  group: 'aero' | 'engine' | 'gearbox' | 'suspension' | 'brakes' | 'tyres' | 'electronics';
  percent: number;
}

export interface PitPlan {
  pitLoss: number;
  pitLossSource: string;
  freshTyreGain: number | null;
  freshTyreGainSource: string;
  warmupPenalty: number;
  rejoinNow: { position: number; aheadNumber: number | null; aheadName: string | null; aheadGap: number | null; behindNumber: number | null; behindName: string | null; behindGap: number | null; traffic: number; text: string } | null;
  cuts: { kind: 'undercut' | 'overcut' | 'menace-undercut'; rivalId: string; number: number | null; name: string | null; gap: number; gainPerLap: number | null; lapsNeeded: number | null; feasible: boolean; text: string }[];
}

export interface TyreThermal {
  position: 'FL' | 'FR' | 'RL' | 'RR';
  surface: number | null;
  currentLapAverage: number | null;
  lastLapAverage: number | null;
  lastLapCoreAverage: number | null;
  min: number | null;
  max: number | null;
  hotSeconds: number;
  state: 'froid' | 'bon' | 'chaud' | null;
  lastLapState: 'froid' | 'bon' | 'chaud' | null;
}

export interface PlayerLap {
  lap: number;
  time: number | null;
  fuel: number | null;
  energy: number | null;
  pit: boolean;
  valid: boolean;
  wearDelta: number[] | null;
}

export interface EngineDto {
  waterTemp: number | null;
  waterIdeal: number | null;
  waterWindow: number | null;
  oilTemp: number | null;
  oilIdeal: number | null;
  oilWindow: number | null;
  oilPressure: number | null;
  rpm: number;
  gear: string | null;
  damageEngine: number | null;
  damageTransmission: number | null;
  damageSuspension: number | null;
  damageBodywork: number | null;
}

export interface BattleDto {
  id: string;
  side: 'ahead' | 'behind';
  name: string;
  number: number | null;
  gap: number | null;
  paceDelta: number | null;
  lapsToContact: number | null;
  compound: string | null;
  tyreLaps: number;
  nextStopLap: number | null;
}

export type StrategyStatus = 'ok' | 'short' | 'critical' | 'learning' | null;

export interface FuelStrategy {
  remaining: number;
  capacity: number;
  perLap: number | null;
  lapsLeft: number | null;
  toAdd: number | null;
  stopsNeeded: number | null;
  nextStopAdd: number | null;
  status: StrategyStatus;
}

export interface EnergyStrategy {
  remainingPct: number;
  perLapPct: number | null;
  lapsLeft: number | null;
  toAddPct: number | null;
  nextStopAddPct: number | null;
  status: StrategyStatus;
}

export interface WheelStatus {
  position: 'FL' | 'FR' | 'RL' | 'RR';
  compound: string | null;
  wearPct: number;
  wearPerLapPct: number | null;
  lapsToHeavyWear: number | null;
  heavyWearLimitPct: number;
  pressure: number | null;
  idealPressure: number | null;
  tempInner: number | null;
  tempMiddle: number | null;
  tempOuter: number | null;
  core: number | null;
  idealTemp: number | null;
  tempWindow: number | null;
  coreIdeal: number | null;
  coreWindow: number | null;
  brake: number | null;
  brakeIdeal: number | null;
  brakeWindow: number | null;
}

export interface LapEntry {
  lap: number;
  time: number | null;
  pit: boolean;
  outLap: boolean;
  valid: boolean;
  compound: string | null;
  fuel: number | null;
  energy: number | null;
}

export interface HubSettings {
  webPort: number;
  httpsPort: number;
  enableHttps: boolean;
  gamePort: number;
  gameHosts: string[];
  demoMode: boolean;
  demoSpeed: number;
  /** Fenêtre sans accélération graphique (Windows). */
  fenetreSansGpu?: boolean;
  /** Voix de l'ingénieur quand le PC de jeu la demande : « pc », « ecrans » ou « deux ». */
  voiceOutput?: VoiceOutput;
  ai: {
    ollamaUrl: string;
    localNodeName: string;
    nodes: { name: string; url: string }[];
    aliases: Record<string, string>;
    listenModel: string;
    thinkModel: string;
    speakModel: string;
    voice: string;
    language: string;
    proactiveRadio: boolean;
    /** Alertes de pilotage dites tout de suite (limites de piste, temps annulé, pénalité, usure brutale, départ). */
    drivingAlerts?: boolean;
    /** Mode « thinking » des modèles qui l'ont (raisonnement avant de répondre, plus lent). */
    thinkingMode?: boolean;
    /** Annonces de limites de piste et de temps annulé (aussi à la voix : « coupe les limites de piste »). */
    trackLimitAlerts?: boolean;
    /** L'ingénieur règle le menu des stands de Le Mans Ultimate (par le collecteur du PC de jeu). */
    lmuPitMenu?: boolean;
    /** « Copy », « Copy that » : accusés de réception en anglais. */
    englishCalls?: boolean;
    /** Modèle par spécialiste (« pneus », « stratégie », « voiture », « course ») ; vide = modèle « réflexion ». */
    specialists?: Record<string, string>;
    /** Au-delà (s), « Copy, je regarde ça et je reviens vers vous » et la réponse arrive ensuite. */
    ackAfterSeconds?: number;
    proactiveMinIntervalSec: number;
    strictAnswers: boolean;
    wetThresholdPct: number;
  };
  thermalProfiles: ThermalProfile[];
}

export interface ThermalProfile {
  game: string;
  car: string;
  tyreMin: number | null;
  tyreMax: number | null;
  coreMin: number | null;
  coreMax: number | null;
  brakeMin: number | null;
  brakeMax: number | null;
  pressureIdeal: number | null;
}

export interface OllamaModel {
  id: string;
  name: string;
  node: string;
  sizeBytes: number;
  capabilities: string[];
  voices: string[];
}

export interface NodeStatus {
  name: string;
  url: string;
  local: boolean;
  ok: boolean;
  error: string | null;
  modelCount: number;
  milliseconds: number;
}

export interface MemoryState {
  entries: { id: number; at: string; lap: number | null; source: string; topic: string; text: string }[];
  plannedStopLap: number | null;
  noStopPlanned: boolean;
  pending: string | null;
}

export interface HubStatus {
  source: {
    mode: 'demo' | 'game';
    demoSpeed: number;
    receiver: {
      port: number;
      isConnected: boolean;
      peers: string[];
      manualHosts: string[];
      secondsSinceLastPacket: number | null;
      secondsSinceLastData: number | null;
      game: string | null;
      kbPerSecond: number;
      localAddresses: string[];
    } | null;
    lastUpdateSecondsAgo: number | null;
    log: string[];
    capture: string | null;
    replay: string | null;
  };
  clients: number;
  version: string;
  dataDir: string;
  webPort: number;
  httpsPort: number | null;
  /** Postes audio du PC de jeu (collecteur : voix de l'ingénieur, micro). */
  stations?: AudioStation[];
  voiceOutput?: VoiceOutput;
}

// ───── Analyses (API /api/analysis/*, /api/telemetry, /api/weather) ─────

export interface TrackSegment {
  id: string;
  kind: 'corner' | 'straight';
  name: string;
  description: string | null;
  isMain: boolean;
  start: number;
  end: number;
  apex: number;
  referenceSpeed: number;
}

export interface SpeedAnalysis {
  segments: TrackSegment[];
  reference: string | null;
  message: string | null;
  drivers: {
    id: string;
    sampleHz: number;
    lapsAnalysed: number;
    lastLap: number | null;
    topSpeedLast: number | null;
    topSpeedBest: number | null;
    last: Record<string, number>;
    best: Record<string, number>;
  }[];
}

export interface SpeedTraces {
  binSize: number;
  segments: TrackSegment[];
  drivers: { id: string; lap: number; time: number | null; speed: (number | null)[] }[];
}

export interface Trajectories {
  segment: TrackSegment | null;
  /** Points : [x, z, vitesse km/h, distance depuis la ligne en m], un tous les ~10 m. */
  drivers: { id: string; lap: number; time: number | null; sampleHz: number; points: [number, number, number, number][] }[];
}

/** Micro-secteurs de chaque tour bouclé (GET /api/analysis/microsectors/laps?ids=…). */
export interface MicroSectorLaps {
  count: number;
  lapLength: number;
  drivers: {
    id: string;
    name: string | null;
    number: number | null;
    classId: string;
    className: string | null;
    car: string | null;
    isPlayer: boolean;
    sampleHz: number;
    laps: MicroSectorLapRow[];
  }[];
}

export interface MicroSectorLapRow {
  lap: number;
  time: number | null;
  /** 1 = stand, 2 = tour de sortie, 4 = invalide, 8 = micro-secteur dans la voie des stands. */
  flags: number;
  compound: string | null;
  stint: number | null;
  tyreLaps: number | null;
  ms: (number | null)[] | null;
}

/** Comparaison des tours du joueur (GET /api/analysis/lapcompare?laps=best,last,12,current). */
export interface LapComparison {
  step: number;
  lapLength: number;
  laps: LapChoice[];
  traces: LapDetail[];
}

export interface LapChoice {
  lap: number;
  time: number | null;
  complete: boolean;
  current: boolean;
  valid: boolean;
  pit: boolean;
  outLap: boolean;
  best: boolean;
  compound: string | null;
  tyreLaps: number | null;
}

export interface ShiftPoint { distance: number; speed: number; rpmBefore: number; rpmAfter: number; from: number; to: number }

export interface ControlChange { key: string; distance: number; from: number; to: number }

export interface LapDetail {
  lap: number;
  time: number | null;
  complete: boolean;
  current: boolean;
  valid: boolean;
  pit: boolean;
  outLap: boolean;
  compound: string | null;
  tyreLaps: number | null;
  fuel: number | null;
  topSpeed: number | null;
  maxRpm: number | null;
  speed: (number | null)[];
  throttle: (number | null)[];
  brake: (number | null)[];
  steering: (number | null)[];
  gear: (number | null)[];
  rpm: (number | null)[];
  /** Temps écoulé depuis la ligne (s), case par case. */
  elapsed: (number | null)[];
  shifts: ShiftPoint[];
  /** Réglages en roulant au départ du tour (« bias », « tc », « tcCut », « tcSlip », « abs », « map », « migration », « arbFront », « arbRear »). */
  controls: Record<string, number>;
  controlChanges: ControlChange[];
}

export interface MicroSectorReport {
  count: number;
  lapLength: number;
  drivers: {
    id: string;
    currentIndex: number;
    current: (number | null)[];
    last: (number | null)[];
    lastPit: boolean[];
    best: (number | null)[];
    theoreticalBest: number | null;
    sampleHz: number;
  }[];
  classBest: Record<string, (number | null)[]>;
}

/** [tour, temps, drapeaux : 1 = stand, 2 = sortie, 4 = invalide] */
export type CompactLap = [number, number | null, number];

export interface DriverLaps {
  id: string;
  laps: CompactLap[];
  fit: { slope: number; intercept: number; fromLap: number; laps: number } | null;
}

export interface TelemetryHistory {
  sessionId: number;
  t: number[];
  series: Record<string, (number | null)[]>;
}

export interface WeatherSample {
  t: number;
  air: number | null;
  track: number | null;
  rain: number;
  wetness: number;
  wind: number | null;
  windFrom: number | null;
}

export interface WeatherReport {
  history: WeatherSample[];
  forecast: WeatherSample[];
  trackPerHour: number | null;
  airPerHour: number | null;
  rainPerHour: number | null;
  trendMinutes: number;
  summary: string | null;
}

// ───── Accueil : temps / tours restants, événements de course (/api/race/events) ─────

export interface RaceLength {
  mode: 'laps' | 'time' | 'time+1' | null;
  timeRemaining: number | null;
  timeTotal: number | null;
  leaderLap: number | null;
  leaderTotalLaps: number | null;
  leaderLapsRemaining: number | null;
  playerLap: number | null;
  playerTotalLaps: number | null;
  playerLapsRemaining: number | null;
  playerFinishIn: number | null;
  playerLapsPossible: number | null;
  progress: number;
  estimated: boolean;
  leaderPace: number | null;
  playerPace: number | null;
  leaderFinished: boolean;
}

export interface RaceEvent {
  id: number;
  time: number;
  lap: number | null;
  kind: 'flag' | 'session' | 'pit' | 'bestlap' | 'track' | 'penalty' | 'retire' | 'position' | 'tyres' | 'damage' | string;
  level: 'info' | 'good' | 'warn' | 'bad';
  title: string;
  text: string | null;
  driverId: string | null;
  isPlayer: boolean;
}
