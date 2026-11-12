import { useEffect, useState } from 'react';
import { api } from '../live';
import type { HubSettings, RaceSnapshot, ThermalProfile } from '../types';

const FIELDS: { key: keyof ThermalProfile; label: string; unit: string }[] = [
  { key: 'tyreMin', label: 'Pneus min', unit: '°C' },
  { key: 'tyreMax', label: 'Pneus max', unit: '°C' },
  { key: 'coreMin', label: 'Carcasse min', unit: '°C' },
  { key: 'coreMax', label: 'Carcasse max', unit: '°C' },
  { key: 'brakeMin', label: 'Freins min', unit: '°C' },
  { key: 'brakeMax', label: 'Freins max', unit: '°C' },
  { key: 'pressureIdeal', label: 'Pression idéale', unit: 'bar' },
];

/**
 * Plages de température par jeu et par voiture : sous le minimum = bleu (froid), dans la plage = vert, au-dessus = rouge.
 * Le profil le plus précis l'emporte (voiture précise > jeu entier > tous les jeux). Champ vide = valeur donnée par le jeu.
 */
export function ThermalProfiles({ snapshot }: { snapshot: RaceSnapshot | null }) {
  const [settings, setSettings] = useState<HubSettings | null>(null);
  const [profiles, setProfilesRaw] = useState<(ThermalProfile & { __k?: number })[]>([]);
  // Clé stable par ligne (les champs numériques ne sont pas contrôlés, pour pouvoir taper « 1,85 » librement).
  const setProfiles = (u: ThermalProfile[] | ((p: (ThermalProfile & { __k?: number })[]) => (ThermalProfile & { __k?: number })[])) =>
    setProfilesRaw((prev) => (typeof u === 'function' ? u(prev) : u).map((p) => { const k = (p as { __k?: number }).__k; return { ...p, __k: k ?? Math.random() }; }));
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    api<HubSettings>('/api/settings').then((s) => { setSettings(s); setProfiles(s.thermalProfiles ?? []); }).catch(() => {});
  }, []);

  const me = snapshot?.drivers.find((d) => d.isPlayer);
  const w = snapshot?.player?.wheels[0];
  const addCurrent = () => {
    const round = (v: number | null | undefined) => (v == null ? null : Math.round(v));
    setProfiles((p) => [...p, {
      game: snapshot?.source ?? '',
      car: me?.car ?? '',
      tyreMin: w?.idealTemp != null ? round(w.idealTemp - (w.tempWindow ?? 10)) : 75,
      tyreMax: w?.idealTemp != null ? round(w.idealTemp + (w.tempWindow ?? 10)) : 95,
      coreMin: null,
      coreMax: null,
      brakeMin: w?.brakeIdeal != null ? round(w.brakeIdeal - (w.brakeWindow ?? 150)) : 300,
      brakeMax: w?.brakeIdeal != null ? round(w.brakeIdeal + (w.brakeWindow ?? 150)) : 650,
      pressureIdeal: w?.idealPressure ?? null,
    }]);
  };

  const update = (i: number, patch: Partial<ThermalProfile>) => setProfiles((p) => p.map((x, k) => (k === i ? { ...x, ...patch } : x)));
  const save = async () => {
    if (!settings) return;
    try {
      const next = await api<HubSettings>('/api/settings', { method: 'PUT', body: JSON.stringify({ ...settings, thermalProfiles: profiles.map(({ __k, ...rest }) => { void __k; return rest; }) }) });
      setSettings(next);
      setProfiles(next.thermalProfiles ?? []);
      setMessage('Plages enregistrées : les couleurs sont mises à jour sur tous les écrans.');
    } catch (e) {
      setMessage(`Erreur : ${(e as Error).message}`);
    }
    window.setTimeout(() => setMessage(null), 4000);
  };

  const num = (v: number | null, unit: string) => (v == null ? '' : unit === 'bar' ? (v / 100).toFixed(2) : String(v));
  const parse = (text: string, unit: string) => {
    if (text.trim() === '') return null;
    const n = Number(text.replace(',', '.'));
    return isFinite(n) ? (unit === 'bar' ? Math.round(n * 100) : n) : null;
  };

  return (
    <section className="card">
      <div className="card-head">
        <span className="card-title">Plages de température par jeu et voiture</span>
        <span className="muted hide-xs">bleu = froid · vert = bon · rouge = trop chaud</span>
      </div>
      <div className="card-body stack-sm">
        <p className="secondary small" style={{ margin: 0 }}>
          Actuellement : <b>{snapshot?.thermalProfile ? `profil « ${snapshot.thermalProfile} »` : 'valeurs fournies par le jeu'}</b>
          {snapshot?.source ? ` (jeu « ${snapshot.source} »${me?.car ? `, voiture « ${me.car} »` : ''})` : ''}.
          « Voiture » peut être une partie du nom (ex. « GT3 », « 963 ») ; vide = toutes les voitures du jeu. Un champ vide garde la valeur du jeu.
        </p>
        {profiles.length > 0 && (
          <div className="table-scroll">
            <table className="mini-table profiles">
              <thead>
                <tr><th>Jeu</th><th>Voiture</th>{FIELDS.map((f) => <th key={f.key} className="r">{f.label} <span className="muted">({f.unit})</span></th>)}<th /></tr>
              </thead>
              <tbody>
                {profiles.map((p, i) => (
                  <tr key={p.__k ?? i}>
                    <td><input className="input compact-input" value={p.game} onChange={(e) => update(i, { game: e.target.value })} placeholder="tous" /></td>
                    <td><input className="input compact-input wide" value={p.car} onChange={(e) => update(i, { car: e.target.value })} placeholder="toutes" /></td>
                    {FIELDS.map((f) => (
                      <td key={f.key}>
                        <input className="input compact-input num" inputMode="decimal" defaultValue={num(p[f.key] as number | null, f.unit)}
                          onBlur={(e) => update(i, { [f.key]: parse(e.target.value, f.unit) } as Partial<ThermalProfile>)} placeholder="jeu" />
                      </td>
                    ))}
                    <td><button className="close" onClick={() => setProfiles((x) => x.filter((_, k) => k !== i))} aria-label="Supprimer ce profil">✕</button></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        <div className="row">
          <button className="btn" onClick={addCurrent}>+ Profil pour {me?.car ? `la ${me.car}` : 'ce jeu'}</button>
          <button className="btn primary" onClick={save} disabled={!settings}>Enregistrer</button>
          {message && <span className="secondary small">{message}</span>}
        </div>
      </div>
    </section>
  );
}
