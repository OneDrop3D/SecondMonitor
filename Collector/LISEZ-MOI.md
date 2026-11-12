# SecondMonitor Collecteur

Petit programme à lancer **sur le PC de jeu**. Il lit le jeu et envoie les données au **SecondMonitor Hub** (sur le Mac).
Il remplace le SecondMonitor complet sur le PC de jeu : pas d'écrans, pas de calculs, le strict minimum.

- Un seul fichier, rien à installer (.NET est inclus).
- Il reprend les connecteurs de SecondMonitor : Le Mans Ultimate, ACC, rFactor 2, Assetto Corsa, Automobilista 2, Project CARS 1 et 2, RaceRoom, rFactor / AMS 1, F1 2019 à 24, Forza Motorsport.
- Il parle le même langage que le plugin « Data Broadcast Server » : le hub le trouve tout seul sur le réseau.

## Windows

1. Copiez le dossier `win-x64` sur le PC de jeu, par exemple dans `Documents\SecondMonitor-Collecteur`.
2. Double-cliquez sur **`SecondMonitor-Collecteur.exe`**. Si Windows demande d'autoriser l'accès au réseau, choisissez **Réseau privé**.
3. Lancez le jeu. Le collecteur l'affiche (« Jeu détecté : … ») dès que le hub du Mac est connecté.

Pour le lancer automatiquement à chaque démarrage de Windows (fenêtre réduite) :

```
SecondMonitor-Collecteur.exe --autostart on
```

`--autostart off` annule.

Pour **tester la liaison avec le Mac sans lancer de jeu** : `SecondMonitor-Collecteur.exe --demo`. Une course fictive doit apparaître sur le hub.

> Le SecondMonitor complet et le collecteur utilisent le même port (52642) : n'en lancez qu'un des deux à la fois.

Rfactor 2, rFactor et Assetto Corsa ont besoin d'un petit plugin dans le dossier du jeu. Comme SecondMonitor, le collecteur
propose de l'installer la première fois (une autorisation administrateur est demandée).

## Linux / SteamOS

- **F1 et Forza** envoient leurs données par le réseau. La version Linux suffit : lancez `./SecondMonitor-Collecteur` depuis le dossier `linux-x64`.
- **Les autres jeux** (LMU, ACC, rFactor 2, AMS2, Assetto Corsa, RaceRoom) tournent avec Proton et ne partagent leurs données qu'à l'intérieur de Proton.
  Il faut donc lancer la version Windows du collecteur **dans le même environnement que le jeu** :
  1. Installez protontricks. Sur SteamOS, en mode bureau : `flatpak install flathub com.github.Matoking.protontricks`.
  2. Copiez les dossiers `win-x64` et `linux-x64` côte à côte.
  3. Le jeu lancé, exécutez `./lancer-avec-proton.sh <appid>`. Par exemple `./lancer-avec-proton.sh 2399420` pour Le Mans Ultimate.

  Les appid courants sont listés dans le script. L'appid d'un jeu est le nombre dans l'adresse de sa page Steam.

## Réglages (facultatifs) : `collecteur.json`

Le fichier est créé à côté du programme au premier lancement.

| Réglage | Par défaut | Rôle |
|---|---|---|
| `qualite` | `equilibre` | `econome` : 10 lectures/s, adversaires 1×/s, le plus léger. `equilibre` : 20 lectures/s, vous 10×/s, adversaires 4×/s. `precis` : 30 lectures/s, adversaires 10×/s, pour des Vmax et des trajectoires plus fines. |
| `pauseSansHubSecondes` | `20` | Sans hub connecté, le jeu n'est plus lu du tout (aucune charge). `0` = toujours lire. |
| `prioriteBasse` | `true` | Le jeu passe toujours avant le collecteur. |
| `jeuxDesactives` | `[]` | Jeux à ne pas surveiller (noms affichés au démarrage). |
| `port` | `52642` | Port attendu par le hub. |

## Consommation mesurée

Mesures faites sur Mac, en démo, simulation de la course comprise (donc plus que dans un jeu réel) :

- au repos, sans hub : environ 0,1 % d'un cœur ;
- en envoi au hub (« équilibré ») : environ 2,5 % d'un cœur et 110 Mo de mémoire, soit environ 0,3 % d'un processeur 8 cœurs ;
- sur le réseau : quelques dizaines de Ko/s.

## Reconstruire

Depuis le Mac : `Collector/publier.sh`. Les fichiers sont créés dans `Collector/dist/win-x64` et `Collector/dist/linux-x64`.
