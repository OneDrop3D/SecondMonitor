#!/bin/sh
# Retire SecondMonitor et SecondMonitor Collecteur (vos réglages sont gardés dans ~/.config).
D="${XDG_DATA_HOME:-$HOME/.local/share}"
rm -f "$D/applications/secondmonitor.desktop" "$D/applications/secondmonitor-collecteur.desktop" \
      "$D/icons/hicolor/256x256/apps/secondmonitor.png" "$HOME/.config/autostart/secondmonitor-collecteur.desktop"
rm -rf "$D/secondmonitor"
echo "SecondMonitor est désinstallé. Réglages conservés : ~/.config/SecondMonitorHub et ~/.config/SecondMonitor Collecteur"
