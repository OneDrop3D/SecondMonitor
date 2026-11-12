#!/bin/zsh
# Construit SecondMonitor pour tous les systèmes, depuis ce Mac (aucune machine Windows ou Linux nécessaire).
# Utilisé par OneDrop3D (Publieur › SecondMonitor) ou à la main :
#
#   ./construire.sh mac      → dist/SecondMonitor-Hub-<v>-mac-arm64.zip (+ mac-x64) et dist/mac-arm64/SecondMonitor Hub.app
#   ./construire.sh win      → dist/SecondMonitor-Collecteur-<v>-windows-x64.zip et dist/SecondMonitor-Hub-<v>-windows-x64.zip
#   ./construire.sh linux    → dist/SecondMonitor-Collecteur-<v>-linux-x64.tar.gz et dist/SecondMonitor-Hub-<v>-linux-x64.tar.gz
#   ./construire.sh tous     → les trois
#
# Hub : l'écran d'ingénieur (Mac de préférence, mais aussi Windows ou Linux).
# Collecteur : le petit programme du PC de jeu (Windows ; Linux/SteamOS).
# Tout est autonome : .NET est inclus, rien à installer chez l'utilisateur.

set -e
setopt NULL_GLOB
cd "${0:A:h}"
export PATH="/opt/homebrew/bin:/opt/homebrew/opt/node@20/bin:/usr/local/share/dotnet:$HOME/.dotnet:/usr/local/bin:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

V=$(sed -nE 's:.*<Version>([^<]+)</Version>.*:\1:p' Hub/Directory.Build.props | head -1)
CV=$(sed -nE 's:.*<Version>([^<]+)</Version>.*:\1:p' Collector/Directory.Build.props | head -1)
WV=$(sed -nE 's/.*"version": "([^"]+)".*/\1/p' Hub/web/package.json | head -1)
if [ "$V" != "$CV" ] || [ "$V" != "$WV" ]; then
  echo "Versions différentes : hub $V, collecteur $CV, interface $WV. Elles doivent être identiques." >&2
  exit 1
fi

DIST="$PWD/dist"
TMP="$DIST/.travail-${1:-tous}-$$"   # dossier propre à cette construction (OneDrop3D peut en lancer plusieurs à la fois)
mkdir -p "$DIST"
echo "SecondMonitor $V"

etape() { echo; echo "== $*"; }

# Verrou pour la partie commune (interface web + hub) : deux constructions en parallèle ne la touchent pas en même temps.
VERROU="$DIST/.verrou-hub"
TIENT_VERROU=""
prendre_verrou() { until mkdir "$VERROU" 2>/dev/null; do sleep 1; done; TIENT_VERROU=1; }
rendre_verrou() { if [ -n "$TIENT_VERROU" ]; then rmdir "$VERROU" 2>/dev/null || true; TIENT_VERROU=""; fi; }
trap 'rendre_verrou; rm -rf "$TMP"' EXIT

interface_web() {
  if [ -n "$INTERFACE_FAITE" ]; then return; fi
  etape "Interface web"
  (
    cd Hub/web
    if [ ! -d node_modules ]; then npm ci --no-audit --no-fund; fi
    npm run build
  )
  INTERFACE_FAITE=1
}

