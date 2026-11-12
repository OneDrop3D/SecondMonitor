#!/bin/zsh
# Construit le collecteur pour Windows et Linux (fichiers prêts à copier sur le PC de jeu) dans Collector/dist.
cd "$(dirname "$0")/src" || exit 1
for rid in win-x64 linux-x64; do
  echo "→ $rid"
  dotnet publish -c Release -r $rid -o ../dist/$rid || exit 1
done
cp ../linux/lancer-avec-proton.sh ../dist/linux-x64/
cp ../LISEZ-MOI.md ../dist/win-x64/ 2>/dev/null; cp ../LISEZ-MOI.md ../dist/linux-x64/ 2>/dev/null
echo "Terminé : Collector/dist/win-x64 (PC Windows) et Collector/dist/linux-x64 (Linux / SteamOS)."
