# SecondMonitor (fork OneDrop3D) — journal des versions

SecondMonitor (l'application qui affiche) et SecondMonitor Collecteur (PC de jeu), pour Windows, Mac et Linux/SteamOS.
Basé sur [SecondMonitor](https://gitlab.com/winzarten/SecondMonitor) de winzarten (licence MIT).

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
