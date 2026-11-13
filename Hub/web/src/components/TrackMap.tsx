import { useMemo, useState } from 'react';
import { gap } from '../format';
import type { DriverRow, RaceSnapshot } from '../types';
import { ClassSwatch } from './Common';
import { useWidth } from '../charts/useSize';
import { visibleDrivers, type ClassView } from '../classView';

const PAD = 18;
const MAX_H = 440;

interface Props {
  snapshot: RaceSnapshot;
  classColor: (classId: string) => string;
  onSelect: (driver: DriverRow) => void;
  classView?: ClassView;
}

interface Placed {
  d: DriverRow;
  x: number;
  y: number;
}

/**
 * Carte de piste : tracé appris automatiquement (un tour complet suffit), voitures colorées par catégorie.
 * Tant que le tracé n'est pas connu, on affiche un « cercle de chronométrage » (position sur le tour).
 */
export function TrackMap({ snapshot, classColor, onSelect, classView = 'all' }: Props) {
  const [hover, setHover] = useState<{ p: Placed; cx: number; cy: number } | null>(null);
  const outline = snapshot.track.outline;
  const { ref, width } = useWidth<HTMLDivElement>(400);
  const W = Math.max(240, Math.round(width));

  const frame = useMemo(() => {
    if (!outline || outline.length < 10) return null;
    const xs = outline.map((p) => p[0]);
    const zs = outline.map((p) => p[1]);
    const minX = Math.min(...xs), maxX = Math.max(...xs), minZ = Math.min(...zs), maxZ = Math.max(...zs);
    const scale = Math.min((W - PAD * 2) / Math.max(1, maxX - minX), (MAX_H - PAD * 2) / Math.max(1, maxZ - minZ));
    const h = Math.max(160, (maxZ - minZ) * scale + PAD * 2);
    const ox = (W - (maxX - minX) * scale) / 2;
    const tx = (x: number) => ox + (x - minX) * scale;
    const ty = (z: number) => PAD + (z - minZ) * scale;
    const path = outline.map((p, i) => `${i === 0 ? 'M' : 'L'}${tx(p[0]).toFixed(1)},${ty(p[1]).toFixed(1)}`).join(' ') + ' Z';
    return { tx, ty, h, path };
  }, [outline, W]);

  const H = frame?.h ?? Math.min(MAX_H, W * 0.8);

  const { rows: shown, traffic } = visibleDrivers(snapshot, classView);
  const placed: Placed[] = shown.map((d) => {
    if (frame && outline) {
      const useWorld = d.inPits && d.x != null && d.z != null && snapshot.track.worldPositionsValid && snapshot.track.outlineSource === 'jeu';
      if (useWorld) return { d, x: frame.tx(d.x!), y: frame.ty(d.z!) };
      const f = ((d.lapFraction % 1) + 1) % 1;
      const pos = f * (outline.length - 1);
      const i = Math.floor(pos);
      const t = pos - i;
      const a = outline[i], b = outline[Math.min(outline.length - 1, i + 1)];
      return { d, x: frame.tx(a[0] + (b[0] - a[0]) * t), y: frame.ty(a[1] + (b[1] - a[1]) * t) };
    }
    // Cercle de chronométrage : départ en haut, sens horaire.
    const r = Math.min(W, H) / 2 - 30;
    const angle = d.lapFraction * Math.PI * 2 - Math.PI / 2;
    const inset = d.inPits ? 18 : 0;
    return { d, x: W / 2 + (r - inset) * Math.cos(angle), y: H / 2 + (r - inset) * Math.sin(angle) };
  });

  const player = placed.find((p) => p.d.isPlayer);
  const neighbourIds = new Map<string, number>(); // id → décalage vertical de l'étiquette
  if (player && snapshot.sessionType === 'Race') {
    // Voitures directement devant / derrière sur la piste (toutes catégories) : on les étiquette.
    const others = placed.filter((p) => !p.d.isPlayer && !p.d.inPits && p.d.gapToPlayer != null);
    const ahead = others.filter((p) => p.d.gapToPlayer! < 0).sort((a, b) => b.d.gapToPlayer! - a.d.gapToPlayer!)[0];
    const behind = others.filter((p) => p.d.gapToPlayer! > 0).sort((a, b) => a.d.gapToPlayer! - b.d.gapToPlayer!)[0];
    // Étiquette des voisins seulement s'ils ne chevauchent pas « VOUS » (sinon : infobulle au survol).
    const far = (p: Placed) => Math.hypot(p.x - player.x, p.y - player.y) > 46;
    if (ahead && far(ahead)) neighbourIds.set(ahead.d.id, -15);
    if (behind && far(behind)) neighbourIds.set(behind.d.id, 15);
  }

  const classes = [...new Map(snapshot.drivers.map((d) => [d.classId, d.className ?? d.classId])).entries()];
  const drawOrder = [...placed].sort((a, b) => Number(a.d.isPlayer) - Number(b.d.isPlayer));
  // Tracé de bibliothèque pas encore calé sur le jeu : voitures et ligne de départ en pointillé (rien d'affirmé).
  const estimated = snapshot.track.outlineSource === 'bibliothèque' && snapshot.track.outlineCalibration === 'estimé';
  const start = frame && outline ? { x: frame.tx(outline[0][0]), y: frame.ty(outline[0][1]) } : { x: W / 2, y: 30 - 6 };

  return (
    <div className="card">
      <div className="card-head">
        <span className="card-title">Carte</span>
        <span className="muted">{snapshot.track.name}{snapshot.track.layout ? ` · ${snapshot.track.layout}` : ''}</span>
        {snapshot.track.outlineSource === 'bibliothèque' && (
          <span className="pill small-pill" title={`Tracé réel (${snapshot.track.outlineCredit ?? ''}) en attendant celui appris en roulant. ${estimated
            ? 'Ligne de départ et sens estimés : positions approximatives jusqu\'au calage sur les positions du jeu.'
            : 'Ligne de départ et sens calés sur les positions du jeu.'}`}>{estimated ? 'tracé réel · départ estimé' : 'tracé réel'}</span>
        )}
        <span className="spacer" />
        {traffic.size > 0 && <span className="pill" title="Voitures d'autres catégories à moins de 5 s">{traffic.size} en trafic</span>}
        {!frame && <span className="pill" title="La carte se dessine après un tour complet sans passage au stand">Tracé en apprentissage</span>}
      </div>
      <div ref={ref} className="map-wrap">
      <svg className="map-svg" width={W} height={H} role="img" aria-label="Position des voitures sur la piste">
        {frame ? (
          <>
            <path d={frame.path} fill="none" stroke="var(--surface-3)" strokeWidth={12} strokeLinejoin="round" />
            <path d={frame.path} fill="none" stroke="var(--axis)" strokeWidth={2} strokeLinejoin="round" />
          </>
        ) : (
          <circle cx={W / 2} cy={H / 2} r={Math.min(W, H) / 2 - 30} fill="none" stroke="var(--surface-3)" strokeWidth={12} />
        )}
        <rect x={start.x - 2} y={start.y - 9} width={4} height={18} fill={estimated ? 'none' : 'var(--text-primary)'} stroke={estimated ? 'var(--text-primary)' : 'none'} strokeDasharray={estimated ? '2 2' : undefined} rx={1} />

        {drawOrder.map((p) => {
          const isP = p.d.isPlayer;
          const label = isP || neighbourIds.has(p.d.id);
          return (
            <g
              key={p.d.id}
              style={{ cursor: 'pointer' }}
              onClick={() => onSelect(p.d)}
              onPointerMove={(e) => setHover({ p, cx: e.clientX, cy: e.clientY })}
              onPointerLeave={() => setHover(null)}
              opacity={p.d.inPits ? 0.55 : estimated ? 0.7 : 1}
            >
              <circle cx={p.x} cy={p.y} r={14} fill="transparent" />
              {isP && <circle cx={p.x} cy={p.y} r={11} fill="none" stroke="var(--accent)" strokeWidth={2} />}
              <circle cx={p.x} cy={p.y} r={isP ? 7.5 : 6} fill={classColor(p.d.classId)} stroke="var(--surface-1)" strokeWidth={2} />
              {label && (
                <text
                  x={p.x > W - 120 ? p.x - 13 : p.x + 13}
                  y={p.y + 4 + (neighbourIds.get(p.d.id) ?? 0)}
                  textAnchor={p.x > W - 120 ? 'end' : 'start'} fontSize={11} fontWeight={isP ? 700 : 500} fill="var(--text-primary)" stroke="var(--surface-1)" strokeWidth={3} paintOrder="stroke">
                  {isP ? 'VOUS' : `${p.d.number ?? ''} ${p.d.shortName}`} {!isP && p.d.gapToPlayer != null ? gap(p.d.gapToPlayer) : ''}
                </text>
              )}
            </g>
          );
        })}
      </svg>
      </div>
      {classes.length > 1 && (
        <div className="map-legend">
          {classes.map(([id, name]) => (
            <span key={id} className="row" style={{ gap: 6 }}><ClassSwatch color={classColor(id)} />{name}</span>
          ))}
          <span className="row" style={{ gap: 6 }}><span className="swatch" style={{ border: '2px solid var(--accent)', borderRadius: '50%' }} />Vous</span>
        </div>
      )}
      {hover && (
        <div className="tooltip" style={{ left: hover.cx + 14, top: hover.cy + 10 }}>
          <div className="tv">P{hover.p.d.position} · {hover.p.d.number != null ? `#${hover.p.d.number} ` : ''}{hover.p.d.name}</div>
          <div className="secondary">{hover.p.d.className} · {hover.p.d.car}</div>
          {hover.p.d.gapToPlayer != null && !hover.p.d.isPlayer && <div>Écart avec vous : <b className="tnum">{gap(hover.p.d.gapToPlayer)}</b></div>}
          {hover.p.d.inPits && <div>Au stand</div>}
        </div>
      )}
    </div>
  );
}
