#!/bin/zsh
# Construit SecondMonitor pour tous les systèmes, depuis ce Mac (aucune machine Windows ou Linux nécessaire,
# sauf l'installateur Windows .exe, fabriqué par Inno Setup sur GitHub ou sur un PC Windows).
# Utilisé par OneDrop3D (Publieur › SecondMonitor) ou à la main :
#
#   ./construire.sh mac      → dist/SecondMonitor-<v>-mac-arm64.dmg (+ mac-x64) : les deux applications à glisser dans Applications
#   ./construire.sh win      → dist/SecondMonitor-<v>-windows-x64.zip (les deux applications, sans installation)
#                              + dist/SecondMonitor-<v>-windows-x64-installation.exe si Inno Setup (iscc) est disponible
#   ./construire.sh linux    → dist/SecondMonitor-<v>-linux-x64.tar.gz (les deux applications + installer.sh)
#   ./construire.sh tous     → les trois
#
# Chaque téléchargement installe DEUX applications :
#   SecondMonitor            l'application qui affiche les données (chronométrage, stratégie, ingénieur IA)
#   SecondMonitor Collecteur la petite application du PC de jeu qui lit le jeu et envoie les données
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

# Verrou pour la partie commune (interface web) : deux constructions en parallèle ne la touchent pas en même temps.
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

