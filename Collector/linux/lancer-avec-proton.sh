#!/bin/bash
# SecondMonitor Collecteur sur Linux / SteamOS, pour les jeux qui tournent avec Proton.
#
# Les jeux Windows (LMU, ACC, rFactor 2, AMS2, Assetto Corsa, RaceRoom…) publient leurs données dans une « mémoire partagée »
# qui n'existe qu'à l'intérieur de leur environnement Proton. On lance donc la version Windows du collecteur
# DANS le même environnement, une fois le jeu démarré.
#
# Usage : ./lancer-avec-proton.sh <appid Steam du jeu>
#   Le Mans Ultimate 2399420 · ACC 805550 · rFactor 2 365960 · Automobilista 2 1066890
#   Assetto Corsa 244210 · RaceRoom 211500 · Project CARS 2 378860
#   (l'appid est le nombre dans l'adresse de la page Steam du jeu)
#
# Dans Proton, le collecteur tourne sans fenêtre : son journal s'affiche dans ce terminal.
#
# Prérequis : protontricks (sur SteamOS : flatpak install flathub com.github.Matoking.protontricks)

set -e
APPID="$1"
if [ -z "$APPID" ]; then
  echo "Usage : $0 <appid Steam du jeu>   (ex. 2399420 pour Le Mans Ultimate)"
  exit 1
fi

DIR="$(cd "$(dirname "$0")" && pwd)"
EXE="$DIR/collecteur-windows/SecondMonitor-Collecteur.exe"
[ -f "$EXE" ] || EXE="$DIR/win-x64/SecondMonitor-Collecteur.exe"
[ -f "$EXE" ] || EXE="$DIR/SecondMonitor-Collecteur.exe"
if [ ! -f "$EXE" ]; then
  echo "SecondMonitor-Collecteur.exe introuvable (dossier collecteur-windows à côté de ce script)."
  exit 1
fi

echo "Lancement du collecteur dans l'environnement Proton du jeu $APPID… (le jeu doit déjà tourner)"
if command -v protontricks-launch >/dev/null 2>&1; then
  exec protontricks-launch --appid "$APPID" "$EXE" --sans-fenetre "${@:2}"
elif flatpak info com.github.Matoking.protontricks >/dev/null 2>&1; then
  exec flatpak run --command=protontricks-launch com.github.Matoking.protontricks --appid "$APPID" "$EXE" --sans-fenetre "${@:2}"
else
  echo "protontricks n'est pas installé."
  echo "Sur SteamOS (mode bureau) : flatpak install flathub com.github.Matoking.protontricks"
  exit 1
fi
