# SecondMonitor (fork OneDrop3D) — journal des versions

SecondMonitor (l'application qui affiche) et SecondMonitor Collecteur (PC de jeu), pour Windows, Mac et Linux/SteamOS.
Basé sur [SecondMonitor](https://gitlab.com/winzarten/SecondMonitor) de winzarten (licence MIT).

## Version 0.6.1

- **Windows** : SecondMonitor s'affiche de nouveau dans sa fenêtre (elle pouvait rester noire quand le collecteur avait été ouvert sur le même PC).
- **Fenêtre** : écran « Chargement de SecondMonitor… » au démarrage ; si l'interface ne s'affiche pas, elle s'ouvre toute seule dans le navigateur et la raison est notée dans le journal. Option `--sans-gpu` (ou « fenetreSansGpu » dans settings.json) pour les pilotes graphiques capricieux.
- **Collecteur** : nouvelle carte « Radio de l'ingénieur » : la voix de l'ingénieur peut sortir sur le PC de jeu, dans le casque choisi (bouton « Tester »).
- **Collecteur** : « Parler avec le micro de ce PC » : le bouton radio du PC de jeu (et ClickDeck) enregistre le micro choisi sur le PC de jeu, avec un test du niveau du micro ; un appui pendant que l'ingénieur parle le coupe.
- **Réglages** : nouvelle carte « Voix de l'ingénieur » : voix sur le PC de jeu, sur les écrans de SecondMonitor, ou les deux ; la carte Radio de l'Ingénieur IA indique quand le PC de jeu a la voix ou le micro.
- **Ingénieur IA, alertes de pilotage immédiates** : limites de piste (« Track limits virage 5 : trop large. 2 sur 3 : la prochaine, c'est la pénalité »), coupé (« levez un peu le pied »), temps annulé, pénalité reçue, moteur coupé ou point mort au départ. Réglage « Alertes de pilotage immédiates ».
- **Ingénieur IA, usure brutale analysée** : quand un pneu prend plusieurs points d'usure en quelques secondes, l'ingénieur dit où (virage), dans quelle phase (freinage, entrée, milieu, sortie), la cause probable et le réglage à essayer avec les valeurs actuelles (« TC de 4 à 5 », « répartition de 54,5 à 54,0 % »), puis demande ce qui s'est passé.
- **Ingénieur IA, réglages de la voiture** : répartition, migration, TC (niveau, coupure, glissement), ABS, cartographie, regen, moteur électrique, batterie et énergie virtuelle lus dans le jeu (Le Mans Ultimate ; F1 24 et ACC en partie). « TC ? », « la carto ? », « la balance des freins » répondent avec la valeur exacte.
- **Ingénieur IA, catégories de voitures** : Hypercar LMH (hybride sur l'avant au-dessus de 190 km/h) et LMDh (hybride sur l'arrière), LMP2, LMGT3, GTE, GT3, GT4, F1 : ce qui se règle en roulant, ce que fait chaque réglage, et jamais un réglage que la voiture n'a pas.
- **Ingénieur IA, réponses courtes** : les températures et l'usure aux quatre roues, sans commentaire (« Avant gauche 80 degrés, avant droit 90, arrière gauche 50, arrière droit 55 ») ; les mots seuls (« Pneus », « TC », « Fure des pneus ») sont compris ; plus de « Vous êtes P1 » quand vous roulez seul en essais (comparaison avec vos propres meilleurs micro-secteurs).
- **Ingénieur IA, « Copy »** : accusés de réception en anglais comme une vraie radio d'équipe (réglable). Si l'IA réfléchit plus de 2,5 s : « Copy, je regarde ça et je reviens vers vous », vous pouvez poser d'autres questions, et la réponse arrive ensuite (« Pour la stratégie : … »). Les questions qui demandent l'IA passent dans l'ordre, une à la fois.
- **Ingénieur IA, spécialistes** : pneus, stratégie, voiture, course ; chacun ne reçoit que les faits de son domaine (réponse plus rapide, deux phrases au plus) et peut avoir son propre modèle (carte « Spécialistes »).
- **Ingénieur IA, demandes d'arrêt** : « je boxe », « je veux des softs neufs », « la réparation aussi », « fais le plein » sont confirmés et gardés pour l'arrêt (écran et mémoire de course).
- **Voix** : une vraie pause entre les valeurs d'une liste (les virgules n'étaient pas marquées à l'oral).
- **Pédales et volant** : nouveau bloc de l'accueil avec l'accélérateur, le frein et le volant en direct et sur tout le tour, comparés à votre meilleur tour ; l'ingénieur analyse votre pilotage (« Peux-tu analyser mon pilotage ? »).
- **Réglages et limites** : nouveau bloc de l'accueil (répartition, TC, ABS, cartographie, énergie, points de limites de piste, pénalités).
- **Sessions** : boutons « Terminer la session » et « Nouvelle session », et menu déroulant pour revoir les échanges d'une session passée (écran Ingénieur IA).
- **Dégâts** : plus de fausses « roues arrachées » en entrant au garage ou en sortant des stands.

---

## Version 0.6.0

- **Mises à jour intégrées** (comme Multibook et ClickDeck) : SecondMonitor vérifie au démarrage puis toutes les 6 h, télécharge la nouvelle version (taille et SHA-256 vérifiés) et « Redémarrer et installer » remplace les deux applications ensemble, sans repasser par Réglages › Confidentialité et sécurité sur Mac. Retour automatique à l'ancienne version si la nouvelle ne démarre pas. Le collecteur du PC de jeu a aussi son bouton « Installer » (jamais pendant que le jeu envoie des données). La première version avec ce système (0.6.0) s'installe encore à la main.
- **Linux** : installer.sh vérifie les dépendances et donne la commande exacte (apt, dnf, pacman, zypper, SteamOS), options `--demarrage-auto`, `--pare-feu`, `--verifier`, `--silencieux` ; désinstallation complète avec `--tout` ; repli navigateur sans session graphique ; check-list de test dans packaging/linux/LISEZ-MOI-LINUX.md.
- **Accueil en tableau de bord personnalisable** : bouton « Disposition » dans la barre du haut pour déplacer les blocs (glisser, au doigt sur iPad et téléphone), changer leur largeur et leur hauteur, les masquer, en ajouter ou tout réinitialiser. Disposition enregistrée sur chaque appareil (grand écran, tablette, téléphone sur une colonne). Blocs : Classement (mini-secteurs violet / vert / jaune avec temps de secteur, intervalle, places gagnées ▲▼, arrêts, pneu et âge), Carte du circuit, Radio (derniers échanges pilote ↔ ingénieur, réécoute de la voix), Événements de course (drapeaux, arrêts des voisins, meilleurs tours, tours invalidés, abandons, alertes pneus et dégâts, notifications passées, filtre par type), Ma voiture, Prochain arrêt, Meilleurs tours, Duels, Dégâts, Météo, Delta.
- **Barre du haut** : nouveau bloc temps restant / tours restants avec barre d'avancement ; en course au temps, le hub estime vos tours restants (et ceux du leader) à partir du rythme réel et de la règle de fin (le tour en cours se termine quand le temps est écoulé). État de la session, vent et piste mouillée affichés.
- **Notifications** : chacune a sa croix, disparaît seule, ne revient pas en boucle pour le même événement et reste consultable dans « Événements de course » (correction : la notification « Données reçues » pouvait rester bloquée). Les alertes pneus / dégâts et les drapeaux se masquent aussi d'une croix.
- **Musique Deezer** (carte Musique dans Réglages) : la musique joue dans l'application Deezer de l'ordinateur (pleine qualité, votre compte) et se commande à la voix sans passer par l'IA (« mets mon Flow », « mets la playlist rock », « mets Daft Punk », « pause musique », « chanson suivante », « remets la précédente », « monte / baisse la musique », « c'est quoi ce morceau ? ») ; la radio passe avant (pause puis reprise, ou volume de Deezer baissé sur Windows / Linux) ; option « pas de musique en course » ou « seulement en essais ».
- **Collecteur plus léger pour le PC de jeu** (même protocole, rien de perdu pour SecondMonitor) : quand le jeu se tait (menus, pause, garage), le signe de vie d'une seconde ne renvoie plus tout le plateau identique (≈ 18 Ko/s → 0,2 Ko/s en F1 24) ; recherche du jeu en un seul passage sur les processus (3× moins d'allocations, 2× moins de CPU sans jeu) ; envois sans copies inutiles (−38 % d'allocations en course) ; fenêtre mise à jour seulement quand quelque chose change, et presque plus quand elle est réduite. Option **Diagnostic** (Réglages, ou `--diagnostic`) : CPU, mémoire, débit réseau réel et lectures du jeu par seconde. `--rejouer fichier.smrec` renvoie un enregistrement de SecondMonitor pour mesurer sans le jeu.
- **Ingénieur IA** : réponses chiffrées courtes et instantanées (meilleur / pire / dernier tour, moyenne, régularité, « Vitesse max 204 km/h au tour 7, en moyenne 202 km/h ; le plus rapide est le numéro 52 à 208 km/h » avec un conseil de sortie de virage ou d'aileron en cas de déficit) ; base « symptôme → réglages » (sous-virage / survirage selon la phase, blocages, roue intérieure, patinage, freinage instable, rebonds, pneus, pluie) adaptée au jeu et à la voiture, sans attendre le modèle ; fil de conversation (« oui », « non » → « Copy, compris », « autre chose ? », « c'est en sortie », « c'est mieux »), propositions refusées jamais reproposées ; chaque échange classé (Stratégie, Réglages de course, Réglages de la voiture, Informations course) ; **Carnet** de session enregistré automatiquement (dossier « carnets ») avec récapitulatif de fin de session (IA ou calculé) ; radio d'arrivée avec le résultat ; conseils batterie pour les hybrides (F1 : ERS, LMU : hypercars) ; quand une donnée manque, l'ingénieur dit laquelle et pourquoi.
- **Tracés réels de circuits** : la bibliothèque passe de 40 à 130 circuits et variantes (ACC au complet, Le Mans Ultimate, Assetto Corsa, F1, rFactor 2, Automobilista 2 avec les circuits brésiliens, RaceRoom avec les circuits allemands et scandinaves, iRacing, Project CARS 2), d'après OpenStreetMap (© contributeurs OpenStreetMap, ODbL) et bacinger/f1-circuits (MIT). La ligne de départ et le sens de course sont recalés automatiquement sur les positions des voitures dans le jeu ; tant qu'un tracé n'est pas sûr, il est signalé « départ estimé » ou n'est pas affiché.

## Version 0.5.0

- **Dégâts mécaniques** pièce par pièce, quand le jeu les donne :
  - F1 24 : ailerons avant gauche et droit, aileron arrière, fond plat, diffuseur, pontons, boîte, moteur, usure de chaque élément moteur (MGU-K, MGU-H, batterie…), pannes DRS et ERS, pneus abîmés ;
  - ACC : carrosserie par zone et suspension par roue ;
  - Le Mans Ultimate : carrosserie, pièce arrachée, surchauffe, crevaisons ;
  - les autres jeux : moteur, boîte, suspension, carrosserie, freins.

  Un nouveau dégât est signalé tout de suite. Son coût par tour est mesuré deux tours plus tard, et l'ingénieur dit si un arrêt pour réparer est rentable.
- **Stratégie d'arrêt calculée** :
  - perte au stand et gain des pneus neufs mesurés sur tout le plateau ;
  - position à la sortie si vous rentrez maintenant ;
  - **undercut** et **overcut** sur la voiture devant ;
  - **menace d'undercut** de la voiture derrière.

  Le tout est affiché dans l'écran Stratégies et le panneau du Chronométrage, et l'ingénieur prévient quand un undercut est jouable.
- **Ingénieur IA** :
  - il connaît le vocabulaire de course (undercut, overcut, relais, tour de sortie, graining, carrossage…) et recopie les calculs ;
  - il s'adapte à la session : la voiture en essais (compte rendu de tour avec pressions et carrossage), le tour lancé en qualification, le déroulé en course ;
  - les questions rapides changent selon la session.
- **Tracés réels** de 40 circuits (Silverstone, Spa, Monza, Le Castellet, Suzuka, COTA, Interlagos, Imola, Portimão, Losail…). Ils s'affichent dès le premier tour au lieu du cercle, puis sont remplacés par le tracé appris en roulant. Un tracé n'est utilisé que si le nom et la longueur du circuit correspondent.
- **Bouton radio** où l'on veut :
  - une touche du clavier ou un bouton du volant du **PC de jeu**, réglé dans la fenêtre du collecteur ; il marche même quand le jeu est au premier plan (Windows) ;
  - le volant branché sur l'ordinateur qui affiche ;
  - une touche ClickDeck.

  L'écran « poste radio » se choisit dans les Réglages.
- **Essais et qualification** : le panneau de droite du Chronométrage montre la voiture (meilleur tour, écart, pneus sur le dernier tour, pressions) à la place des duels.
- Nouvelle adresse `/api/deck` : toutes les données de course sous une forme compacte, pour ClickDeck et les petits écrans. `POST /api/radio/press` pour un bouton radio externe.
- En mode serveur sans fenêtre, plus de fenêtre surprise si le port est déjà pris.

## Version 0.4.1

- **Écran noir corrigé** : une valeur de calcul impossible (division par zéro sur certaines données de jeu) faisait planter l'interface. Ces valeurs s'affichent maintenant « — ». Si un écran rencontre malgré tout un problème, il l'affiche et les autres continuent.
- **Voir que ça marche** : nouvel écran **Connexion** avec trois étapes (collecteur, jeu, données) et un bandeau vert « Tout fonctionne ». Il propose aussi une saisie simple de l'adresse du PC. Une notification apparaît quand les données arrivent, et l'état reste affiché dans la barre du bas.
- **Guide de mise en route** au premier lancement : connexion, ingénieur IA (Ollama, modèles, test), bouton radio et volant.
- Le jeu ouvert mais muet n'est plus pris pour une course (avant, l'écran restait vide sans explication). Le collecteur et SecondMonitor disent quoi régler, par exemple la télémétrie UDP de F1 24.
- **Assetto Corsa** (aussi avec Content Manager) : le collecteur propose d'installer le plugin nécessaire (avant, la demande était perdue et le jeu n'était jamais détecté). Un bouton permet de relancer en administrateur si besoin.
- **Fenêtre du collecteur** plus claire : grand bandeau d'état, trois étapes cochées, conseils, réglages repliés.
- **Ingénieur IA** :
  - rythme calculé (le vôtre, l'objectif pour rattraper ou gagner, la projection) ;
  - tendance météo à 30 minutes ;
  - réponse à toutes les questions posées d'un coup ;
  - une phrase dictée sans « ? » est quand même comprise comme une question.
- **Alertes pneus** : moyenne de température par tour, alerte quand un pneu est trop chaud sur tout un tour ou depuis plus de 30 s, pneus froids au tour de formation. Toutes les sessions sont couvertes, pas seulement la course. Le pneu concerné est encadré en rouge à l'écran.
- Diagnostic : enregistrement de ce que le collecteur envoie, rejouable (`--rejouer`), et `journal.txt`.
- Windows : la vue web utilise un dossier de travail dans le profil de l'utilisateur.

## Version 0.4.0

- **Deux vraies applications, installées ensemble** sur Windows, Mac et Linux : **SecondMonitor** (affiche les données, dans sa propre fenêtre, sans navigateur) et **SecondMonitor Collecteur** (PC de jeu, avec une petite fenêtre d'état : connexion, jeu détecté, débit, qualité, démarrage automatique, démonstration, journal).
- **Écran « Chronométrage » repris de SecondMonitor d'origine** :
  - bandeau de session avec la météo, les deltas en direct (meilleur tour, tour précédent), les meilleurs secteurs, le meilleur tour et le temps restant ;
  - grand tableau avec les secteurs violets et verts et le numéro du tour du meilleur temps ;
  - les quatre roues, le carburant, le moteur, la carte, les duels et la stratégie ;
  - barre d'écrans à droite et barre d'état en bas.
- Installateurs : `.exe` pour Windows (Inno Setup, choix des applications, raccourcis, désinstallation), `.dmg` pour Mac (les deux applications), `.tar.gz` + `installer.sh` pour Linux (menu des applications).
- Corrigé : sur Mac, l'application quittait de manière imprévue quand une autre copie tournait déjà (port 8686 occupé). Désormais elle ouvre simplement la copie en cours, ou explique quel programme occupe le port.
- Le collecteur range ses réglages dans le dossier de l'utilisateur (l'application installée peut être en lecture seule).
- Arrondis homogènes sur tous les blocs. Thème sombre par défaut, comme l'original.

## Version 0.3.1

- Première version publiée : hub pour Mac (Apple Silicon et Intel), Windows et Linux ; collecteur pour Windows et Linux/SteamOS.
- Construction automatique de tous les systèmes (OneDrop3D et GitHub Actions).

---

## Version 0.3.0

- **Collecteur** : petit programme pour le PC de jeu (Windows, Linux/SteamOS) qui remplace SecondMonitor complet, avec les mêmes jeux et une charge minimale.
- **Ingénieur IA** : voix sans chiffres mal lus, coupure de la radio d'un appui, prise de parole spontanée, mémoire de course, changement de modèle à la voix, plusieurs machines IA.
- **Analyses** : micro-secteurs, Vmax/Vmin et trajectoires, télémétrie pneus/freins/moteur, météo et tendance, prévisions.
- **Interface** : centrée sur votre catégorie, adaptée à toutes les tailles d'écran, logos des marques, graphiques agrandissables.

---