# hub <rid> <dossier> [unique] : SecondMonitor (l'application qui affiche). « unique » = un seul exécutable (Mac).
hub() {
  prendre_verrou
  INTERFACE_FAITE=""
  interface_web
  etape "SecondMonitor $1"
  rm -rf "$2"
  local unique=()
  if [ "$3" = "unique" ]; then unique=(-p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true); fi
  dotnet publish Hub/src/SecondMonitor.Hub -c Release -r "$1" --self-contained "${unique[@]}" \
    -p:DebugType=embedded -p:SatelliteResourceLanguages=fr -o "$2" -v q -nologo
  rm -f "$2"/*.pdb
  rendre_verrou
}

# collecteur <rid> <dossier> [unique]
collecteur() {
  etape "SecondMonitor Collecteur $1"
  rm -rf "$2"
  local unique=(-p:PublishSingleFile=false)
  if [ "$3" = "unique" ]; then unique=(); fi
  dotnet publish Collector/src -c Release -r "$1" -o "$2" -v q -nologo "${unique[@]}"
  rm -f "$2"/*.pdb
  cp Collector/LISEZ-MOI.md "$2/"
}

# ensemble <rid> <dossier> : les deux applications dans un même dossier, avec un seul exemplaire de .NET.
# Les deux sont construites avec le même SDK et les mêmes paquets : un fichier commun différent est une erreur.
ensemble() {
  hub "$1" "$TMP/ens-hub-$1"
  collecteur "$1" "$TMP/ens-coll-$1"
  etape "Assemblage des deux applications ($1)"
  rm -rf "$2"; mkdir -p "$2"
  cp -R "$TMP/ens-hub-$1/." "$2/"
  (cd "$TMP/ens-coll-$1" && find . -type f) | while read -r f; do
    if [ -f "$2/$f" ] && ! cmp -s "$2/$f" "$TMP/ens-coll-$1/$f"; then
      echo "Conflit : $f diffère entre SecondMonitor et le collecteur." >&2
      exit 1
    fi
    mkdir -p "$2/$(dirname "$f")"
    cp "$TMP/ens-coll-$1/$f" "$2/$f"
  done
}

# Info.plist d'une application Mac : plist <app> <nom> <identifiant> <exécutable> <micro oui|non>
plist() {
  local micro=""
  if [ "$5" = "oui" ]; then
    micro="  <key>NSMicrophoneUsageDescription</key><string>Le micro sert à parler à l'ingénieur IA (bouton radio).</string>"
  fi
  cat > "$1/Contents/Info.plist" <<EOS
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key><string>$2</string>
  <key>CFBundleDisplayName</key><string>$2</string>
  <key>CFBundleIdentifier</key><string>$3</string>
  <key>CFBundleExecutable</key><string>$4</string>
  <key>CFBundleIconFile</key><string>SecondMonitor</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleShortVersionString</key><string>$V</string>
  <key>CFBundleVersion</key><string>$V</string>
  <key>LSMinimumSystemVersion</key><string>12.0</string>
  <key>LSApplicationCategoryType</key><string>public.app-category.sports</string>
  <key>NSHighResolutionCapable</key><true/>
  <key>NSLocalNetworkUsageDescription</key><string>SecondMonitor échange les données de course avec le PC de jeu sur le réseau local.</string>
$micro
</dict>
</plist>
EOS
}

signer() {
  # Identité STABLE si elle existe : les autorisations (micro, réseau local) sont gardées d'une version à l'autre.
  # Sans elle, signature ad hoc : macOS redemande ces autorisations à chaque version.
  # Créer l'identité (une fois) : packaging/mac/creer-identite-signature.sh [--github]
  local ids="$(security find-identity -v -p codesigning 2>/dev/null)"
  # Certificat auto-signé : macOS ne l'« approuve » pas (absent de find-identity -v), mais codesign s'en sert très bien
  # et l'exigence désignée (empreinte du certificat) reste la même d'une version à l'autre.
  local id="${ONEDROP3D_SIGN_IDENTITY:-}"
  [ -n "$id" ] || ! security find-certificate -c "OneDrop3D SecondMonitor" >/dev/null 2>&1 || id="OneDrop3D SecondMonitor"
  [ -n "$id" ] || id="$(printf '%s\n' "$ids" | grep -oE '"(Multibook|ClickDeck|OneDrop3D)[^"]*"' | head -1 | tr -d '"')"
  echo "Signature : ${id:-ad hoc (les autorisations seront redemandées à chaque version)}"
  codesign --force --deep --sign "${id:--}" --identifier "$2" "$1" >/dev/null 2>&1 || codesign --force --deep --sign - --identifier "$2" "$1"
  codesign --verify --deep --strict "$1"
}

construire_mac() {
  for arch in x64 arm64; do  # arm64 en dernier : ce sont les applications que OneDrop3D installe sur ce Mac
    hub "osx-$arch" "$TMP/hub-osx-$arch" unique
    collecteur "osx-$arch" "$TMP/coll-osx-$arch" unique
    etape "Applications Mac ($arch)"
    local dossier="$DIST/mac-$arch"
    rm -rf "$dossier"; mkdir -p "$dossier"

    local app="$dossier/SecondMonitor.app"
    mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"
    cp -R "$TMP/hub-osx-$arch/." "$app/Contents/MacOS/"
    cp packaging/SecondMonitor.icns "$app/Contents/Resources/"
    plist "$app" "SecondMonitor" "fr.onedrop3d.secondmonitor" "SecondMonitorHub" oui
    chmod +x "$app/Contents/MacOS/SecondMonitorHub"
    signer "$app" fr.onedrop3d.secondmonitor

    local coll="$dossier/SecondMonitor Collecteur.app"
    mkdir -p "$coll/Contents/MacOS" "$coll/Contents/Resources"
    cp -R "$TMP/coll-osx-$arch/." "$coll/Contents/MacOS/"
    rm -f "$coll/Contents/MacOS/LISEZ-MOI.md"
    cp packaging/SecondMonitor.icns "$coll/Contents/Resources/"
    plist "$coll" "SecondMonitor Collecteur" "fr.onedrop3d.secondmonitor-collecteur" "SecondMonitor-Collecteur" non
    chmod +x "$coll/Contents/MacOS/SecondMonitor-Collecteur"
    signer "$coll" fr.onedrop3d.secondmonitor-collecteur

    # Image disque : les deux applications et un raccourci vers Applications, à glisser.
    local img="$TMP/dmg-$arch"
    rm -rf "$img"; mkdir -p "$img"
    ditto "$app" "$img/SecondMonitor.app"
    ditto "$coll" "$img/SecondMonitor Collecteur.app"
    ln -s /Applications "$img/Applications"
    rm -f "$DIST/SecondMonitor-$V-mac-$arch.dmg"
    hdiutil create -quiet -volname "SecondMonitor $V" -srcfolder "$img" -ov -format UDZO "$DIST/SecondMonitor-$V-mac-$arch.dmg"
    echo "→ dist/SecondMonitor-$V-mac-$arch.dmg"
  done
  rm -rf "$DIST/mac-x64"  # seules les applications de ce Mac restent dépliées (pour « Installer sur ce Mac »)
}

construire_win() {
  local dossier="$DIST/windows-x64/SecondMonitor"
  ensemble win-x64 "$dossier"
  cp packaging/windows/LISEZ-MOI.txt "$dossier/"
  printf '%s\n' "$V" > "$dossier/version.txt"   # lu par la mise à jour intégrée
  rm -f "$DIST/SecondMonitor-$V-windows-x64.zip"
  (cd "$DIST/windows-x64" && zip -qr -X "$DIST/SecondMonitor-$V-windows-x64.zip" SecondMonitor)
  echo "→ dist/SecondMonitor-$V-windows-x64.zip"

  # Installateur : Inno Setup (sur Windows, ou sur Mac/Linux avec Wine).
  local iscc=""
  if command -v iscc >/dev/null 2>&1; then iscc="iscc"; fi
  if [ -z "$iscc" ] && [ -n "$ISCC_EXE" ]; then iscc="$ISCC_EXE"; fi
  if [ -n "$iscc" ]; then
    etape "Installateur Windows"
    $iscc /Qp "/DVersion=$V" "/DSource=$dossier" "/DSortie=$DIST" packaging/windows/SecondMonitor.iss
    echo "→ dist/SecondMonitor-$V-windows-x64-installation.exe"
  else
    echo "(Installateur .exe non construit ici : Inno Setup absent. GitHub le construit automatiquement.)"
  fi
}

construire_linux() {
  local dossier="$TMP/linux/secondmonitor"
  ensemble linux-x64 "$dossier"
  # Jeux lancés avec Proton (LMU, ACC, rFactor 2, AMS2…) : collecteur Windows à lancer dans le préfixe du jeu.
  collecteur win-x64 "$dossier/collecteur-windows" unique
  rm -f "$dossier/collecteur-windows/LISEZ-MOI.md"
  cp Collector/linux/lancer-avec-proton.sh packaging/linux/installer.sh packaging/linux/desinstaller.sh packaging/linux/LISEZ-MOI-LINUX.md "$dossier/"
  cp packaging/icone.png packaging/icone.svg "$dossier/"
  printf '%s\n' "$V" > "$dossier/version.txt"   # lu par la mise à jour intégrée
  chmod 755 "$dossier/SecondMonitorHub" "$dossier/SecondMonitor-Collecteur" "$dossier"/*.sh
  chmod -R u+rwX,go+rX,go-w "$dossier"
  rm -f "$DIST/SecondMonitor-$V-linux-x64.tar.gz"
  # Archive propre aussi depuis un Mac : ni fichiers « ._ » ni attributs étendus, propriétaire neutre.
  local options=(--owner=0 --group=0)
  if tar --version 2>/dev/null | grep -q bsdtar; then options=(--uid 0 --gid 0 --no-mac-metadata --no-xattrs); fi
  COPYFILE_DISABLE=1 tar "${options[@]}" -C "$TMP/linux" -czf "$DIST/SecondMonitor-$V-linux-x64.tar.gz" secondmonitor
  echo "→ dist/SecondMonitor-$V-linux-x64.tar.gz"
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
ls -lh "$DIST" | grep -E "SecondMonitor-$V" || true
