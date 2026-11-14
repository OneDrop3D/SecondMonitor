# SecondMonitor sous Linux

Le paquet `SecondMonitor-<version>-linux-x64.tar.gz` contient les deux applications (64 bits Intel/AMD, .NET inclus) :
**SecondMonitor** (l'écran qui affiche) et **SecondMonitor Collecteur** (lit le jeu sur le PC de jeu).

## Installer

```sh
tar xzf SecondMonitor-*-linux-x64.tar.gz
./secondmonitor/installer.sh
```

Rien n'est demandé en mot de passe : tout va dans `~/.local/share/secondmonitor`, avec deux entrées dans le menu des applications.
L'installateur vérifie les bibliothèques du système et donne, s'il en manque, la commande exacte pour votre distribution :

| Distribution | Commande (tout ce qui peut manquer) |
|---|---|
| Debian, Ubuntu, Mint | `sudo apt install libwebkit2gtk-4.1-0 libnotify4 libicu74 libssl3` (le numéro de libicu dépend de la version) |
| Fedora | `sudo dnf install webkit2gtk4.1 libnotify libicu openssl-libs` |
| Arch, Manjaro | `sudo pacman -S --needed webkit2gtk-4.1 libnotify icu openssl` |
| openSUSE | `sudo zypper install libwebkit2gtk-4_1-0 libnotify4 libicu libopenssl3` |

- Sans WebKitGTK 4.1 : SecondMonitor s'ouvre dans le navigateur et le collecteur dans un terminal (tout fonctionne quand même).
- Sans ICU : SecondMonitor démarre en mode « sans langue » (l'installateur règle `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`).
- Sans OpenSSL : pas de HTTPS (micro depuis un autre appareil) ni de mise à jour intégrée.

Options : `--demarrage-auto` (collecteur lancé à l'ouverture de session), `--pare-feu` (ouvre les ports avec ufw ou firewalld),
`--verifier` (dépendances seulement), `--silencieux` (aucune question).

**Pare-feu** : le PC de jeu doit accepter UDP **52642** (collecteur) ; l'ordinateur qui affiche, TCP **8686** et **8687** pour les autres
écrans (iPad, téléphone) et UDP **8689** pour trouver les serveurs de calcul. Si la découverte automatique ne trouve pas le PC de jeu,
saisissez son adresse dans SecondMonitor › Connexion.

**Serveur de calcul** : un PC Linux avec une grosse carte graphique peut prêter son Ollama au SecondMonitor qui affiche la course.
Au premier lancement, choisissez « Serveur de calcul » ; sans écran : `~/.local/share/secondmonitor/SecondMonitorHub --sans-fenetre --serveur-calcul`
(le code d'appairage s'affiche dans le terminal, de nouveau à chaque changement ; retour à l'affichage : `--affichage`).
Il doit accepter TCP **8686**. Son Ollama peut rester fermé au réseau : SecondMonitor le relaie, seulement pour les SecondMonitor reliés par code.

## Quels jeux ?

- **F1 (2019 à 24) et Forza Motorsport** envoient leurs données par le réseau (UDP) : le collecteur Linux suffit, même si le jeu tourne avec Proton.
  F1 : télémétrie UDP activée, IP 127.0.0.1, port 20777, format 2024.
- **LMU, ACC, rFactor 2, AMS2, Assetto Corsa, RaceRoom, Project CARS** passent par une mémoire partagée qui n'existe que dans Proton :
  lancez la version Windows du collecteur dans l'environnement du jeu, une fois le jeu démarré :
  `~/.local/share/secondmonitor/lancer-avec-proton.sh <appid>` (ex. `2399420` pour Le Mans Ultimate ; protontricks nécessaire,
  sur SteamOS : `flatpak install flathub com.github.Matoking.protontricks`).

## Mettre à jour, désinstaller

- Mise à jour : SecondMonitor › Réglages › **Mises à jour** (ou le bandeau « Mise à jour x.y.z disponible — Installer »), ou le bouton de la
  fenêtre du collecteur. Les deux applications sont remplacées, rouvertes, et l'ancienne version revient si la nouvelle ne démarre pas.
- Désinstaller : `~/.local/share/secondmonitor/desinstaller.sh` (réglages gardés) ou `desinstaller.sh --tout`.
- Données : réglages et enregistrements dans `~/.config/SecondMonitorHub` et `~/.config/SecondMonitor Collecteur` ; téléchargements de
  mise à jour dans `~/.cache/secondmonitor`.

## Check-list de test

1. `./secondmonitor/installer.sh --verifier` : tout est ✔, sinon lancer la commande proposée.
2. `./secondmonitor/installer.sh` : les deux entrées apparaissent dans le menu, avec l'icône.
3. Ouvrir **SecondMonitor** depuis le menu : une fenêtre (pas le navigateur), l'écran « Connexion ». Réglages › Course de démonstration : les données défilent.
4. Ouvrir **SecondMonitor Collecteur** : sa fenêtre affiche l'adresse du PC. Cocher « Démonstration » : SecondMonitor la reçoit
   (même machine, ou une autre machine du réseau).
5. Depuis un téléphone : `http://<adresse>:8686` s'ouvre (sinon : pare-feu, `installer.sh --pare-feu`).
6. F1 24 avec Proton : télémétrie UDP 127.0.0.1:20777 → le collecteur Linux montre le jeu et les données.
7. Un jeu à mémoire partagée (LMU…) : `lancer-avec-proton.sh <appid>` une fois le jeu lancé → données reçues.
8. `installer.sh --demarrage-auto`, fermer la session, la rouvrir : le collecteur démarre réduit.
9. Réglages › Mises à jour › « Vérifier maintenant » : « à jour » ou une version proposée (pas d'erreur).
10. `desinstaller.sh` : plus d'entrées dans le menu, `~/.local/share/secondmonitor` a disparu, les réglages restent.

En cas de souci : `~/.config/SecondMonitorHub/journal.txt`, `~/.config/SecondMonitor Collecteur/collecteur.log`, et lancer
`~/.local/share/secondmonitor/SecondMonitorHub` dans un terminal pour voir les messages.
