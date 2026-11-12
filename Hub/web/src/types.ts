// Miroir des DTO C# (SecondMonitor.Hub.Core.Race.RaceSnapshot). Durées en s, distances en m, litres, kPa, °C.

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
}

export interface TrackDto {
  name: string | null;
  layout: string | null;
  length: number;
  outline: [number, number][] | null;
  worldPositionsValid: boolean;
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
      localAddresses: string[];
    } | null;
    lastUpdateSecondsAgo: number | null;
    log: string[];
  };
  clients: number;
  version: string;
  dataDir: string;
  webPort: number;
  httpsPort: number | null;
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
  drivers: { id: string; lap: number; time: number | null; sampleHz: number; points: [number, number, number][] }[];
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
