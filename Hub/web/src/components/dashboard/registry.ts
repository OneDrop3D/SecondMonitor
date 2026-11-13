import type { ReactNode } from 'react';
import type { ClassView } from '../../classView';
import type { RadioMessage } from '../../radio';
import type { DriverRow, RaceEvent, RaceSnapshot } from '../../types';
import type { Notice } from './notices';

/*
 * Registre des blocs de l'accueil (tableau de bord personnalisable).
 *
 * Ajouter un bloc depuis un autre module (musique, carnet…) :
 *
 *   import { registerBlock } from './dashboard/registry';
 *   registerBlock({
 *     type: 'musique', title: 'Musique', icon: 'music', description: 'Lecteur et file d’attente',
 *     size: { w: 4, h: 5 }, medium: { w: 3, h: 5 },
 *     render: ({ ctx }) => <MonBloc snapshot={ctx.snapshot} />,
 *   });
 *
 * puis importer ce module une fois (par exemple `import './components/MonBloc';` dans App.tsx).
 * Le bloc apparaît dans « Disposition › Ajouter un bloc » ; il n'est pas ajouté d'office aux dispositions existantes
 * (sauf `addByDefault: true`, pris en compte pour les nouvelles dispositions et après « Réinitialiser »).
 */

export interface DashboardContext {
  snapshot: RaceSnapshot;
  classColor: (classId: string) => string;
  onSelect: (d: DriverRow) => void;
  classView: ClassView;
  setClassView: (v: ClassView) => void;
  /** Échanges radio reçus par cet écran (les plus récents à la fin), avec l'audio quand il est disponible. */
  radio: RadioMessage[];
  /** Événements de course du hub (les plus anciens d'abord). */
  events: RaceEvent[];
  /** Notifications déjà fermées ou expirées (fil d'événements). */
  noticeHistory: Notice[];
}

export interface BlockSize {
  /** Largeur en colonnes, hauteur en rangées, nombre de colonnes de la grille (12, 6 ou 1). */
  w: number;
  h: number;
  cols: number;
  /** Taille utile du contenu (px, sous l'en-tête du bloc). */
  bodyWidth: number;
  bodyHeight: number;
}

export interface BlockProps {
  ctx: DashboardContext;
  size: BlockSize;
}

export interface BlockDef {
  type: string;
  title: string;
  /** Nom d'icône de Timing.tsx (Icon), ex. 'timing', 'radio', 'flag'. */
  icon: string;
  description: string;
  /** Taille par défaut sur grand écran (12 colonnes) et sur tablette (6 colonnes). */
  size: { w: number; h: number };
  medium?: { w: number; h: number };
  minW?: number;
  minH?: number;
  /** Présent dans la disposition par défaut (sinon : à ajouter depuis la liste). */
  addByDefault?: boolean;
  /** Ordre dans la disposition par défaut (plus petit = plus haut). */
  order?: number;
  /** Petit texte ou badge dans l'en-tête du bloc (compteur…). */
  badge?: (p: BlockProps) => ReactNode;
  render: (p: BlockProps) => ReactNode;
}

const registry = new Map<string, BlockDef>();
const listeners = new Set<() => void>();

export function registerBlock(def: BlockDef) {
  registry.set(def.type, def);
  listeners.forEach((l) => l());
}

export function getBlock(type: string): BlockDef | undefined {
  return registry.get(type);
}

export function allBlocks(): BlockDef[] {
  return [...registry.values()].sort((a, b) => (a.order ?? 100) - (b.order ?? 100));
}

export function onRegistryChange(l: () => void): () => void {
  listeners.add(l);
  return () => { listeners.delete(l); };
}
