#!/bin/bash
# Ajoute des fichiers à la release publique « v<version> » (étiquette en cours), sans jamais remplacer
# un fichier déjà publié (par OneDrop3D ou à la main). Les systèmes se construisent en parallèle :
# le premier crée la release, les suivants y ajoutent leurs fichiers.
set -e
tag="$GITHUB_REF_NAME"
v="${tag#v}"
awk -v t="## Version $v" '$0==t{f=1;next} f&&/^## Version /{exit} f&&/^---$/{exit} f' CHANGELOG.md > notes.md
[ -s notes.md ] || echo "SecondMonitor $v" > notes.md
gh release view "$tag" >/dev/null 2>&1 || \
  gh release create "$tag" --prerelease --title "SecondMonitor $v" --notes-file notes.md || true
for f in "$@"; do
  nom="$(basename "$f")"
  if gh release view "$tag" --json assets -q '.assets[].name' 2>/dev/null | grep -qxF "$nom"; then
    echo "$nom déjà publié : conservé."
    continue
  fi
  for i in 1 2 3 4 5; do
    gh release upload "$tag" "$f" && break
    sleep 10
  done
done
