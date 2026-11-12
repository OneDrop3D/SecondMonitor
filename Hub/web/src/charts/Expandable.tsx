import { useEffect, useState, type ReactNode } from 'react';

/**
 * Bloc agrandissable : un bouton ouvre le contenu en plein écran (graphique plus haut, chiffres plus gros).
 * `render(height, expanded)` reçoit la hauteur disponible.
 */
export function Expandable({ title, subtitle, height, render, actions }: { title: string; subtitle?: string; height: number; render: (height: number, expanded: boolean) => ReactNode; actions?: ReactNode }) {
  const [open, setOpen] = useState(false);
  const [vh, setVh] = useState(() => window.innerHeight);
  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && setOpen(false);
    const onResize = () => setVh(window.innerHeight);
    window.addEventListener('keydown', onKey);
    window.addEventListener('resize', onResize);
    document.body.style.overflow = 'hidden';
    return () => { window.removeEventListener('keydown', onKey); window.removeEventListener('resize', onResize); document.body.style.overflow = ''; };
  }, [open]);

  return (
    <div className="chart-box">
      <div className="chart-box-head">
        <span className="chart-title">{title}</span>
        {subtitle && <span className="muted small">{subtitle}</span>}
        <span className="spacer" />
        {actions}
        <button className="icon-btn" onClick={() => setOpen(true)} title="Agrandir" aria-label={`Agrandir : ${title}`}>⤢</button>
      </div>
      {render(height, false)}
      {open && (
        <div className="modal-backdrop" onClick={() => setOpen(false)}>
          <div className="modal" role="dialog" aria-label={title} onClick={(e) => e.stopPropagation()}>
            <div className="card-head">
              <span className="card-title">{title}</span>
              {subtitle && <span className="muted">{subtitle}</span>}
              <span className="spacer" />
              {actions}
              <button className="close" onClick={() => setOpen(false)} aria-label="Fermer">✕</button>
            </div>
            <div className="card-body modal-body">{render(Math.max(300, vh - 190), true)}</div>
          </div>
        </div>
      )}
    </div>
  );
}
