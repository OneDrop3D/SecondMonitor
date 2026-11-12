import type { RaceSnapshot } from '../types';
import { MAX_SELECTED, type Selection } from '../selection';

/** Choix des pilotes comparés : pastilles colorées (couleur = celle des courbes) + ajout rapide. */
export function DriverPicker({ snapshot, selection }: { snapshot: RaceSnapshot; selection: Selection }) {
  const byId = new Map(snapshot.drivers.map((d) => [d.id, d]));
  const available = snapshot.drivers.filter((d) => !selection.ids.includes(d.id));
  return (
    <div className="picker" role="group" aria-label="Pilotes comparés">
      {selection.ids.map((id) => {
        const d = byId.get(id);
        if (!d) return null;
        return (
          <button key={id} className="chip on picker-chip" onClick={() => selection.toggle(id)} title="Retirer de la comparaison">
            <span className="legend-line" style={{ borderColor: selection.colorOf(id) }} />
            {d.isPlayer ? 'Vous' : `#${d.number ?? '–'} ${d.shortName}`}
            <span className="muted" aria-hidden>×</span>
          </button>
        );
      })}
      {selection.ids.length < MAX_SELECTED && available.length > 0 && (
        <select className="select compact" value="" onChange={(e) => e.target.value && selection.toggle(e.target.value)} aria-label="Ajouter un pilote">
          <option value="">+ Ajouter…</option>
          {available.map((d) => (
            <option key={d.id} value={d.id}>P{d.position} · #{d.number ?? '–'} {d.name}{d.className ? ` (${d.className})` : ''}</option>
          ))}
        </select>
      )}
      <span className="picker-presets">
        <button className="chip" onClick={() => selection.set(selection.neighbours())}>Mes voisins</button>
        <button className="chip" onClick={() => selection.set(selection.classLeaders())}>Leaders</button>
      </span>
    </div>
  );
}
