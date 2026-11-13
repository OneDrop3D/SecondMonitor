# SecondMonitor : chronométrage, stratégie et ingénieur de course IA sur un deuxième écran

[![Construction](https://github.com/OneDrop3D/SecondMonitor/actions/workflows/build.yml/badge.svg)](https://github.com/OneDrop3D/SecondMonitor/actions/workflows/build.yml)
[![Licence MIT](https://img.shields.io/badge/licence-MIT-blue.svg)](LICENSE)

Pendant que vous pilotez, SecondMonitor affiche sur un autre écran tout ce qu'un ingénieur de course regarde :
- le chronométrage complet (secteurs violets et verts, meilleurs tours, écarts) ;
- l'état de votre voiture (pneus, freins, carburant, moteur) ;
- la carte du circuit ;
- la stratégie et la météo ;
- un **ingénieur IA** qui vous parle à la radio.

![L'écran Chronométrage de SecondMonitor](docs/capture-chronometrage.png)

> **Faut-il installer le SecondMonitor d'origine (celui de Matus Celko) ?**
> **Non.** Ce projet en est une nouvelle version complète et autonome. Il reprend son écran et ses connecteurs de jeux,
> et y ajoute les nouveautés. Installez seulement le fichier ci-dessous, rien d'autre (.NET est inclus).
> Si vous avez déjà l'ancien SecondMonitor sur le PC de jeu, fermez-le avant d'ouvrir le collecteur : ils utilisent le même port.

## Deux applications, installées ensemble

| Application | Où l'ouvrir | Ce qu'elle fait |
|---|---|---|
| **SecondMonitor** | Sur l'ordinateur qui **affiche** : un Mac, un PC portable, ou le PC de jeu lui-même sur un deuxième écran | Affiche le chronométrage, la voiture, la stratégie, la météo et l'ingénieur IA |
| **SecondMonitor Collecteur** | Sur le **PC de jeu** | Lit le jeu et envoie les données. Il tient dans une petite fenêtre et ne consomme presque rien |

Une seule installation met les **deux** applications sur l'ordinateur. Vous ouvrez celle qui correspond à son rôle.

```
   PC de jeu                                     Ordinateur qui affiche (ou le même PC, 2ᵉ écran)
 ┌────────────────────────────┐   réseau local  ┌─────────────────────────────────────┐
 │ jeu ─▶ SecondMonitor        │ ───────────────▶│ SecondMonitor                        │
 │        Collecteur           │                 │ chronométrage · stratégie · IA       │──▶ iPad / téléphone
 └────────────────────────────┘                 └─────────────────────────────────────┘    (navigateur, facultatif)
```

## Télécharger

Prenez le fichier de votre système dans les [**Releases**](https://github.com/OneDrop3D/SecondMonitor/releases) :

| Système | Fichier |
|---|---|
| **Windows** | `SecondMonitor-<version>-windows-x64-installation.exe` (installateur)<br>ou `SecondMonitor-<version>-windows-x64.zip` (sans installation) |
| **Mac** (Apple Silicon : M1, M2, M3, M4…) | `SecondMonitor-<version>-mac-arm64.dmg` |
| **Mac** (Intel) | `SecondMonitor-<version>-mac-x64.dmg` |
| **Linux**, SteamOS | `SecondMonitor-<version>-linux-x64.tar.gz` |

Les versions marquées « préversion » n'ont pas encore été essayées sur toutes les machines. Signalez les problèmes dans les [*Issues*](https://github.com/OneDrop3D/SecondMonitor/issues).

## Installer

### Windows
1. Lancez `SecondMonitor-…-installation.exe`. Windows peut afficher « Windows a protégé votre ordinateur » (l'application n'est pas signée par un éditeur payant) : cliquez **Informations complémentaires › Exécuter quand même**.
2. Choisissez ce que vous installez : les deux applications, seulement le collecteur (PC de jeu) ou seulement SecondMonitor.
3. Elles apparaissent dans le menu Démarrer : **SecondMonitor** et **SecondMonitor Collecteur**.

Au premier lancement du collecteur, Windows demande l'accès au réseau : choisissez **Réseau privé**.

### Mac
1. Ouvrez le `.dmg` et glissez **SecondMonitor** et **SecondMonitor Collecteur** dans *Applications*.
2. Au premier lancement, macOS peut bloquer l'application (elle n'est pas enregistrée auprès d'Apple). Allez dans *Réglages Système › Confidentialité et sécurité*, puis **Ouvrir quand même**.
   Une seule fois : les mises à jour suivantes s'installent depuis SecondMonitor, sans ce passage.
   Si macOS dit qu'elle est endommagée : `xattr -dr com.apple.quarantine /Applications/SecondMonitor*.app`.
3. Acceptez l'accès au **réseau local** et, pour la radio, au **micro**.

Sur Mac, le collecteur sert aux jeux sur console (F1 et Forza envoient leurs données par le réseau).

### Linux / SteamOS
```sh
tar xzf SecondMonitor-*-linux-x64.tar.gz
./secondmonitor/installer.sh
```
Les deux applications s'ajoutent au menu (rien n'est demandé en mot de passe, tout va dans `~/.local/share/secondmonitor`).
Le script vérifie les bibliothèques (WebKitGTK 4.1, libnotify, ICU, OpenSSL) et donne la commande exacte pour apt, dnf, pacman ou zypper
s'il en manque. Sans WebKitGTK, SecondMonitor s'ouvre dans le navigateur et le collecteur dans un terminal.
Options : `--demarrage-auto` (collecteur à l'ouverture de session), `--pare-feu` (UDP 52642, TCP 8686-8687 avec ufw ou firewalld),
`--verifier`. Désinstaller : `~/.local/share/secondmonitor/desinstaller.sh` (`--tout` retire aussi les réglages).
Détails, jeux compatibles (F1 et Forza en natif, les autres via Proton) et check-list de test : [packaging/linux/LISEZ-MOI-LINUX.md](packaging/linux/LISEZ-MOI-LINUX.md).

## Mettre à jour

SecondMonitor vérifie tout seul au démarrage puis toutes les 6 heures (réglable, ou désactivable, dans *Réglages › Mises à jour*).
Quand une version sort, un petit bandeau « Mise à jour x.y.z disponible — **Installer** » apparaît. Un clic suffit :

1. le fichier de votre système est téléchargé depuis les releases de ce dépôt, puis vérifié (taille et SHA-256) ;
2. SecondMonitor se ferme, **les deux applications** (SecondMonitor et le collecteur) sont remplacées, puis rouvertes ;
3. si la nouvelle version ne démarre pas, l'ancienne est remise en place et la raison s'affiche au démarrage suivant.

Sur le PC de jeu, la fenêtre du **collecteur** a le même bouton (jamais de vérification pendant que le jeu envoie des données).

- **Mac** : la mise à jour ne passe pas par le navigateur, donc pas de quarantaine : plus de *Réglages › Confidentialité et sécurité ›
  Ouvrir quand même* après la première installation. Pour que macOS ne redemande pas non plus le micro à chaque version, les
  applications doivent être signées avec une identité stable (voir `packaging/mac/creer-identite-signature.sh`).
- **Windows** : l'installateur se lance en silencieux (une copie de l'installation actuelle est gardée jusqu'au démarrage réussi) ;
  la version sans installation (.zip) remplace son dossier.
- **Linux** : l'archive remplace `~/.local/share/secondmonitor` puis `installer.sh --silencieux` rafraîchit les raccourcis.

Les versions sont annoncées par le manifeste `secondmonitor.json` du dépôt [OneDrop3D/mises-a-jour](https://github.com/OneDrop3D/mises-a-jour)
(même format que Multibook et ClickDeck), écrit par OneDrop3D à chaque publication. Seuls les fichiers des releases de
OneDrop3D/SecondMonitor, en https et avec leur SHA-256, sont acceptés.

## Démarrer

1. **Sur le PC de jeu**, ouvrez **SecondMonitor Collecteur**. Sa fenêtre montre :
   - si SecondMonitor est connecté ;
   - le jeu détecté ;
   - le débit envoyé ;
   - l'adresse du PC.

   Pour tester sans jeu, cochez *Démonstration*.
2. **Sur l'ordinateur qui affiche**, ouvrez **SecondMonitor**. Il trouve le collecteur tout seul sur le réseau local.
   Sinon, saisissez l'adresse du PC de jeu dans *Réglages*.
3. **Lancez une session** dans le jeu : le chronométrage se remplit.

Au premier lancement, SecondMonitor ouvre un **guide de mise en route** : connexion, ingénieur IA, bouton radio. Il se rouvre avec le bouton **?** de la barre de droite.
L'écran **Connexion** et la barre d'état en bas montrent à tout moment si le collecteur, le jeu et les données arrivent. Une notification apparaît dès que les données arrivent.

**Un seul PC avec deux écrans** ? Ouvrez les deux applications sur ce PC et placez la fenêtre SecondMonitor sur le deuxième écran.
**Un iPad ou un téléphone** ? Ouvrez dans son navigateur l'adresse indiquée dans *Réglages* de SecondMonitor.

## Les écrans de SecondMonitor

L'écran d'accueil reprend celui de SecondMonitor d'origine. Les autres écrans sont dans la barre d'icônes à droite.

- **Chronométrage** :
  - en haut : session, météo, deltas en direct (meilleur tour, tour précédent), meilleurs secteurs, meilleur tour, temps restant ;
  - au centre : le tableau de chronométrage (dernier tour, S1/S2/S3 en violet ou vert, meilleur tour, tour en cours, stands, pneus, prochain arrêt estimé, écart) ;
  - en bas : les quatre pneus (surface, carcasse, usure, freins, pressions), le carburant, le moteur, la carte, les duels et la stratégie.
- **Classement & duels** : écarts, rythme, contact prévu avec la voiture devant ou derrière.
- **Tours & prévisions** : temps au tour de tous les pilotes, dégradation, projection du relais, carburant et usure à l'arrivée.
- **Vitesses & trajectoires** : Vmax et Vmin de tout le plateau, virage par virage, et trajectoires comparées.
- **Micro-secteurs** : 24 tronçons chronométrés, meilleur tour théorique.
- **Voiture** : températures et pressions détaillées, graphiques agrandissables, plages bleu / vert / rouge réglables par jeu et par voiture.
- **Météo** : historique et tendance pour les 30 minutes suivantes.
- **Stratégies** : relais et arrêts estimés de chaque pilote.
- **Ingénieur IA** et **Réglages**.

En course multi-catégories, le tableau montre votre catégorie et les autres voitures à moins de 5 s de vous. Le bouton *Toutes* affiche tout le plateau.

## L'ingénieur IA

- Il fonctionne **100 % en local** avec [Ollama](https://ollama.com), sans compte, sans abonnement et sans envoyer de données.
- Pour lui parler, un appui sur le bouton radio pour parler, un second pour envoyer. Un appui pendant qu'il parle le coupe. Le bouton radio peut être :
  - le bouton de l'écran, ou la barre d'espace ;
  - un bouton du volant branché sur l'ordinateur qui affiche ;
  - une **touche du clavier ou un bouton du volant du PC de jeu** : réglage dans la fenêtre du collecteur ; marche même pendant le jeu, sous Windows ;
  - une touche **ClickDeck**.
- **Voix et micro sur le PC de jeu** : dans la fenêtre du collecteur, carte *Radio de l'ingénieur*, cochez « Entendre l'ingénieur sur ce PC »
  (et choisissez le casque) et/ou « Parler avec le micro de ce PC » (et choisissez le micro). Le bouton radio du PC de jeu et ClickDeck enregistrent
  alors le micro du PC de jeu. *Réglages › Voix de l'ingénieur* dans SecondMonitor : voix sur le PC de jeu seul (par défaut), sur les écrans, ou les deux.
  La carte *Radio* de l'écran *Ingénieur IA* indique quand le PC de jeu a la voix ou le micro.
- Les chiffres sont calculés par SecondMonitor : l'IA ne les invente pas.
- Il calcule le **rythme** : le vôtre, celui qu'il faut pour rattraper la voiture devant ou pour gagner, et ce que donne le rythme actuel à l'arrivée.
  Il donne aussi la tendance météo des 30 minutes suivantes. Il répond à plusieurs questions posées d'un coup.
- Il calcule la **stratégie d'arrêt** :
  - la position à la sortie si vous rentrez maintenant ;
  - l'**undercut** et l'**overcut** sur la voiture devant ;
  - la **menace d'undercut** de la voiture derrière.

  Perte au stand et gain des pneus neufs sont mesurés sur tout le plateau. Il connaît le vocabulaire de course.
- Il suit les **dégâts** pièce par pièce (F1 : ailerons, fond plat, éléments moteur… ; ACC et LMU : zones, suspension, crevaisons), mesure ce qu'ils coûtent au tour et dit si réparer vaut un arrêt.
- Il s'adapte à la session : la voiture en essais (compte rendu de tour : pressions, carrossage), le tour lancé en qualification, le déroulé en course.
- Il prend la parole de lui-même quand une décision se présente :
  - **pneus trop chauds sur tout un tour** ou en surchauffe depuis plus de 30 s, ou encore froids au tour de formation ;
  - pluie, relais à allonger, box dans deux tours, voiture qui revient, freins ou moteur trop chauds.
- **Alertes de pilotage** dites tout de suite, en une phrase : limites de piste (trop large ou coupé, points avant pénalité), temps annulé,
  pénalité, moteur coupé au départ, et **usure brutale** d'un pneu analysée (virage, phase, cause probable, réglage à essayer avec les valeurs actuelles).
- Il connaît la **voiture** : catégorie (Hypercar LMH ou LMDh, LMP2, LMGT3, GTE, GT3, GT4, F1), ce qui se règle en roulant et les **réglages actuels**
  lus dans le jeu (répartition, migration, TC, ABS, cartographie, regen, énergie ; Le Mans Ultimate, et en partie F1 24 et ACC).
- **Pédales et volant** enregistrés sur chaque tour : bloc *Pédales et volant* de l'accueil et analyse du pilotage à la voix.
- Ton radio : réponses courtes, « Copy » en anglais (réglable). Si l'IA réfléchit plus de 2,5 s, il répond « Copy, je reviens vers vous » et vous pouvez
  poser d'autres questions ; les questions pour l'IA passent une à la fois, chacune chez son **spécialiste** (pneus, stratégie, voiture, course).
- **Demandes d'arrêt** à la voix (« je boxe », « softs neufs », « la réparation aussi ») confirmées et gardées pour l'arrêt. Le menu des stands du jeu
  se règle encore dans le jeu.
- Boutons **Terminer la session** / **Nouvelle session** et menu pour revoir une session passée (écran *Ingénieur IA*).
- Il garde une **mémoire de la course** et on peut changer de modèle à la voix. Les modèles peuvent tourner sur plusieurs machines (un Raspberry Pi par exemple).
- Un **mode test** fonctionne sans jeu.

Mise en route : installez Ollama, téléchargez un modèle (`ollama pull lfm3-omni:2b`), puis choisissez-le dans l'écran *Ingénieur IA*.

## Jeux pris en charge

Le Mans Ultimate · Assetto Corsa Competizione · rFactor 2 · Assetto Corsa · Automobilista 2 · Project CARS 1 et 2 · RaceRoom ·
rFactor / Automobilista 1 · F1 2019, 2020, 2021, 24 · Forza Motorsport.

### Réglages à faire dans certains jeux

La fenêtre du collecteur indique ce qui manque quand un jeu est ouvert mais n'envoie rien.

| Jeu | À faire une fois |
|---|---|
| **F1 24** (et F1 2019 à 2021) | Dans le jeu : *Options › Paramètres › Télémétrie* : Télémétrie UDP **Activée**, Mode diffusion **Désactivé**, IP **127.0.0.1**, Port **20777**, Format UDP **2024** (l'année du jeu). |
| **Assetto Corsa** (aussi lancé par Content Manager) | Le collecteur propose d'installer le plugin SecondMonitor (bouton *Installer le plugin*, puis *Relancer en administrateur* si le jeu est dans Program Files). Activez ensuite l'application Python « SecondMonitor » dans le jeu ou dans Content Manager. |
| **Le Mans Ultimate**, **rFactor 2** | Le plugin de mémoire partagée du jeu doit être activé. |
| **Forza Motorsport** | Réglages du jeu : *Data Out* activé, IP 127.0.0.1, port 20127. |
| Les autres | Rien à faire. |

## Cartes des circuits

Dès le premier tour, SecondMonitor affiche le **tracé réel** de 40 circuits connus. Un tracé n'est retenu que si le nom et la longueur
du circuit correspondent : il ne remplace jamais une version courte ou un circuit inconnu.
Ensuite, la carte apprise en roulant dans le jeu prend le relais. Elle est exacte pour tous les circuits et gardée pour la fois suivante.
Tracés : [bacinger/f1-circuits](https://github.com/bacinger/f1-circuits) (licence MIT).

## ClickDeck

Le boîtier ClickDeck peut afficher les données de course (position, classement, essence, pneus, météo…) sur des touches de plusieurs tailles. Il peut aussi
servir de bouton radio et poser des questions à l'ingénieur. Il lit l'adresse `http://<ordinateur de SecondMonitor>:8686/api/deck`.

## Réseau et sécurité

- Tout reste sur votre réseau local. Le seul service extérieur est Ollama, sur vos propres machines.
- Ports utilisés :
  - `52642` en UDP, entre le collecteur et SecondMonitor (même protocole que le plugin « Data Broadcast Server » du SecondMonitor d'origine) ;
  - `8686`, pour l'interface en HTTP ;
  - `8687`, pour l'interface en HTTPS (micro et volant depuis un iPad ou un autre ordinateur).
- L'interface n'a pas de mot de passe : n'importe quel appareil de votre réseau local peut l'ouvrir. Utilisez-la chez vous ou derrière un VPN (Tailscale…), pas sur un Wi-Fi public.
- Le collecteur ne lit le jeu que lorsqu'un SecondMonitor est connecté. Il tourne en priorité basse : le jeu passe toujours avant lui.

## Construire

Tout se construit depuis un Mac avec le SDK .NET 10 (`brew install dotnet`) et Node.js 20 (`brew install node@20`) :

```sh
./construire.sh tous      # ou : mac, win, linux   →  fichiers dans dist/
```

L'installateur Windows `.exe` est fabriqué par [Inno Setup](https://jrsoftware.org/isinfo.php) sur Windows. Le workflow GitHub [build.yml](.github/workflows/build.yml) s'en charge.
Sans lui, `./construire.sh win` produit seulement le `.zip`.

- **OneDrop3D** : le produit *SecondMonitor* du Publieur lance ces mêmes commandes. Il change la version, envoie le code sur ce dépôt,
  fait construire sur GitHub et publie la release.
- **GitHub Actions** :
  - les tests tournent à chaque envoi ;
  - tous les systèmes sont construits sur chaque étiquette `v*` ou à la demande (*Actions › Construction › Run workflow*).

| Dossier | Contenu |
|---|---|
| `Hub/` | SecondMonitor (l'application qui affiche) : serveur .NET 10 et fenêtre (`src/`), interface React + TypeScript (`web/`), tests (`tests/`) |
| `Collector/` | SecondMonitor Collecteur. Voir [Collector/LISEZ-MOI.md](Collector/LISEZ-MOI.md). |
| `packaging/` | icônes, installateur Windows (Inno Setup), scripts d'installation Linux |
| `Connectors/`, `Foundation/`, `Remote/` | code du SecondMonitor d'origine, réutilisé sans modification |
| `Applications/`, `Telemetry/`, `Timing/`… | l'application Windows d'origine (WPF), gardée pour référence ; voir [docs/README-SecondMonitor-original.md](docs/README-SecondMonitor-original.md) |

```sh
dotnet test Hub/SecondMonitorHub.sln                                   # tests
cd Hub/web && npm install && npm run dev                               # interface en direct (relais vers le serveur)
dotnet run --project Hub/src/SecondMonitor.Hub -- --demo               # SecondMonitor avec la course de démonstration
dotnet run --project Collector/src -- --demo                           # collecteur qui envoie la démonstration
```

Diagnostic : SecondMonitor enregistre ce que le collecteur envoie (dossier `enregistrements` des données, 5 derniers fichiers) et tient un `journal.txt`.
Une session peut être rejouée : `SecondMonitorHub --rejouer <fichier.smrec> [--vitesse 4]`.

Options de SecondMonitor :
- `--navigateur` ouvre l'interface dans le navigateur au lieu de la fenêtre ;
- `--sans-fenetre` lance le serveur seul (machine sans écran, Raspberry Pi) ;
- `--sans-gpu` affiche la fenêtre sans accélération graphique (Windows : fenêtre noire avec certains pilotes) ; même effet avec `"fenetreSansGpu": true` dans `settings.json`.

Si la fenêtre n'affiche pas l'interface au bout de 15 s, SecondMonitor l'ouvre dans le navigateur et en note la raison dans `journal.txt`.
Sous Windows, SecondMonitor et le collecteur ont chacun leur dossier WebView2 (`%LOCALAPPDATA%\SecondMonitor\WebView2-Affichage` et `WebView2-Collecteur`).

## Crédits et licence

- [SecondMonitor](https://gitlab.com/winzarten/SecondMonitor) © 2023 Matus Celko (winzarten), licence MIT : écran d'origine, connecteurs de jeux, modèle de données, protocole réseau.
- Nouvelle version (SecondMonitor multiplateforme, collecteur, ingénieur IA, analyses) © 2026 OneDrop3D, licence MIT.
- Logos des marques automobiles : [Simple Icons](https://simpleicons.org) (CC0). Les marques appartiennent à leurs propriétaires.
- Tracés des circuits : [bacinger/f1-circuits](https://github.com/bacinger/f1-circuits) © Tomislav Bacinger, licence MIT.

Voir [LICENSE](LICENSE).
