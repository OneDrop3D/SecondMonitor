// « Nouvelle fenêtre » : une fenêtre de plus pour un autre écran, avec sa propre page et ses préférences d'affichage
// (adresse « ?fenetre=2 », voir prefs.ts). Dans l'application, SecondMonitor l'ouvre lui-même sur un écran libre
// (AppWindow.cs) ; sinon (navigateur, iPad…) c'est une fenêtre du navigateur, à glisser sur l'autre écran.
import { isExtraWindow, readDevicePref, windowNumber, writeDevicePref } from './prefs';

/** Une fenêtre ajoutée ouverte dans ce navigateur signale sa présence ici (heure du dernier signe de vie). */
const ALIVE = 'smhub-fenetre-ouverte:';
const BEAT_MS = 10_000;
/** Un onglet caché peut voir ses minuteries ralenties : on attend largement avant de considérer un numéro libre. */
const STALE_MS = 120_000;

/** Fenêtre ajoutée : signale qu'elle est ouverte, pour qu'une nouvelle fenêtre du navigateur ne prenne pas son numéro. */
export function announceWindow() {
  if (!isExtraWindow) return;
  const key = ALIVE + windowNumber;
  const beat = () => writeDevicePref(key, String(Date.now()));
  beat();
  const timer = window.setInterval(beat, BEAT_MS);
  window.addEventListener('pagehide', () => { window.clearInterval(timer); writeDevicePref(key, null); });
  window.addEventListener('pageshow', (e) => { if (e.persisted) beat(); });
}

export type NewWindowOutcome =
  | { kind: 'native'; number: number; otherScreen: boolean }
  | { kind: 'browser'; number: number }
  | { kind: 'blocked'; reason?: string };

/** Page affichée dans la fenêtre de l'application (Photino ajoute window.external.sendMessage), pas dans un navigateur. */
export function inAppWindow(): boolean {
  try {
    return typeof (window.external as unknown as { sendMessage?: unknown } | undefined)?.sendMessage === 'function';
  } catch {
    return false;
  }
}

/**
 * Numéro pour une fenêtre du navigateur : le plus petit qui n'est pas celui d'une fenêtre encore ouverte ici
 * (comme dans l'application : une fenêtre rouverte retrouve la page et la disposition de ce numéro). Réservé aussitôt.
 */
function nextBrowserNumber(): number {
  const now = Date.now();
  for (let n = 2; n < 99; n++) {
    if (n === windowNumber) continue;
    const seen = Number(readDevicePref(ALIVE + n, '0'));
    if (!(now - seen < STALE_MS)) {
      writeDevicePref(ALIVE + n, String(now));
      return n;
    }
  }
  return 99;
}

/** Ouvre une fenêtre de plus, sur la page <tab> (celle d'où l'on vient). */
export async function openNewWindow(tab: string): Promise<NewWindowOutcome> {
  let reason: string | undefined;
  try {
    const res = await fetch('/api/fenetres', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ onglet: tab }) });
    const json = await res.json() as { native?: boolean; numero?: number; autreEcran?: boolean; raison?: string };
    if (res.ok && json.native && json.numero) return { kind: 'native', number: json.numero, otherScreen: !!json.autreEcran };
    reason = json.raison;
  } catch {
    /* hub injoignable ou ancienne version : fenêtre du navigateur */
  }

  // La vue web de l'application n'ouvre pas de fenêtre de navigateur : inutile d'essayer.
  if (inAppWindow()) return { kind: 'blocked', reason };
  const n = nextBrowserNumber();
  const opened = window.open(`${location.pathname}?fenetre=${n}#${tab}`, `secondmonitor-fenetre-${n}`, 'popup=yes,width=1280,height=800');
  return opened ? { kind: 'browser', number: n } : { kind: 'blocked', reason: 'Le navigateur a bloqué la fenêtre : autorisez les fenêtres surgissantes pour cette adresse.' };
}
