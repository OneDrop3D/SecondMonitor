#!/bin/sh
# Installe SecondMonitor et SecondMonitor Collecteur pour l'utilisateur courant (aucun mot de passe demandé) :
# programmes dans ~/.local/share/secondmonitor, raccourcis dans le menu des applications.
set -e
SRC="$(cd "$(dirname "$0")" && pwd)"
DEST="${XDG_DATA_HOME:-$HOME/.local/share}/secondmonitor"
APPS="${XDG_DATA_HOME:-$HOME/.local/share}/applications"
ICONS="${XDG_DATA_HOME:-$HOME/.local/share}/icons/hicolor/256x256/apps"

if [ "$SRC" != "$DEST" ]; then
  echo "Copie dans $DEST…"
  rm -rf "$DEST"
  mkdir -p "$DEST"
  cp -R "$SRC/." "$DEST/"
fi
chmod +x "$DEST/SecondMonitorHub" "$DEST/SecondMonitor-Collecteur" "$DEST"/*.sh

# Les fenêtres utilisent WebKitGTK. Sans lui : SecondMonitor s'ouvre dans le navigateur, le collecteur dans un terminal.
FENETRE=oui
if ! (ldconfig -p 2>/dev/null | grep -q libwebkit2gtk-4.1) && ! ls /usr/lib*/libwebkit2gtk-4.1.so.0 /usr/lib/*/libwebkit2gtk-4.1.so.0 >/dev/null 2>&1; then
  FENETRE=non
fi
if [ "$FENETRE" = oui ]; then
  VUE=""; COLL=""; TERM_COLL=false
else
  VUE=" --navigateur"; COLL=" --sans-fenetre"; TERM_COLL=true
fi

mkdir -p "$APPS" "$ICONS"
cp "$DEST/icone.png" "$ICONS/secondmonitor.png"
cat > "$APPS/secondmonitor.desktop" <<EOD
[Desktop Entry]
Type=Application
Name=SecondMonitor
Comment=Chronométrage, stratégie et ingénieur de course IA
Exec="$DEST/SecondMonitorHub"$VUE
Path=$DEST
Icon=secondmonitor
Terminal=false
Categories=Game;Utility;
EOD
cat > "$APPS/secondmonitor-collecteur.desktop" <<EOD
[Desktop Entry]
Type=Application
Name=SecondMonitor Collecteur
Comment=Lit le jeu et envoie les données à SecondMonitor
Exec="$DEST/SecondMonitor-Collecteur"$COLL
Path=$DEST
Icon=secondmonitor
Terminal=$TERM_COLL
Categories=Game;Utility;
EOD
command -v update-desktop-database >/dev/null 2>&1 && update-desktop-database "$APPS" >/dev/null 2>&1 || true

echo
echo "✔ SecondMonitor et SecondMonitor Collecteur sont dans le menu des applications."
if [ "$FENETRE" = non ]; then
  echo
  echo "Pour avoir de vraies fenêtres, installez WebKitGTK puis relancez ./installer.sh :"
  echo "  Debian / Ubuntu : sudo apt install libwebkit2gtk-4.1-0"
  echo "  Fedora          : sudo dnf install webkit2gtk4.1"
  echo "  Arch            : sudo pacman -S webkit2gtk-4.1"
  echo "  (En attendant, SecondMonitor s'ouvre dans le navigateur et le collecteur dans un terminal.)"
fi
echo
echo "Jeux lancés avec Proton (LMU, ACC, rFactor 2, AMS2…) : $DEST/lancer-avec-proton.sh <appid>"
echo "Pour désinstaller : $DEST/desinstaller.sh"
