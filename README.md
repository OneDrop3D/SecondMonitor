# SecondMonitor — ingénieur de course sur un deuxième ordinateur

[![Construction](https://github.com/OneDrop3D/SecondMonitor/actions/workflows/build.yml/badge.svg)](https://github.com/OneDrop3D/SecondMonitor/actions/workflows/build.yml)
[![Licence MIT](https://img.shields.io/badge/licence-MIT-blue.svg)](LICENSE)

Le pilote joue sur son PC. Un autre ordinateur, par exemple un Mac, sert de **mur des stands**. Il affiche le classement, la stratégie, les temps, la télémétrie, la météo et un **ingénieur de course IA** qui parle à la radio. Le PC de jeu ne fait presque rien : il envoie les données, c'est tout.

```
 PC de jeu (Windows, Linux/SteamOS)          Mac / PC / Linux (hub)                 Écrans
 ┌──────────────────────────┐   réseau   ┌───────────────────────────┐   web   ┌──────────────────────┐
 │ jeu ─▶ SecondMonitor     │ ─────────▶ │ SecondMonitor Hub          │ ──────▶ │ navigateur du Mac,   │
 │        Collecteur        │  (local)   │ calculs, analyses, IA      │         │ iPad, téléphone,     │
 │ (très léger)             │            │ (Ollama, sur cette machine)│         │ PC de jeu (radio)    │
 └──────────────────────────┘            └───────────────────────────┘         └──────────────────────┘
```

Ce projet est basé sur [SecondMonitor](https://gitlab.com/winzarten/SecondMonitor) de Matus Celko (winzarten), sous licence MIT.
Les connecteurs de jeux d'origine sont réutilisés tels quels. L'application Windows d'origine est toujours dans ce dépôt ;
sa documentation est dans [docs/README-SecondMonitor-original.md](docs/README-SecondMonitor-original.md).

## Télécharger

Les fichiers sont dans les [**Releases**](https://github.com/OneDrop3D/SecondMonitor/releases). Rien à installer à côté : tout est inclus.

| Ordinateur | Fichier | Rôle |
|---|---|---|
| Mac (Apple Silicon) | `SecondMonitor-Hub-<version>-mac-arm64.zip` | le hub : écrans de l'ingénieur et IA |
| Mac (Intel) | `SecondMonitor-Hub-<version>-mac-x64.zip` | le hub |
| PC de jeu Windows | `SecondMonitor-Collecteur-<version>-windows-x64.zip` | envoie le jeu au hub |
| PC de jeu Linux / SteamOS | `SecondMonitor-Collecteur-<version>-linux-x64.tar.gz` | envoie le jeu au hub (avec Proton) |
| Hub sur Windows | `SecondMonitor-Hub-<version>-windows-x64.zip` | si l'ingénieur est sur un PC |
| Hub sur Linux | `SecondMonitor-Hub-<version>-linux-x64.tar.gz` | si l'ingénieur est sur Linux |

> Les versions marquées « préversion » n'ont pas encore été essayées sur toutes les machines. Signalez les problèmes dans les *Issues*.

## Démarrage en 3 étapes

1. **Sur le Mac** : décompressez `SecondMonitor-Hub-…-mac-arm64.zip` et glissez **SecondMonitor Hub** dans *Applications*, puis ouvrez-le.
   L'interface s'ouvre dans le navigateur (`http://localhost:8686`).
   - L'application n'est pas enregistrée auprès d'Apple. Au premier lancement, autorisez-la dans *Réglages Système › Confidentialité et sécurité › Ouvrir quand même*.
   - Si macOS dit qu'elle est endommagée, tapez `xattr -dr com.apple.quarantine "/Applications/SecondMonitor Hub.app"`.
2. **Sur le PC de jeu** : décompressez le collecteur et lancez **SecondMonitor-Collecteur.exe**. Si Windows le demande, autorisez le **réseau privé**.
   Pour qu'il démarre avec Windows : `SecondMonitor-Collecteur.exe --autostart on`.
   Pour tester la liaison sans jeu : `SecondMonitor-Collecteur.exe --demo`.
3. **Lancez le jeu.** Le hub trouve le PC tout seul sur le réseau local. Sinon : *Réglages › Adresse du PC de jeu*.

Pour essayer sans rien d'autre : *Réglages › Lancer la démo* dans le hub.

## Ce que fait le hub

- **Course** : classement de votre catégorie, avec le trafic des autres catégories à moins de 5 s, écarts, rythme, pneus, arrêts estimés de chaque adversaire, carte apprise automatiquement, duels (« contact dans 3 tours »).
- **Tours & prévisions** : temps au tour comparés avec la projection du relais, dégradation, usure des pneus et carburant par tour avec projection, carburant et usure à l'arrivée.
- **Vitesses & trajectoires** : virages et lignes droites détectés automatiquement, Vmax et Vmin de tout le plateau, profils de vitesse, trajectoires comparées virage par virage.
- **Micro-secteurs** : 24 tronçons chronométrés pour tous (violet, vert, jaune), delta en direct, meilleur tour théorique.
- **Voiture** : pneus (surface, carcasse, pressions, usure), freins, moteur (eau, huile), dégâts, avec des graphiques agrandissables. Les plages bleu / vert / rouge se règlent par jeu et par voiture.
- **Météo** : températures, pluie, humidité de la piste, vent, historique et tendance pour les 30 minutes suivantes.
- **Stratégies** : relais de tous les pilotes, gommes, prochains arrêts estimés avec leur niveau de confiance.
- Une interface pensée pour toutes les tailles d'écran (Mac, iPad, téléphone), en thème sombre ou clair.

## L'ingénieur IA

- Il fonctionne **100 % en local** avec [Ollama](https://ollama.com), sans compte, sans abonnement et sans envoyer de données.
- On lui parle à la voix, avec le bouton radio de l'écran, la barre d'espace ou **un bouton du volant**. Un appui pour parler, un appui pour envoyer.
  Appuyer pendant qu'il parle le coupe.
- Les chiffres sont toujours calculés par le hub : l'IA ne les invente pas. Pour les questions courantes, la réponse calculée est dite directement.
- Il **prend la parole de lui-même** quand une décision se présente : pluie, pneus pluie, relais qu'on peut allonger (seulement si ça fait gagner un arrêt), box dans deux tours, voiture qui revient, surchauffe.
- Il garde une **mémoire de la course** : vos retours (« l'arrière glisse ») et les décisions prises ensemble.
- On peut changer de modèle à la voix (« on passe à mistral ») et répartir les modèles sur plusieurs machines du réseau, par exemple un Raspberry Pi.
- Un **mode test** fonctionne sans jeu, avec un diagnostic de toute la chaîne : Ollama, réflexion, voix, écoute, micro, volant.

Mise en route : installez Ollama sur le Mac, téléchargez un modèle (par exemple `ollama pull lfm3-omni:2b`), puis choisissez-le dans l'onglet *Ingénieur IA*.

## Jeux pris en charge

Le Mans Ultimate · Assetto Corsa Competizione · rFactor 2 · Assetto Corsa · Automobilista 2 · Project CARS 1 et 2 · RaceRoom ·
rFactor / Automobilista 1 · F1 2019, 2020, 2021, 24 · Forza Motorsport.

Linux / SteamOS : F1 et Forza fonctionnent avec la version Linux du collecteur. Les autres jeux tournent avec Proton, et le collecteur
Windows se lance dans le même environnement que le jeu (`lancer-avec-proton.sh`). Les détails sont dans [Collector/LISEZ-MOI.md](Collector/LISEZ-MOI.md).

## Réseau et sécurité

- Tout reste sur votre réseau local. Le seul service extérieur est Ollama, sur vos propres machines.
- Ports utilisés :
  - `8686` (interface, HTTP) ;
  - `8687` (interface en HTTPS, nécessaire au micro et au volant depuis un autre appareil que le Mac) ;
  - `52642` en UDP (collecteur vers hub, même protocole que le plugin « Data Broadcast Server » de SecondMonitor).
- Le certificat HTTPS est créé par le hub. Le navigateur affiche un avertissement la première fois : acceptez-le.
- **L'interface du hub n'a pas de mot de passe.** N'importe quel appareil de votre réseau local peut l'ouvrir. Utilisez-la sur un réseau de confiance
  (maison, VPN comme Tailscale), pas sur un Wi-Fi public. L'arrêt du hub n'est possible que depuis le Mac lui-même.
- Le collecteur et le hub n'ont pas besoin d'être sur le même sous-réseau : indiquez l'adresse du PC dans le hub.

## Construire

Tout se construit depuis un Mac, sans machine Windows ni Linux. Il faut le SDK .NET 10 (`brew install dotnet`) et Node.js 20 (`brew install node@20`).

```sh
./construire.sh tous      # ou : mac, win, linux   →  archives dans dist/
```

- **OneDrop3D** : le produit *SecondMonitor* du Publieur lance ces mêmes commandes. Il change la version dans les trois fichiers concernés,
  envoie le code sur ce dépôt et peut aussi faire construire sur GitHub.
- **GitHub Actions** : le workflow [build.yml](.github/workflows/build.yml) lance les tests et construit tous les systèmes sur chaque étiquette `v*`,
  ou à la demande (*Actions › Construction › Run workflow*). Les archives sont dans les artefacts `secondmonitor-mac`, `secondmonitor-windows` et `secondmonitor-linux`.

Développement :

| Dossier | Contenu |
|---|---|
| `Hub/` | le hub : serveur .NET 10 (`src/`), interface React + TypeScript (`web/`), tests (`tests/`). Voir [Hub/README.md](Hub/README.md). |
| `Collector/` | le collecteur du PC de jeu. Voir [Collector/LISEZ-MOI.md](Collector/LISEZ-MOI.md). |
| `Connectors/`, `Foundation/`, `Remote/` | code de SecondMonitor d'origine, réutilisé sans modification par le hub et le collecteur |
| `Applications/`, `Telemetry/`, `Timing/`, `Rating/`… | l'application Windows d'origine (WPF) |
| `packaging/` | icônes |
| `construire.sh` | construction de toutes les archives |

```sh
dotnet test Hub/SecondMonitorHub.sln                                            # tests du hub
cd Hub/web && npm install && npm run dev                                        # interface en direct (proxy vers le hub)
dotnet run --project Hub/src/SecondMonitor.Hub -- --demo                        # hub avec la course de démonstration
dotnet run --project Collector/src -- --demo                                    # collecteur qui envoie la démo
```

## Crédits et licence

- [SecondMonitor](https://gitlab.com/winzarten/SecondMonitor) © 2023 Matus Celko, licence MIT : connecteurs de jeux, modèle de données, protocole réseau, application Windows.
- SecondMonitor Hub, SecondMonitor Collecteur et les modifications © 2026 OneDrop3D, licence MIT.
- Logos des marques automobiles : [Simple Icons](https://simpleicons.org) (CC0). Les marques appartiennent à leurs propriétaires.

Voir [LICENSE](LICENSE).
