import { useState } from 'react';

// Préférences de cet écran (navigateur ou fenêtre de l'application) : gardées au changement d'onglet et au redémarrage.

export function readPref(key: string, fallback: string) {
  try { return localStorage.getItem(key) ?? fallback; } catch { return fallback; }
}

export function writePref(key: string, value: string) {
  try { localStorage.setItem(key, value); } catch { /* stockage indisponible */ }
}

export function readJsonPref<T>(key: string, fallback: T): T {
  try {
    const raw = localStorage.getItem(key);
    return raw ? { ...fallback, ...JSON.parse(raw) } as T : fallback;
  } catch {
    return fallback;
  }
}

export function readJsonList<T>(key: string): T[] {
  try {
    const v = JSON.parse(localStorage.getItem(key) ?? '[]');
    return Array.isArray(v) ? v as T[] : [];
  } catch {
    return [];
  }
}

export function writeJsonPref(key: string, value: unknown) {
  writePref(key, JSON.stringify(value));
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
