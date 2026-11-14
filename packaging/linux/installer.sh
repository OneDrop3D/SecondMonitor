#!/bin/sh
# Installe SecondMonitor et SecondMonitor Collecteur pour l'utilisateur courant (aucun mot de passe demandé) :
# programmes dans ~/.local/share/secondmonitor, raccourcis dans le menu des applications.
#
#   ./installer.sh                   installation (questions : démarrage automatique, pare-feu)
#   ./installer.sh --silencieux      sans aucune question (utilisé par la mise à jour intégrée)
#   ./installer.sh --demarrage-auto  lancer le collecteur à l'ouverture de session (--sans-demarrage-auto : non)
#   ./installer.sh --pare-feu        ouvrir les ports dans le pare-feu (ufw ou firewalld, demande sudo)
#   ./installer.sh --verifier        vérifier seulement les dépendances, sans rien installer
#
# Réglages et journaux : ~/.config/SecondMonitorHub et ~/.config/SecondMonitor Collecteur (XDG_CONFIG_HOME),
# mises à jour téléchargées : ~/.cache/secondmonitor (XDG_CACHE_HOME).
set -e
SRC="$(cd "$(dirname "$0")" && pwd)"
DATA="${XDG_DATA_HOME:-$HOME/.local/share}"
DEST="$DATA/secondmonitor"
APPS="$DATA/applications"
ICONS="$DATA/icons/hicolor"
AUTOSTART="$HOME/.config/autostart/secondmonitor-collecteur.desktop"   # même fichier que le collecteur (Autostart.cs)

SILENCIEUX=non; DEMARRAGE=""; PAREFEU=""; VERIFIER=non
for a in "$@"; do
  case "$a" in
    --silencieux|--non-interactif|-q) SILENCIEUX=oui ;;
    --demarrage-auto) DEMARRAGE=oui ;;
    --sans-demarrage-auto) DEMARRAGE=non ;;
    --pare-feu) PAREFEU=oui ;;
    --verifier) VERIFIER=oui ;;
    -h|--help|--aide) sed -n '2,13p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) echo "Option inconnue : $a (voir --aide)" >&2; exit 2 ;;
  esac
done
# Pas de terminal (lancé par la mise à jour, ou par un double-clic) : jamais de question.
if [ ! -t 0 ]; then SILENCIEUX=oui; fi

dire() { [ "$SILENCIEUX" = oui ] && [ "${2:-}" != toujours ] || printf '%s\n' "$1"; }
demander() {  # demander "question" → vrai si « oui »
  [ "$SILENCIEUX" = oui ] && return 1
  printf '%s [o/N] ' "$1"
  read -r r || return 1
  case "$r" in o|O|oui|Oui|OUI|y|Y|yes) return 0 ;; *) return 1 ;; esac
}

if [ "$(uname -m)" != x86_64 ]; then
  echo "Ce paquet est pour Linux 64 bits Intel/AMD (x86_64) ; cet ordinateur est en $(uname -m)." >&2
  exit 1
fi

# ───── Dépendances (bibliothèques du système) ─────
a_lib() {  # a_lib motif → vrai si une bibliothèque correspondante est installée
  if command -v ldconfig >/dev/null 2>&1 && ldconfig -p 2>/dev/null | grep -q "$1"; then return 0; fi
  for d in /usr/lib /usr/lib64 /lib /lib64 /usr/lib/x86_64-linux-gnu /lib/x86_64-linux-gnu /usr/local/lib; do
    ls "$d"/$1* >/dev/null 2>&1 && return 0
  done
  return 1
}
ID=""; ID_LIKE=""
OSR="${SM_OS_RELEASE:-/etc/os-release}"   # SM_OS_RELEASE : essais seulement
[ -r "$OSR" ] && . "$OSR"
GESTIONNAIRE=""
case " $ID $ID_LIKE " in
  *" steamos "*) GESTIONNAIRE=steamos ;;
  *" debian "*|*" ubuntu "*) GESTIONNAIRE=apt ;;
  *" fedora "*|*" rhel "*|*" centos "*) GESTIONNAIRE=dnf ;;
  *" arch "*) GESTIONNAIRE=pacman ;;
  *" suse "*|*" opensuse "*) GESTIONNAIRE=zypper ;;
esac
if [ -z "$GESTIONNAIRE" ]; then
  for g in apt-get dnf pacman zypper; do command -v $g >/dev/null 2>&1 && { GESTIONNAIRE=${g%-get}; break; }; done
