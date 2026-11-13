#!/bin/sh
# Retire SecondMonitor et SecondMonitor Collecteur.
#   ./desinstaller.sh          programmes, raccourcis, icônes, démarrage automatique (réglages gardés)
#   ./desinstaller.sh --tout   aussi les réglages, enregistrements et téléchargements de mise à jour
D="${XDG_DATA_HOME:-$HOME/.local/share}"
CONF="${XDG_CONFIG_HOME:-$HOME/.config}"
CACHE="${XDG_CACHE_HOME:-$HOME/.cache}"
DEST="$D/secondmonitor"
TOUT=non
[ "${1:-}" = "--tout" ] && TOUT=oui

# Les programmes ouverts sont fermés d'abord.
pkill -f "$DEST/SecondMonitorHub" 2>/dev/null
pkill -f "$DEST/SecondMonitor-Collecteur" 2>/dev/null

rm -f "$D/applications/secondmonitor.desktop" "$D/applications/secondmonitor-collecteur.desktop" \
      "$D/icons/hicolor/256x256/apps/secondmonitor.png" "$D/icons/hicolor/scalable/apps/secondmonitor.svg" \
      "$HOME/.config/autostart/secondmonitor-collecteur.desktop"
rm -rf "$DEST" "$DEST.ancienne" "$DEST.nouvelle" "$CACHE/secondmonitor"
command -v update-desktop-database >/dev/null 2>&1 && update-desktop-database "$D/applications" >/dev/null 2>&1

if [ "$TOUT" = oui ]; then
  rm -rf "$CONF/SecondMonitorHub" "$CONF/SecondMonitor Collecteur"
  echo "SecondMonitor est désinstallé, réglages et enregistrements compris."
else
  echo "SecondMonitor est désinstallé. Réglages conservés : $CONF/SecondMonitorHub et $CONF/SecondMonitor Collecteur"
  echo "(./desinstaller.sh --tout les retire aussi)."
fi
echo "Si vous aviez ouvert les ports du pare-feu : sudo ufw delete allow 52642/udp ; sudo ufw delete allow 8686:8687/tcp"
echo "  (firewalld : sudo firewall-cmd --permanent --remove-port=52642/udp --remove-port=8686-8687/tcp && sudo firewall-cmd --reload)"
