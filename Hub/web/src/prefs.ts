import { useState } from 'react';

// Préférences de cet écran (navigateur ou fenêtre de l'application) : gardées au changement d'onglet et au redémarrage.
//
// Plusieurs fenêtres sur un même ordinateur partagent le même stockage. Une fenêtre ajoutée (« ?fenetre=2 », bouton
// « Nouvelle fenêtre ») range donc ses préférences à part, sous une clé préfixée :
//   - readPref / writePref : préférence d'affichage de la fenêtre (disposition, thème, filtres…). Une fenêtre ajoutée
//     part de celle de la fenêtre principale tant qu'elle n'a pas choisi la sienne ;
//   - readOwnPref : réglage propre à la fenêtre, sans reprendre celui de la principale (voix, poste radio, bouton du
//     volant, page affichée) : une fenêtre ajoutée est muette et n'écoute pas le micro, sauf choix explicite ;
//   - readDevicePref / writeDevicePref : commun à toutes les fenêtres de l'appareil (guide vu, micro et haut-parleur…).

/** Numéro de cette fenêtre : 2, 3… pour une fenêtre ajoutée (« ?fenetre=2 ») ; 1 pour la fenêtre principale et un navigateur. */
export const windowNumber = readWindowNumber();

/** Fenêtre ajoutée : préférences d'affichage à part, muette et pas poste radio par défaut. */
export const isExtraWindow = windowNumber > 1;

/** Page ouverte par l'application (adresse avec « fenetre ») : elle reprend la page affichée la dernière fois. */
export const hasWindowParam = readWindowParam() != null;

function readWindowParam(): string | null {
  try { return new URLSearchParams(location.search).get('fenetre'); } catch { return null; }
}

function readWindowNumber(): number {
  const n = Number(readWindowParam());
  return Number.isInteger(n) && n >= 1 && n <= 99 ? n : 1;
}

/** Clé de stockage de cette fenêtre (inchangée pour la fenêtre principale). */
export function windowKey(key: string): string {
  return isExtraWindow ? `fenetre${windowNumber}:${key}` : key;
}

function get(key: string): string | null {
  try { return localStorage.getItem(key); } catch { return null; }
}

function set(key: string, value: string | null) {
  try { if (value == null) localStorage.removeItem(key); else localStorage.setItem(key, value); } catch { /* stockage indisponible */ }
}

/** Préférence d'affichage de cette fenêtre ; une fenêtre ajoutée reprend celle de la principale tant qu'elle n'a pas la sienne. */
export function readPref(key: string, fallback: string): string {
  return get(windowKey(key)) ?? (isExtraWindow ? get(key) : null) ?? fallback;
}

/** Enregistre pour cette fenêtre seulement (null : efface). */
export function writePref(key: string, value: string | null) {
  set(windowKey(key), value);
}

/** Réglage propre à cette fenêtre, sans reprendre celui de la principale. */
export function readOwnPref(key: string, fallback: string): string {
  return get(windowKey(key)) ?? fallback;
}

/** Réglage commun à toutes les fenêtres de cet appareil. */
export function readDevicePref(key: string, fallback: string): string {
  return get(key) ?? fallback;
}

export function writeDevicePref(key: string, value: string | null) {
  set(key, value);
}

export function readJsonPref<T>(key: string, fallback: T): T {
  try {
    const raw = readPref(key, '');
    return raw ? { ...fallback, ...JSON.parse(raw) } as T : fallback;
  } catch {
    return fallback;
  }
}

/** Liste gardée dans les préférences ; device : commune à toutes les fenêtres (configurations enregistrées…). */
export function readJsonList<T>(key: string, device = false): T[] {
  try {
    const v = JSON.parse((device ? readDevicePref(key, '[]') : readPref(key, '[]')) || '[]');
    return Array.isArray(v) ? v as T[] : [];
  } catch {
    return [];
  }
}

export function writeJsonPref(key: string, value: unknown, device = false) {
  if (device) writeDevicePref(key, JSON.stringify(value));
  else writePref(key, JSON.stringify(value));
}

/** État gardé dans les préférences (objet complété par les valeurs par défaut s'il manque des champs). */
export function useStoredState<T extends object>(key: string, defaults: T): [T, (patch: Partial<T> | ((v: T) => T)) => void] {
  const [value, setValue] = useState<T>(() => readJsonPref(key, defaults));
  const update = (patch: Partial<T> | ((v: T) => T)) => {
    setValue((current) => {
      const next = typeof patch === 'function' ? patch(current) : { ...current, ...patch };
      writeJsonPref(key, next);
      return next;
    });
  };
  return [value, update];
}

/** Choix simple gardé dans les préférences (texte). */
export function useStoredChoice<T extends string>(key: string, fallback: T, allowed?: readonly T[]): [T, (v: T) => void] {
  const [value, setValue] = useState<T>(() => {
    const v = readPref(key, fallback) as T;
    return !allowed || allowed.includes(v) ? v : fallback;
  });
  return [value, (v: T) => { setValue(v); writePref(key, v); }];
}
