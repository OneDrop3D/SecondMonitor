#!/bin/zsh
# Double-cliquez sur ce fichier (Finder) pour démarrer le hub sur le Mac.
# Au premier lancement : compile l'interface et le hub (quelques minutes), puis ouvre le navigateur.
cd "$(dirname "$0")"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "Le SDK .NET est nécessaire. Installation via Homebrew : brew install dotnet"
  if command -v brew >/dev/null 2>&1; then brew install dotnet; else echo "Installez Homebrew (https://brew.sh) puis relancez."; read; exit 1; fi
fi

if [ ! -f src/SecondMonitor.Hub/wwwroot/index.html ]; then
  if ! command -v npm >/dev/null 2>&1; then
    echo "Node.js est nécessaire pour construire l'interface : brew install node"; read; exit 1
  fi
  (cd web && npm install --no-audit --no-fund && npm run build) || { echo "Échec de la construction de l'interface"; read; exit 1; }
fi

PORT=8686
( sleep 6; open "http://localhost:$PORT" ) &
echo "SecondMonitor Hub démarre… (fermez cette fenêtre pour l'arrêter)"
dotnet run -c Release --project src/SecondMonitor.Hub -- "$@"
