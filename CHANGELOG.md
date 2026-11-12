# SecondMonitor (fork OneDrop3D) — journal des versions

SecondMonitor (l'application qui affiche) et SecondMonitor Collecteur (PC de jeu), pour Windows, Mac et Linux/SteamOS.
Basé sur [SecondMonitor](https://gitlab.com/winzarten/SecondMonitor) de winzarten (licence MIT).

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
