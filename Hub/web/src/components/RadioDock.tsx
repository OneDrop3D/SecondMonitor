import { useEffect } from 'react';
import { useRadio } from '../radio';

/**
 * Radio flottante, visible sur tous les onglets : maintenir pour parler, état (écoute / réflexion), erreurs.
 * Le bouton du volant fonctionne même si cet indicateur est replié.
 */
export function RadioDock({ onOpen }: { onOpen: () => void }) {
  const r = useRadio();
  useEffect(() => {
    if (!r.error) return;
    const t = window.setTimeout(r.clearError, 8000);
    return () => window.clearTimeout(t);
  }, [r.error, r.clearError]);

  const label = r.phase === 'recording' ? 'Je vous écoute… appuyez pour envoyer' : r.phase === 'thinking' ? 'Réflexion…' : r.learning ? 'Appuyez sur un bouton du volant…' : null;
  return (
    <div className="dock" aria-live="polite">
      {r.error && <div className="dock-toast" role="alert" onClick={r.clearError}>{r.error}</div>}
      <div className={`dock-pill ${r.phase}`}>
        {label && <span className="dock-status">{label}</span>}
        {r.testMode && <span className="pill test-pill" title="Les réponses ne sont pas diffusées à la radio">TEST</span>}
        {r.binding && <span className={`dock-wheel ${r.boundPressed ? 'on' : ''}`} title={`Bouton ${r.binding.button + 1} du volant associé à la radio`}>◎</span>}
        <button
          className={`dock-mic ${r.phase}`}
          onClick={r.toggleTalk}
          disabled={!r.micAvailable || r.phase === 'thinking'}
          title={r.micAvailable ? (r.phase === 'recording' ? 'Appuyer pour envoyer' : 'Appuyer pour parler à l\'ingénieur IA') : 'Micro indisponible ici : ouvrez l\'adresse HTTPS (voir Réglages)'}
          aria-label={r.phase === 'recording' ? 'Envoyer' : 'Parler à l\'ingénieur IA'}
        >
          {r.phase === 'recording' ? '■' : '🎙'}
        </button>
        <button className="dock-open" onClick={onOpen} title="Ouvrir l'ingénieur IA" aria-label="Ouvrir l'ingénieur IA">›</button>
      </div>
    </div>
  );
}
