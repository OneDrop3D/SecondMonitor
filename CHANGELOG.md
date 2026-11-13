# SecondMonitor (fork OneDrop3D) — journal des versions

SecondMonitor (l'application qui affiche) et SecondMonitor Collecteur (PC de jeu), pour Windows, Mac et Linux/SteamOS.
Basé sur [SecondMonitor](https://gitlab.com/winzarten/SecondMonitor) de winzarten (licence MIT).

## Non publié

- **Accueil en tableau de bord personnalisable** : bouton « Disposition » dans la barre du haut pour déplacer les blocs (glisser, au doigt sur iPad et téléphone), changer leur largeur et leur hauteur, les masquer, en ajouter ou tout réinitialiser. Disposition enregistrée sur chaque appareil (grand écran, tablette, téléphone sur une colonne). Blocs : Classement (mini-secteurs violet / vert / jaune avec temps de secteur, intervalle, places gagnées ▲▼, arrêts, pneu et âge), Carte du circuit, Radio (derniers échanges pilote ↔ ingénieur, réécoute de la voix), Événements de course (drapeaux, arrêts des voisins, meilleurs tours, tours invalidés, abandons, alertes pneus et dégâts, notifications passées, filtre par type), Ma voiture, Prochain arrêt, Meilleurs tours, Duels, Dégâts, Météo, Delta.
- **Barre du haut** : nouveau bloc temps restant / tours restants avec barre d'avancement ; en course au temps, le hub estime vos tours restants (et ceux du leader) à partir du rythme réel et de la règle de fin (le tour en cours se termine quand le temps est écoulé). État de la session, vent et piste mouillée affichés.
- **Notifications** : chacune a sa croix, disparaît seule, ne revient pas en boucle pour le même événement et reste consultable dans « Événements de course » (correction : la notification « Données reçues » pouvait rester bloquée). Les alertes pneus / dégâts et les drapeaux se masquent aussi d'une croix.
- **Musique Deezer** (carte Musique dans Réglages) : la musique joue dans l'application Deezer de l'ordinateur (pleine qualité, votre compte) et se commande à la voix sans passer par l'IA (« mets mon Flow », « mets la playlist rock », « mets Daft Punk », « pause musique », « chanson suivante », « remets la précédente », « monte / baisse la musique », « c'est quoi ce morceau ? ») ; la radio passe avant (pause puis reprise, ou volume de Deezer baissé sur Windows / Linux) ; option « pas de musique en course » ou « seulement en essais ».
- **Collecteur plus léger pour le PC de jeu** (même protocole, rien de perdu pour SecondMonitor) : quand le jeu se tait (menus, pause, garage), le signe de vie d'une seconde ne renvoie plus tout le plateau identique (≈ 18 Ko/s → 0,2 Ko/s en F1 24) ; recherche du jeu en un seul passage sur les processus (3× moins d'allocations, 2× moins de CPU sans jeu) ; envois sans copies inutiles (−38 % d'allocations en course) ; fenêtre mise à jour seulement quand quelque chose change, et presque plus quand elle est réduite. Option **Diagnostic** (Réglages, ou `--diagnostic`) : CPU, mémoire, débit réseau réel et lectures du jeu par seconde. `--rejouer fichier.smrec` renvoie un enregistrement de SecondMonitor pour mesurer sans le jeu.

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
