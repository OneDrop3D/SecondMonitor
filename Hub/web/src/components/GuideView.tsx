import { useEffect, useState } from 'react';
import { api } from '../live';
import type { HubSettings, HubStatus, NodeStatus, OllamaModel, RaceSnapshot } from '../types';
import { linkStage } from '../status';
import { ConnectionHero } from './ConnectionView';
import { TestCard, WheelBindingCard } from './EngineerView';

/*
 * Guide de mise en route : s'ouvre au premier lancement (et à tout moment avec le bouton « ? »).
 * Chaque étape vérifie en direct ce qui marche déjà et dit quoi faire pour le reste.
 */

interface ModelsResponse { ok: boolean; error?: string | null; models: OllamaModel[]; nodes: NodeStatus[] }

const ETAPES = ['Bienvenue', 'Connexion', 'Ingénieur IA', 'Radio et volant', 'C\'est prêt'];

export function GuideView({ status, snapshot, onClose, onNavigate }: {
  status: HubStatus | null;
  snapshot: RaceSnapshot | null;
  onClose: () => void;
  onNavigate: (tab: 'connection' | 'ai' | 'settings') => void;
}) {
  const [step, setStep] = useState(0);
  const [models, setModels] = useState<ModelsResponse | null>(null);
  const [settings, setSettings] = useState<HubSettings | null>(null);

  const loadAi = () => {
    setModels(null);
    api<ModelsResponse>('/api/ai/models').then(setModels).catch((e) => setModels({ ok: false, error: (e as Error).message, models: [], nodes: [] }));
    api<HubSettings>('/api/settings').then(setSettings).catch(() => undefined);
  };
  useEffect(() => { if (step === 2) loadAi(); }, [step]);

  const stage = linkStage(status, snapshot);
  const go = (tab: 'connection' | 'ai' | 'settings') => { onNavigate(tab); onClose(); };
  const ai = settings?.ai;
  const hasModels = (models?.models.length ?? 0) > 0;
  const rolesOk = !!ai?.thinkModel;

  return (
    <div className="guide-backdrop" role="dialog" aria-modal="true" aria-label="Guide de mise en route">
      <div className="guide">
        <div className="guide-head">
          <b>Mise en route</b>
          <div className="guide-dots">
            {ETAPES.map((e, i) => <button key={e} className={`guide-dot ${i === step ? 'on' : i < step ? 'done' : ''}`} onClick={() => setStep(i)} title={e}>{i + 1}</button>)}
          </div>
          <span className="spacer" />
          <button className="btn" onClick={onClose}>Fermer</button>
        </div>

        <div className="guide-body">
          {step === 0 && (
            <>
              <h2>Bienvenue dans SecondMonitor</h2>
              <p>Deux applications travaillent ensemble :</p>
              <div className="guide-two">
                <div className="guide-card"><b>SecondMonitor Collecteur</b><span>Sur le <b>PC de jeu</b>. Il lit le jeu et envoie les données. Une petite fenêtre vous dit si tout va bien.</span></div>
                <div className="guide-card"><b>SecondMonitor</b> (cette fenêtre)<span>Sur l'ordinateur qui <b>affiche</b> : un Mac, un portable, ou le PC de jeu lui-même sur un deuxième écran.</span></div>
              </div>
              <p className="secondary">Le SecondMonitor d'origine n'est pas nécessaire. En quatre étapes : connexion, ingénieur IA, bouton radio, et c'est parti.</p>
            </>
          )}

          {step === 1 && (
            <>
              <h2>Connexion au PC de jeu</h2>
              <ConnectionHero stage={stage} status={status} snapshot={snapshot} />
              <ol className="guide-list">
                <li>Sur le PC de jeu, ouvrez <b>SecondMonitor Collecteur</b> (menu Démarrer). Autorisez le <b>réseau privé</b> si Windows le demande.</li>
                <li>Sur le même réseau, il est trouvé tout seul en quelques secondes : l'étape 1 ci-dessus passe au vert.</li>
                <li>Sinon, ouvrez l'écran <b>Connexion</b> et tapez l'adresse affichée dans la fenêtre du collecteur.</li>
                <li>Lancez une session : « Tout fonctionne » s'affiche dès que les données arrivent.</li>
              </ol>
              <button className="btn" onClick={() => go('connection')}>Ouvrir l'écran Connexion</button>
            </>
          )}

          {step === 2 && (
            <>
              <h2>Ingénieur IA</h2>
              <p className="secondary">Il fonctionne sur vos machines avec Ollama, gratuit et sans compte. Facultatif : tout le reste marche sans lui.</p>
              <div className="guide-checks">
                <Check ok={models == null ? null : models.ok} title="Ollama"
                  text={models == null ? 'Vérification…' : models.ok ? 'Ollama répond.' : 'Ollama ne répond pas. Installez-le depuis ollama.com/download, ouvrez-le, puis « Vérifier à nouveau ».'} />
                <Check ok={models == null || !models.ok ? null : hasModels} title="Modèle"
                  text={hasModels ? `${models!.models.length} modèle(s) : ${models!.models.slice(0, 3).map((m) => m.name).join(', ')}` : 'Aucun modèle. Dans un terminal : ollama pull lfm3-omni:2b (comprend la voix, réfléchit et parle).'} />
                <Check ok={!hasModels ? null : rolesOk} title="Rôles"
                  text={rolesOk ? `Réflexion : ${ai!.thinkModel}${ai!.listenModel ? ` · écoute : ${ai!.listenModel}` : ''}${ai!.speakModel ? ` · voix : ${ai!.speakModel}` : ''}` : 'Ouvrez l\'écran Ingénieur IA : les modèles sont choisis automatiquement.'} />
              </div>
              <div className="row">
                <button className="btn" onClick={loadAi}>Vérifier à nouveau</button>
                <button className="btn" onClick={() => go('ai')}>Ouvrir l'écran Ingénieur IA</button>
              </div>
              {hasModels && rolesOk && <TestCard />}
            </>
          )}

          {step === 3 && (
            <>
              <h2>Parler à l'ingénieur</h2>
              <ul className="guide-list">
                <li><b>Un appui</b> sur le bouton radio pour parler, <b>un second</b> pour envoyer. Si vous n'avez rien dit, rien n'est envoyé.</li>
                <li><b>Un appui pendant qu'il parle</b> le coupe.</li>
                <li>Le bouton radio est en bas à gauche de l'écran ; la barre d'espace marche aussi. Associez un bouton du volant ci-dessous.</li>
                <li>Exemples : « quel rythme pour rattraper la voiture devant ? », « la météo va évoluer ? », « mes pneus ? ».</li>
              </ul>
              <WheelBindingCard />
            </>
          )}

          {step === 4 && (
            <>
              <h2>C'est prêt</h2>
              <ul className="guide-list">
                <li>L'écran <b>Chronométrage</b> reprend celui de SecondMonitor d'origine. Les autres écrans sont dans la barre d'icônes à droite.</li>
                <li>En bas, la barre d'état montre la connexion au PC de jeu : cliquez dessus pour l'écran Connexion.</li>
                <li>L'ingénieur vous prévient de lui-même : pneus trop chauds, pluie, arrêt à prévoir… (réglable dans Ingénieur IA).</li>
                <li>Ce guide se rouvre avec le bouton <b>?</b> de la barre de droite.</li>
              </ul>
            </>
          )}
        </div>

        <div className="guide-foot">
          {step > 0 && <button className="btn" onClick={() => setStep(step - 1)}>Précédent</button>}
          <span className="spacer" />
          {step < ETAPES.length - 1
            ? <button className="btn primary" onClick={() => setStep(step + 1)}>Suivant</button>
            : <button className="btn primary" onClick={onClose}>Commencer</button>}
        </div>
      </div>
    </div>
  );
}

function Check({ ok, title, text }: { ok: boolean | null; title: string; text: string }) {
  return (
    <div className={`guide-check ${ok == null ? '' : ok ? 'ok' : 'todo'}`}>
      <span className="cx-tick">{ok == null ? '…' : ok ? '✓' : '!'}</span>
      <div><b>{title}</b><div className="secondary">{text}</div></div>
    </div>
  );
}
