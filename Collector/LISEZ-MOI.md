# SecondMonitor Collecteur

Petite application à ouvrir **sur le PC de jeu**. Elle lit le jeu et envoie les données à **SecondMonitor** (l'application qui affiche,
sur un autre ordinateur ou sur un deuxième écran du même PC). Elle est installée en même temps que SecondMonitor.
Le SecondMonitor d'origine n'est pas nécessaire.

- Une petite fenêtre d'état (SecondMonitor connecté, jeu détecté, débit, adresse du PC, qualité, options, journal), rien à installer à côté (.NET est inclus).
- Il reprend les connecteurs de SecondMonitor : Le Mans Ultimate, ACC, rFactor 2, Assetto Corsa, Automobilista 2, Project CARS 1 et 2, RaceRoom, rFactor / AMS 1, F1 2019 à 24, Forza Motorsport.
- Il parle le même langage que le plugin « Data Broadcast Server » : SecondMonitor le trouve tout seul sur le réseau.

## Windows

1. Installez SecondMonitor (`SecondMonitor-…-windows-x64-installation.exe`), puis ouvrez **SecondMonitor Collecteur** depuis le menu Démarrer.
   Si Windows demande d'autoriser l'accès au réseau, choisissez **Réseau privé**.
2. Lancez le jeu. La fenêtre affiche « Jeu » en vert dès que SecondMonitor est connecté.

Dans la fenêtre :
- *Lancer avec Windows* : démarrage automatique, fenêtre réduite ;
- *Démonstration* : envoie une course fictive pour tester la liaison sans jeu.

Sans fenêtre (serveur, script) : `SecondMonitor-Collecteur.exe --sans-fenetre`.

> Le SecondMonitor complet et le collecteur utilisent le même port (52642) : n'en lancez qu'un des deux à la fois.

rFactor 2, rFactor et Assetto Corsa ont besoin d'un petit plugin dans le dossier du jeu. La fenêtre du collecteur le signale avec un bouton
**Installer le plugin**. Si le jeu est dans un dossier protégé (Program Files), le bouton **Relancer en administrateur** fait l'installation une fois.
Assetto Corsa : activez ensuite l'application Python « SecondMonitor » dans le jeu ou dans Content Manager.

F1 24 : activez la télémétrie UDP dans le jeu (*Options › Paramètres › Télémétrie* : Activée, diffusion Désactivée, IP 127.0.0.1, port 20777, format 2024).
Tant qu'un jeu est ouvert sans envoyer de données, la fenêtre l'indique avec le réglage à faire.

## Linux / SteamOS

Installation : `./secondmonitor/installer.sh` (voir le README). Les deux applications s'ajoutent au menu.

- **F1 et Forza** envoient leurs données par le réseau. Le collecteur Linux suffit : ouvrez **SecondMonitor Collecteur** depuis le menu.
- **Les autres jeux** (LMU, ACC, rFactor 2, AMS2, Assetto Corsa, RaceRoom) tournent avec Proton et ne partagent leurs données qu'à l'intérieur de Proton.
  Il faut donc lancer la version Windows du collecteur **dans le même environnement que le jeu** :
  1. Installez protontricks. Sur SteamOS, en mode bureau : `flatpak install flathub com.github.Matoking.protontricks`.
  2. Le jeu lancé, exécutez `~/.local/share/secondmonitor/lancer-avec-proton.sh <appid>`. Par exemple `./lancer-avec-proton.sh 2399420` pour Le Mans Ultimate.

  Les appid courants sont listés dans le script. L'appid d'un jeu est le nombre dans l'adresse de sa page Steam.

## Réglages (facultatifs) : `collecteur.json`

La qualité et le démarrage automatique se règlent dans la fenêtre. Les autres réglages sont dans `collecteur.json`, créé au premier lancement
(bouton *Dossier des réglages*) : `%APPDATA%\SecondMonitor Collecteur` sous Windows, `~/.config/SecondMonitor Collecteur` sous Linux,
`~/Library/Application Support/SecondMonitor Collecteur` sur Mac.

| Réglage | Par défaut | Rôle |
|---|---|---|
| `qualite` | `equilibre` | `econome` : 10 lectures/s, adversaires 1×/s, le plus léger. `equilibre` : 20 lectures/s, vous 10×/s, adversaires 4×/s. `precis` : 30 lectures/s, adversaires 10×/s, pour des Vmax et des trajectoires plus fines. |
| `pauseSansHubSecondes` | `20` | Sans SecondMonitor connecté, le jeu n'est plus lu du tout (aucune charge). `0` = toujours lire. |
| `prioriteBasse` | `true` | Le jeu passe toujours avant le collecteur. |
| `jeuxDesactives` | `[]` | Jeux à ne pas surveiller (noms affichés au démarrage). |
| `port` | `52642` | Port attendu par SecondMonitor. |

## Consommation mesurée

Mesures faites sur Mac, en démo, simulation de la course comprise (donc plus que dans un jeu réel) :

- au repos, sans SecondMonitor connecté : environ 0,1 % d'un cœur ;
- en envoi (« équilibré ») : environ 2,5 % d'un cœur et 110 Mo de mémoire, soit environ 0,3 % d'un processeur 8 cœurs ; la fenêtre ouverte ajoute un peu de mémoire (vue web du système), la fenêtre réduite rien de notable ;
- sur le réseau : de quelques dizaines à environ 180 Ko/s selon le nombre de voitures (180 Ko/s mesurés avec la course de démonstration).

## Reconstruire

Depuis le Mac : `./construire.sh win|linux|mac` à la racine du dépôt (voir le README).