fi
# paquet <webkit|gtk|notify|icu|ssl> → nom du paquet pour ce système
paquet() {
  case "$GESTIONNAIRE:$1" in
    apt:webkit) echo libwebkit2gtk-4.1-0 ;;  apt:gtk) echo libgtk-3-0 ;;  apt:notify) echo libnotify4 ;;
    apt:icu) apt-cache search --names-only '^libicu[0-9]+$' 2>/dev/null | cut -d' ' -f1 | sort -V | tail -1 | grep . || echo libicu74 ;;
    apt:ssl) echo libssl3 ;;
    dnf:webkit) echo webkit2gtk4.1 ;;  dnf:gtk) echo gtk3 ;;  dnf:notify) echo libnotify ;;  dnf:icu) echo libicu ;;  dnf:ssl) echo openssl-libs ;;
    pacman:webkit|steamos:webkit) echo webkit2gtk-4.1 ;;  pacman:gtk|steamos:gtk) echo gtk3 ;;  pacman:notify|steamos:notify) echo libnotify ;;
    pacman:icu|steamos:icu) echo icu ;;  pacman:ssl|steamos:ssl) echo openssl ;;
    zypper:webkit) echo libwebkit2gtk-4_1-0 ;;  zypper:gtk) echo libgtk-3-0 ;;  zypper:notify) echo libnotify4 ;;
    zypper:icu) echo libicu ;;  zypper:ssl) echo libopenssl3 ;;
    *) echo "$1" ;;
  esac
}
commande_installation() {
  case "$GESTIONNAIRE" in
    apt) echo "sudo apt install $*" ;;
    dnf) echo "sudo dnf install $*" ;;
    pacman) echo "sudo pacman -S --needed $*" ;;
    zypper) echo "sudo zypper install $*" ;;
    steamos) echo "(SteamOS : système en lecture seule) sudo steamos-readonly disable && sudo pacman -S --needed $* && sudo steamos-readonly enable" ;;
    *) echo "installez avec le gestionnaire de paquets de votre distribution : $*" ;;
  esac
}

MANQUE_FENETRE=""; MANQUE_REQUIS=""
a_lib libwebkit2gtk-4.1.so.0 || MANQUE_FENETRE="$MANQUE_FENETRE $(paquet webkit)"
a_lib libgtk-3.so.0 || MANQUE_FENETRE="$MANQUE_FENETRE $(paquet gtk)"
a_lib libnotify.so.4 || MANQUE_FENETRE="$MANQUE_FENETRE $(paquet notify)"
ICU=oui; a_lib libicuuc.so || { ICU=non; MANQUE_REQUIS="$MANQUE_REQUIS $(paquet icu)"; }
SSL=oui; { a_lib libssl.so.3 || a_lib libssl.so.1.1; } || { SSL=non; MANQUE_REQUIS="$MANQUE_REQUIS $(paquet ssl)"; }
FENETRE=oui; [ -n "$MANQUE_FENETRE" ] && FENETRE=non

if [ "$VERIFIER" = oui ] || [ "$SILENCIEUX" = non ]; then
  echo "Système : ${PRETTY_NAME:-Linux} ($GESTIONNAIRE)"
  [ "$FENETRE" = oui ] && echo "  ✔ fenêtres (WebKitGTK 4.1, GTK 3, libnotify)" || echo "  ✘ fenêtres : manque$MANQUE_FENETRE"
  [ "$ICU" = oui ] && echo "  ✔ ICU (langues, dates)" || echo "  ✘ ICU : SecondMonitor démarrera en mode « sans langue » (formats anglais)"
  [ "$SSL" = oui ] && echo "  ✔ OpenSSL (HTTPS, mises à jour)" || echo "  ✘ OpenSSL : pas de HTTPS (micro depuis un autre appareil) ni de mise à jour intégrée"
fi
if [ -n "$MANQUE_FENETRE$MANQUE_REQUIS" ] && { [ "$VERIFIER" = oui ] || [ "$SILENCIEUX" = non ]; }; then
  echo
  echo "Pour tout avoir, installez ce qui manque puis relancez ./installer.sh :"
  echo "  $(commande_installation $MANQUE_FENETRE $MANQUE_REQUIS)"
  [ "$FENETRE" = non ] && echo "  En attendant, SecondMonitor s'ouvre dans le navigateur et le collecteur dans un terminal."
  echo
fi
[ "$VERIFIER" = oui ] && exit 0

# ───── Copie ─────
if [ "$SRC" != "$DEST" ]; then
  dire "Copie dans $DEST…"
  # Une version ouverte est fermée d'abord (ses fichiers vont être remplacés).
  pkill -f "$DEST/SecondMonitorHub" 2>/dev/null || true
  pkill -f "$DEST/SecondMonitor-Collecteur" 2>/dev/null || true
  rm -rf "$DEST"
  mkdir -p "$DEST"
  cp -R "$SRC/." "$DEST/"
