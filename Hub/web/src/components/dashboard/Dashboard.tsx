import { useEffect, useRef, useState, type KeyboardEvent as ReactKeyboardEvent, type PointerEvent as ReactPointerEvent } from 'react';
import { useWidth } from '../../charts/useSize';
import { ErrorBoundary } from '../ErrorBoundary';
import { Icon } from '../Timing';
import { allBlocks, getBlock, onRegistryChange, type DashboardContext } from './registry';
import { GAP, ROW, clamp, colsFor, limits, loadLayout, resetLayout, saveLayout, type LayoutItem, type LayoutMode } from './layout';
import './BlocksCore';
import './dashboard.css';

/*
 * Accueil en grille de blocs personnalisable (bouton « Disposition » de la barre du haut) :
 * déplacer par glisser (poignée ⠿, souris ou doigt), changer la largeur et la hauteur par paliers (coin ↘ ou boutons),
 * masquer, ajouter depuis la liste, réinitialiser. Disposition enregistrée sur cet appareil :
 * une pour les grands écrans (12 colonnes), une pour les tablettes (6 colonnes) ; le téléphone suit celle de la tablette sur une colonne.
 */

const HEAD = 30;

export function Dashboard({ ctx, editing, onDone }: { ctx: DashboardContext; editing: boolean; onDone: () => void }) {
  const { ref, width } = useWidth<HTMLDivElement>(1200);
  const { cols, mode, phone } = colsFor(width);
  const [layouts, setLayouts] = useState<Record<LayoutMode, LayoutItem[]>>(() => ({ large: loadLayout('large'), medium: loadLayout('medium') }));
  const [, setRegistryTick] = useState(0);
  const [dragging, setDragging] = useState<string | null>(null);
  const [adding, setAdding] = useState(false);
  const items = layouts[mode];
  const scroller = useRef<HTMLDivElement>(null);

  // Un module peut enregistrer un bloc après le premier affichage : on le propose dans la liste.
  useEffect(() => onRegistryChange(() => {
    setRegistryTick((t) => t + 1);
    setLayouts({ large: loadLayout('large'), medium: loadLayout('medium') });
  }), []);
  useEffect(() => { if (!editing) setAdding(false); }, [editing]);

  const update = (fn: (list: LayoutItem[]) => LayoutItem[]) => {
    setLayouts((all) => {
      const next = fn(all[mode]);
      saveLayout(mode, next);
      return { ...all, [mode]: next };
    });
  };

  const colW = cols > 0 ? (width - (cols - 1) * GAP) / cols : width;

  // ───── Déplacement (poignée) ─────
  const drag = useRef<{ type: string; target?: string; after?: boolean } | null>(null);
  const onGripDown = (e: ReactPointerEvent, type: string) => {
    if (e.button !== 0) return;
    e.preventDefault();
    capture(e);
    drag.current = { type };
    setDragging(type);
  };
  const onGripMove = (e: ReactPointerEvent) => {
    const d = drag.current;
    if (!d) return;
    const sc = scroller.current?.getBoundingClientRect();
    if (sc && scroller.current) {
      if (e.clientY < sc.top + 48) scroller.current.scrollBy(0, -16);
      else if (e.clientY > sc.bottom - 48) scroller.current.scrollBy(0, 16);
    }
    const el = (document.elementFromPoint(e.clientX, e.clientY) as HTMLElement | null)?.closest<HTMLElement>('[data-block]');
    const target = el?.dataset.block;
    if (!el || !target || target === d.type) return;
    const r = el.getBoundingClientRect();
    const after = (e.clientX - r.left) / r.width + (e.clientY - r.top) / r.height > 1;
    if (d.target === target && d.after === after) return;
    d.target = target;
    d.after = after;
    update((list) => move(list, d.type, target, after));
  };
  const onGripUp = () => { drag.current = null; setDragging(null); };
  const onGripKey = (e: ReactKeyboardEvent, type: string) => {
    const visible = items.filter((i) => !i.hidden);
    const idx = visible.findIndex((i) => i.type === type);
    if (e.key === 'ArrowUp' || e.key === 'ArrowLeft') {
      if (idx > 0) update((list) => move(list, type, visible[idx - 1].type, false));
      e.preventDefault();
    } else if (e.key === 'ArrowDown' || e.key === 'ArrowRight') {
      if (idx >= 0 && idx < visible.length - 1) update((list) => move(list, type, visible[idx + 1].type, true));
      e.preventDefault();
    }
  };

  // ───── Taille (coin) ─────
  const resize = useRef<{ type: string; x: number; y: number; w: number; h: number } | null>(null);
  const onResizeDown = (e: ReactPointerEvent, it: LayoutItem) => {
    if (e.button !== 0) return;
    e.preventDefault();
    capture(e);
    resize.current = { type: it.type, x: e.clientX, y: e.clientY, w: it.w, h: it.h };
    setDragging(it.type);
  };
  const onResizeMove = (e: ReactPointerEvent) => {
    const r = resize.current;
    if (!r) return;
    const lim = limits(r.type, cols);
    const w = phone ? r.w : clamp(r.w + Math.round((e.clientX - r.x) / (colW + GAP)), lim.minW, lim.maxW);
    const h = clamp(r.h + Math.round((e.clientY - r.y) / (ROW + GAP)), lim.minH, lim.maxH);
    update((list) => (list.some((i) => i.type === r.type && (i.w !== w || i.h !== h)) ? list.map((i) => (i.type === r.type ? { ...i, w, h } : i)) : list));
  };
  const onResizeUp = () => { resize.current = null; setDragging(null); };

  const setSize = (type: string, dw: number, dh: number) => {
    const lim = limits(type, cols);
    update((list) => list.map((i) => (i.type === type ? { ...i, w: clamp(i.w + dw, lim.minW, lim.maxW), h: clamp(i.h + dh, lim.minH, lim.maxH) } : i)));
  };
  const hide = (type: string) => update((list) => list.map((i) => (i.type === type ? { ...i, hidden: true } : i)));
  const add = (type: string) => {
    const def = getBlock(type);
    update((list) => {
      const rest = list.filter((i) => i.type !== type);
      const size = mode === 'large' ? def?.size : def?.medium ?? def?.size;
      const prev = list.find((i) => i.type === type);
      return [...rest, { type, w: prev?.w ?? size?.w ?? 4, h: prev?.h ?? size?.h ?? 5, hidden: false }];
    });
    setAdding(false);
    window.setTimeout(() => document.querySelector(`[data-block="${CSS.escape(type)}"]`)?.scrollIntoView({ behavior: 'smooth', block: 'nearest' }), 50);
  };
  const reset = () => {
    if (!window.confirm(`Revenir à la disposition par défaut ${mode === 'large' ? 'grand écran' : 'tablette / téléphone'} sur cet appareil ?`)) return;
    setLayouts((all) => ({ ...all, [mode]: resetLayout(mode) }));
  };

  const visible = items.filter((i) => !i.hidden && getBlock(i.type));
  const hidden = allBlocks().filter((b) => !visible.some((i) => i.type === b.type));

  return (
    <div className={`db-wrap ${editing ? 'editing' : ''}`} ref={scroller}>
      {editing && (
        <div className="db-editbar" role="toolbar" aria-label="Disposition de l'accueil">
          <Icon name="layout" size={18} />
          <span className="db-editbar-text">
            <b>Disposition {mode === 'large' ? 'grand écran' : phone ? 'téléphone' : 'tablette'}</b>
            <span className="muted"> · {phone ? 'flèches ou poignée ⠿ pour l\'ordre, ↕ pour la hauteur' : 'poignée ⠿ pour déplacer, coin ↘ ou boutons pour la taille'} · enregistrée sur cet appareil</span>
          </span>
          <span className="spacer" />
          <div className="db-add">
            <button className="btn" onClick={() => setAdding((a) => !a)} aria-expanded={adding} disabled={hidden.length === 0}><Icon name="plus" size={14} /> Ajouter un bloc</button>
            {adding && (
              <div className="db-add-menu" role="menu">
                {hidden.map((b) => (
                  <button key={b.type} role="menuitem" className="db-add-item" onClick={() => add(b.type)}>
                    <Icon name={b.icon} size={18} />
                    <span><b>{b.title}</b><span className="muted small">{b.description}</span></span>
                  </button>
                ))}
              </div>
            )}
          </div>
          <button className="btn" onClick={reset}>Réinitialiser</button>
          <button className="btn primary" onClick={onDone}>Terminé</button>
        </div>
      )}
      <div ref={ref} className={`db-grid ${phone ? 'phone' : ''}`} style={{ gridTemplateColumns: `repeat(${cols}, minmax(0, 1fr))`, gridAutoRows: `${ROW}px`, gap: `${GAP}px` }}>
        {visible.map((it) => {
          const def = getBlock(it.type)!;
          const span = phone ? 1 : Math.min(cols, it.w);
          const bodyWidth = Math.round((phone ? width : span * colW + (span - 1) * GAP) - 2);
          const bodyHeight = it.h * ROW + (it.h - 1) * GAP - HEAD - 2;
          const props = { ctx, size: { w: span, h: it.h, cols, bodyWidth, bodyHeight } };
          return (
            <section
              key={it.type}
              data-block={it.type}
              className={`db-block db-b-${it.type} ${editing ? 'editing' : ''} ${dragging === it.type ? 'dragging' : ''}`}
              style={{ gridColumn: `span ${span}`, gridRow: `span ${it.h}` }}
              aria-label={def.title}
            >
              <header className="db-head">
                {editing && (
                  <button
                    className="db-grip"
                    onPointerDown={(e) => onGripDown(e, it.type)}
                    onPointerMove={onGripMove}
                    onPointerUp={onGripUp}
                    onPointerCancel={onGripUp}
                    onKeyDown={(e) => onGripKey(e, it.type)}
                    title="Glisser pour déplacer (ou flèches du clavier)"
                    aria-label={`Déplacer ${def.title}`}
                  >
                    <Icon name="grip" size={18} />
                  </button>
                )}
                <Icon name={def.icon} size={15} />
                <span className="db-title">{def.title}</span>
                {!editing && def.badge?.(props)}
                <span className="spacer" />
                {editing && (
                  <span className="db-sizer">
                    {!phone && <span className="db-sizer-grp" title={`Largeur : ${span} colonne(s) sur ${cols}`}>
                      <span className="db-sizer-ico" aria-hidden>↔</span>
                      <button onClick={() => setSize(it.type, -1, 0)} aria-label="Moins large">−</button>
                      <span className="tnum">{span}</span>
                      <button onClick={() => setSize(it.type, 1, 0)} aria-label="Plus large">+</button>
                    </span>}
                    <span className="db-sizer-grp" title={`Hauteur : ${it.h} rangée(s)`}>
                      <span className="db-sizer-ico" aria-hidden>↕</span>
                      <button onClick={() => setSize(it.type, 0, -1)} aria-label="Moins haut">−</button>
                      <span className="tnum">{it.h}</span>
                      <button onClick={() => setSize(it.type, 0, 1)} aria-label="Plus haut">+</button>
                    </span>
                    <button className="db-hide" onClick={() => hide(it.type)} aria-label={`Masquer ${def.title}`} title="Masquer (à rajouter avec « Ajouter un bloc »)"><Icon name="close" size={14} /></button>
                  </span>
                )}
              </header>
              <div className="db-body">
                <ErrorBoundary label={def.title} resetKey={it.type}>
                  {def.render(props)}
                </ErrorBoundary>
              </div>
              {editing && (
                <span
                  className="db-resize"
                  onPointerDown={(e) => onResizeDown(e, it)}
                  onPointerMove={onResizeMove}
                  onPointerUp={onResizeUp}
                  onPointerCancel={onResizeUp}
                  title="Glisser pour changer la taille"
                  aria-hidden
                />
              )}
            </section>
          );
        })}
        {visible.length === 0 && (
          <div className="db-empty db-none">
            <div>Aucun bloc affiché.</div>
            <div className="muted small">« Disposition » › « Ajouter un bloc », ou « Réinitialiser ».</div>
          </div>
        )}
      </div>
    </div>
  );
}

function capture(e: ReactPointerEvent) {
  try { (e.currentTarget as Element).setPointerCapture(e.pointerId); } catch { /* pointeur déjà relâché */ }
}

/** Déplace `type` juste avant (ou après) `target` dans la liste. */
function move(list: LayoutItem[], type: string, target: string, after: boolean): LayoutItem[] {
  const item = list.find((i) => i.type === type);
  if (!item || type === target) return list;
  const rest = list.filter((i) => i.type !== type);
  const t = rest.findIndex((i) => i.type === target);
  if (t < 0) return list;
  rest.splice(after ? t + 1 : t, 0, item);
  return rest;
}