# hub <rid> <dossier> : serveur autonome (un seul exécutable + l'interface web à côté)
hub() {
  prendre_verrou
  INTERFACE_FAITE=""
  interface_web
  etape "Hub $1"
  rm -rf "$2"
  dotnet publish Hub/src/SecondMonitor.Hub -c Release -r "$1" --self-contained \
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true \
    -p:DebugType=embedded -p:SatelliteResourceLanguages=fr -o "$2" -v q -nologo
  rm -f "$2"/*.pdb
  rendre_verrou
}

# collecteur <rid> <dossier>
collecteur() {
  etape "Collecteur $1"
  rm -rf "$2"
  dotnet publish Collector/src -c Release -r "$1" -o "$2" -v q -nologo
  cp Collector/LISEZ-MOI.md "$2/"
}

construire_mac() {
  for arch in x64 arm64; do  # arm64 en dernier : c'est l'application que OneDrop3D installe sur ce Mac
    hub "osx-$arch" "$TMP/hub-osx-$arch"
    etape "Application Mac ($arch)"
    local dossier="$DIST/mac-$arch"
    local app="$dossier/SecondMonitor Hub.app"
    rm -rf "$dossier"; mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"
    cp -R "$TMP/hub-osx-$arch/." "$app/Contents/MacOS/"
    cp packaging/SecondMonitor.icns "$app/Contents/Resources/"
    cat > "$app/Contents/MacOS/SecondMonitor-Hub" <<'EOS'
#!/bin/zsh
# Démarre le hub et ouvre l'interface dans le navigateur. Pour l'arrêter : Réglages › Quitter le hub.
DIR="${0:A:h}"
exec "$DIR/SecondMonitorHub" --ouvrir
EOS
    chmod +x "$app/Contents/MacOS/SecondMonitor-Hub" "$app/Contents/MacOS/SecondMonitorHub"
    cat > "$app/Contents/Info.plist" <<EOS
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key><string>SecondMonitor Hub</string>
  <key>CFBundleDisplayName</key><string>SecondMonitor Hub</string>
  <key>CFBundleIdentifier</key><string>fr.onedrop3d.secondmonitor-hub</string>
  <key>CFBundleExecutable</key><string>SecondMonitor-Hub</string>
  <key>CFBundleIconFile</key><string>SecondMonitor</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleShortVersionString</key><string>$V</string>
  <key>CFBundleVersion</key><string>$V</string>
  <key>LSMinimumSystemVersion</key><string>12.0</string>
  <key>LSApplicationCategoryType</key><string>public.app-category.sports</string>
  <key>NSHighResolutionCapable</key><true/>
</dict>
</plist>
EOS
    # Signature : identité OneDrop3D si elle existe (autorisations gardées d'une version à l'autre), sinon ad hoc.
    local id="${ONEDROP3D_SIGN_IDENTITY:-$(security find-identity -v -p codesigning 2>/dev/null | grep -oE '"(Multibook|ClickDeck|OneDrop3D)[^"]*"' | head -1 | tr -d '"')}"
    codesign --force --deep --sign "${id:--}" --identifier fr.onedrop3d.secondmonitor-hub "$app" >/dev/null 2>&1 || codesign --force --deep --sign - "$app"
    codesign --verify --deep --strict "$app"
    rm -f "$DIST/SecondMonitor-Hub-$V-mac-$arch.zip"
    ditto -c -k --sequesterRsrc --keepParent "$app" "$DIST/SecondMonitor-Hub-$V-mac-$arch.zip"
    echo "→ dist/SecondMonitor-Hub-$V-mac-$arch.zip"
  done
  rm -rf "$DIST/mac-x64"  # seule l'application de ce Mac reste dépliée (pour « Installer sur ce Mac »)
}

construire_win() {
  hub win-x64 "$TMP/hub-win"
  cat > "$TMP/hub-win/Lancer SecondMonitor Hub.cmd" <<'EOS'
@echo off
rem Démarre le hub et ouvre l'interface dans le navigateur. Laissez la fenêtre ouverte (ou réduite).
cd /d "%~dp0"
SecondMonitorHub.exe --ouvrir
EOS
  rm -rf "$TMP/zip-hub"; mkdir -p "$TMP/zip-hub"; cp -R "$TMP/hub-win" "$TMP/zip-hub/SecondMonitor Hub"
  rm -f "$DIST/SecondMonitor-Hub-$V-windows-x64.zip"
  (cd "$TMP/zip-hub" && zip -qr -X "$DIST/SecondMonitor-Hub-$V-windows-x64.zip" "SecondMonitor Hub")
  echo "→ dist/SecondMonitor-Hub-$V-windows-x64.zip"

  collecteur win-x64 "$TMP/coll-win"
  rm -rf "$TMP/zip-coll"; mkdir -p "$TMP/zip-coll"; cp -R "$TMP/coll-win" "$TMP/zip-coll/SecondMonitor Collecteur"
  rm -f "$DIST/SecondMonitor-Collecteur-$V-windows-x64.zip"
  (cd "$TMP/zip-coll" && zip -qr -X "$DIST/SecondMonitor-Collecteur-$V-windows-x64.zip" "SecondMonitor Collecteur")
  echo "→ dist/SecondMonitor-Collecteur-$V-windows-x64.zip"
}

construire_linux() {
  hub linux-x64 "$TMP/hub-linux"
  cat > "$TMP/hub-linux/lancer-hub.sh" <<'EOS'
#!/bin/sh
# Démarre le hub et ouvre l'interface dans le navigateur.
cd "$(dirname "$0")" && exec ./SecondMonitorHub --ouvrir
EOS
  chmod +x "$TMP/hub-linux/lancer-hub.sh" "$TMP/hub-linux/SecondMonitorHub"
  rm -rf "$TMP/tar-hub"; mkdir -p "$TMP/tar-hub"; cp -R "$TMP/hub-linux" "$TMP/tar-hub/secondmonitor-hub"
  tar -C "$TMP/tar-hub" -czf "$DIST/SecondMonitor-Hub-$V-linux-x64.tar.gz" secondmonitor-hub
  echo "→ dist/SecondMonitor-Hub-$V-linux-x64.tar.gz"

  # Linux/SteamOS : collecteur Linux (F1, Forza) + collecteur Windows à lancer dans Proton (LMU, ACC, rF2, AMS2…)
  collecteur linux-x64 "$TMP/coll-linux"
  collecteur win-x64 "$TMP/coll-linux/win-x64"
  rm -f "$TMP/coll-linux/win-x64/LISEZ-MOI.md"
  cp Collector/linux/lancer-avec-proton.sh "$TMP/coll-linux/"
  chmod +x "$TMP/coll-linux/SecondMonitor-Collecteur" "$TMP/coll-linux/lancer-avec-proton.sh"
  rm -rf "$TMP/tar-coll"; mkdir -p "$TMP/tar-coll"; cp -R "$TMP/coll-linux" "$TMP/tar-coll/secondmonitor-collecteur"
  tar -C "$TMP/tar-coll" -czf "$DIST/SecondMonitor-Collecteur-$V-linux-x64.tar.gz" secondmonitor-collecteur
  echo "→ dist/SecondMonitor-Collecteur-$V-linux-x64.tar.gz"
}

case "${1:-tous}" in
  mac) construire_mac ;;
  win|windows) construire_win ;;
  linux) construire_linux ;;
  tous|all) construire_mac; construire_win; construire_linux ;;
  *) echo "Usage : $0 mac|win|linux|tous" >&2; exit 1 ;;
esac

rm -rf "$TMP"
etape "Terminé : SecondMonitor $V"
ls -lh "$DIST" | grep -E "SecondMonitor-.*$V" || true