fi
chmod 755 "$DEST/SecondMonitorHub" "$DEST/SecondMonitor-Collecteur" "$DEST"/*.sh

# ───── Raccourcis du menu ─────
if [ "$FENETRE" = oui ]; then VUE=""; COLL=""; TERM_COLL=false
else VUE=" --navigateur"; COLL=" --sans-fenetre"; TERM_COLL=true; fi
# Sans ICU, .NET refuse de démarrer : mode « sans langue » plutôt que rien du tout.
ENV=""; [ "$ICU" = non ] && ENV="env DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 "

mkdir -p "$APPS" "$ICONS/256x256/apps" "$ICONS/scalable/apps"
cp "$DEST/icone.png" "$ICONS/256x256/apps/secondmonitor.png"
[ -f "$DEST/icone.svg" ] && cp "$DEST/icone.svg" "$ICONS/scalable/apps/secondmonitor.svg"
cat > "$APPS/secondmonitor.desktop" <<EOD
[Desktop Entry]
Type=Application
Name=SecondMonitor
GenericName=Chronométrage de course
Comment=Chronométrage, stratégie et ingénieur de course IA
Exec=$ENV"$DEST/SecondMonitorHub"$VUE
Path=$DEST
Icon=secondmonitor
Terminal=false
Categories=Game;Utility;
Keywords=sim;racing;course;télémétrie;LMU;ACC;F1;
StartupNotify=true
EOD
cat > "$APPS/secondmonitor-collecteur.desktop" <<EOD
[Desktop Entry]
Type=Application
Name=SecondMonitor Collecteur
Comment=Lit le jeu et envoie les données à SecondMonitor
Exec=env WEBKIT_DISABLE_DMABUF_RENDERER=1 "$DEST/SecondMonitor-Collecteur"$COLL
Path=$DEST
Icon=secondmonitor
Terminal=$TERM_COLL
Categories=Game;Utility;
Keywords=sim;racing;collecteur;télémétrie;
EOD
chmod 644 "$APPS/secondmonitor.desktop" "$APPS/secondmonitor-collecteur.desktop"
command -v update-desktop-database >/dev/null 2>&1 && update-desktop-database "$APPS" >/dev/null 2>&1 || true
command -v gtk-update-icon-cache >/dev/null 2>&1 && gtk-update-icon-cache -q -t "$ICONS" >/dev/null 2>&1 || true

# ───── Démarrage automatique du collecteur (facultatif) ─────
if [ -z "$DEMARRAGE" ] && [ ! -f "$AUTOSTART" ]; then
  demander "Lancer le collecteur à l'ouverture de session (fenêtre réduite) ? Utile sur le PC de jeu." && DEMARRAGE=oui
fi
if [ "$DEMARRAGE" = oui ] || { [ -z "$DEMARRAGE" ] && [ -f "$AUTOSTART" ]; }; then
  mkdir -p "$(dirname "$AUTOSTART")"
  printf '[Desktop Entry]\nType=Application\nName=SecondMonitor Collecteur\nExec="%s/SecondMonitor-Collecteur"%s --reduit\nTerminal=%s\nX-GNOME-Autostart-enabled=true\n' "$DEST" "$COLL" "$TERM_COLL" > "$AUTOSTART"
  dire "✔ Le collecteur démarrera à l'ouverture de session."
elif [ "$DEMARRAGE" = non ]; then
  rm -f "$AUTOSTART"
fi

# ───── Pare-feu ─────
# PC de jeu (collecteur) : UDP 52642 entrant. Ordinateur qui affiche (SecondMonitor) : TCP 8686 et 8687 pour les autres écrans,
# UDP 8689 pour entendre les serveurs de calcul. Serveur de calcul : TCP 8686 (relais vers son Ollama, protégé par code).
PF=""
if command -v firewall-cmd >/dev/null 2>&1 && firewall-cmd --state >/dev/null 2>&1; then
  PF="sudo firewall-cmd --permanent --add-port=52642/udp --add-port=8689/udp --add-port=8686-8687/tcp && sudo firewall-cmd --reload"
elif command -v ufw >/dev/null 2>&1 && { systemctl is-active --quiet ufw 2>/dev/null || grep -qs '^ENABLED=yes' /etc/ufw/ufw.conf; }; then
  PF="sudo ufw allow 52642/udp && sudo ufw allow 8689/udp && sudo ufw allow 8686:8687/tcp"
fi
if [ -n "$PF" ]; then
  if [ "$PAREFEU" = oui ] || demander "Un pare-feu est actif. Ouvrir les ports de SecondMonitor (UDP 52642 et 8689, TCP 8686-8687) ? (sudo)"; then
    sh -c "$PF" && dire "✔ Ports ouverts dans le pare-feu." toujours || echo "Ports non ouverts. À faire à la main : $PF"
  else
    dire "Pare-feu actif : si le PC de jeu n'est pas trouvé ou si un autre écran ne se connecte pas, ouvrez les ports :"
    dire "  $PF"
  fi
fi

dire ""
dire "✔ SecondMonitor et SecondMonitor Collecteur sont dans le menu des applications."
dire "  Jeux lancés avec Proton (LMU, ACC, rFactor 2, AMS2…) : $DEST/lancer-avec-proton.sh <appid>"
dire "  Mises à jour : SecondMonitor › Réglages › Mises à jour (ou le bouton de la fenêtre du collecteur)."
dire "  Pour désinstaller : $DEST/desinstaller.sh"
exit 0
