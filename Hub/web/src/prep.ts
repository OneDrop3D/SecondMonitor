// Préparer la course : plan d'arrêts calculé avant le départ (même logique que RacePlanner côté hub).

/** Mêmes valeurs que le hub : PlayerStrategyTracker.FuelMarginLaps et RacePlanner.MaxRealisticSaving. */
export const DEFAULT_MARGIN = 1;
export const MAX_REALISTIC_SAVING = 0.15;

/** Le Mans Ultimate : temps immobile = 2 s + 0,40 s par % d'énergie remis + 12 s pour quatre pneus (RacePlanner.LmuStopSeconds). */
export const lmuStopSeconds = (energyPct: number, tyres: boolean) => 2 + 0.4 * Math.max(0, energyPct) + (tyres ? 12 : 0);

export interface PrepInput {
  /** Course au temps (minutes) ou au nombre de tours : le nombre de tours l'emporte s'il est donné. */
  minutes: number | null;
  laps: number | null;
  lapTime: number;
  fuelPerLap: number;
  /** Énergie virtuelle (LMU), % par tour. */
  energyPerLap: number | null;
  tank: number;
  /** Temps perdu par le passage au stand (entrée, limiteur, sortie), sans le temps immobile. */
  pitLoss: number;
  /** Tours que tient un train de pneus ; null : pneus changés à chaque arrêt. */
  tyreLife: number | null;
  margin: number;
}

export interface PrepStop {
  /** Arrêt à la fin de ce tour. */
  lap: number;
  /** Tours du relais qui suit. */
  stint: number;
  fuel: number;
  energy: number | null;
  tyres: boolean;
  /** Temps immobile (LMU, connu seulement avec l'énergie). */
  stationary: number | null;
  /** Ratio essence / énergie à régler (L par %). */
  ratio: number | null;
}

export interface PrepPlan {
  laps: number;
  stops: PrepStop[];
  /** Économie par tour nécessaire (0 : rien à économiser). */
  saving: number;
  fuelTarget: number;
  energyTarget: number | null;
  /** Temps total passé au stand (passages + immobile). */
  pitTime: number;
}

const valid = (i: PrepInput) => i.lapTime > 0 && i.fuelPerLap > 0 && i.tank > 0 && ((i.laps ?? 0) > 0 || (i.minutes ?? 0) > 0);

/** Relais réguliers : n arrêts répartis sur la course (voiture plus légère, pneus ménagés, toujours dans la fenêtre du hub). */
function build(i: PrepInput, laps: number, n: number): PrepPlan {
  const ends = Array.from({ length: n + 1 }, (_, k) => (k === n ? laps : Math.round((laps * (k + 1)) / (n + 1))));
  const stints = ends.map((e, k) => e - (k === 0 ? 0 : ends[k - 1]));
  // Le relais le plus exigeant (le dernier garde la marge) fixe la consommation à viser.
  const longest = Math.max(...stints.map((s, k) => s + (k === n ? i.margin : 0)));
  const fuelTarget = Math.min(i.fuelPerLap, i.tank / longest);
  const energyTarget = i.energyPerLap ? Math.min(i.energyPerLap, 100 / longest) : null;
  const saving = Math.max(1 - fuelTarget / i.fuelPerLap, energyTarget != null && i.energyPerLap ? 1 - energyTarget / i.energyPerLap : 0);

  let fuel = i.tank, energy = 100, tyreAge = 0, pitTime = 0;
  const stops: PrepStop[] = [];
  for (let k = 0; k < n; k++) {
    fuel -= stints[k] * fuelTarget;
    tyreAge += stints[k];
    const next = stints[k + 1] + (k + 1 === n ? i.margin : 0);
    const addFuel = Math.min(i.tank - fuel, Math.max(0, next * fuelTarget - fuel));
    fuel += addFuel;
    let addEnergy: number | null = null;
    if (energyTarget != null) {
      energy -= stints[k] * energyTarget;
      addEnergy = Math.min(100 - energy, Math.max(0, next * energyTarget - energy));
      energy += addEnergy;
    }
    const tyres = i.tyreLife == null || tyreAge + stints[k + 1] > i.tyreLife;
    if (tyres) tyreAge = 0;
    const stationary = addEnergy != null ? lmuStopSeconds(addEnergy, tyres) : null;
    pitTime += i.pitLoss + (stationary ?? 0);
    stops.push({ lap: ends[k], stint: stints[k + 1], fuel: addFuel, energy: addEnergy, tyres, stationary, ratio: addEnergy && addEnergy > 0.5 ? addFuel / addEnergy : null });
  }
  return { laps, stops, saving, fuelTarget, energyTarget, pitTime };
}

/** Arrêts nécessaires pour faire `laps` tours sans rien économiser. */
function stopsNeeded(i: PrepInput, laps: number) {
  const range = Math.min(i.tank / i.fuelPerLap, i.energyPerLap ? 100 / i.energyPerLap : Infinity);
  return Math.max(0, Math.ceil((laps + i.margin) / range - 1 - 1e-9));
}

/** Tours de la course : au temps, le temps passé au stand en retire ; la course finit au tour qui suit le temps écoulé. */
function raceLaps(i: PrepInput): number {
  if (i.laps && i.laps > 0) return Math.round(i.laps);
  const total = (i.minutes ?? 0) * 60;
  let laps = Math.floor(total / i.lapTime) + 1;
  for (let k = 0; k < 5; k++) laps = Math.max(1, Math.floor((total - build(i, laps, stopsNeeded(i, laps)).pitTime) / i.lapTime) + 1);
  return laps;
}

export interface Preparation { base: PrepPlan; oneLess: PrepPlan | null; oneMore: PrepPlan }

export function prepare(i: PrepInput): Preparation | null {
  if (!valid(i)) return null;
  const laps = raceLaps(i);
  const n = stopsNeeded(i, laps);
  return { base: build(i, laps, n), oneLess: n > 0 ? build(i, laps, n - 1) : null, oneMore: build(i, laps, n + 1) };
}

// Auto-vérification (développement) : 2 h au Mans en 3:30, 7,3 L et 7,9 %/tour, 100 L → 34 tours (le stand en prend un), 2 arrêts réguliers.
if ((import.meta as { env?: { DEV?: boolean } }).env?.DEV) {
  const p = prepare({ minutes: 120, laps: null, lapTime: 210, fuelPerLap: 7.3, energyPerLap: 7.9, tank: 100, pitLoss: 60, tyreLife: null, margin: 1 });
  console.assert(p != null && p.base.stops.length === 2 && p.base.saving === 0 && p.oneLess!.saving > MAX_REALISTIC_SAVING, 'prep.ts : plan de référence faux', p);
  console.assert(p != null && p.base.stops.every((s) => s.tyres && s.stationary! > 14), 'prep.ts : arrêts faux', p);
}
