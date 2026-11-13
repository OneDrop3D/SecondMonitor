#!/bin/bash
#
# Identité de signature STABLE pour SecondMonitor (Mac), à lancer UNE SEULE FOIS sur le Mac qui publie.
#
# ── Le problème ─────────────────────────────────────────────────────────────
# Sans certificat, les applications sont signées « ad hoc » : leur « exigence désignée » est l'empreinte du code
# (cdhash), qui change à chaque version. macOS rattache les autorisations (micro de l'ingénieur IA, réseau local,
# pare-feu) à cette exigence : chaque nouvelle version les redemande, et une case déjà cochée ne vaut plus.
# Les applications installées jusqu'ici (construites sur GitHub) sont dans ce cas.
#
# ── Ce que fait ce script ───────────────────────────────────────────────────
# 1. Crée un certificat de signature de code auto-signé « OneDrop3D SecondMonitor » (10 ans) dans le trousseau :
#    ./construire.sh mac s'en sert tout seul (fonction signer).
# 2. Avec --github : l'envoie aussi, chiffré, dans les secrets du dépôt OneDrop3D/SecondMonitor
#    (MAC_SIGN_P12, MAC_SIGN_P12_PASSWORD, MAC_SIGN_IDENTITY) : les constructions Mac de GitHub signent alors
#    avec la MÊME identité (voir .github/workflows/build.yml).
# Résultat : l'autorisation du micro est donnée une dernière fois, puis elle survit aux mises à jour.
#
# Ce n'est pas un certificat Apple : Gatekeeper demande toujours « Ouvrir quand même » à la PREMIÈRE installation
# d'un .dmg téléchargé par le navigateur. Les mises à jour intégrées, elles, ne passent plus jamais par là
# (téléchargement sans quarantaine, voir Hub/src/SecondMonitor.Hub.Core/Update).
set -euo pipefail
NOM="OneDrop3D SecondMonitor"
DEPOT="OneDrop3D/SecondMonitor"
TROUSSEAU="$HOME/Library/Keychains/login.keychain-db"
GITHUB=0
[ "${1:-}" = "--github" ] && GITHUB=1

[ "$(uname -s)" = "Darwin" ] || { echo "Ce script ne sert que sous macOS."; exit 0; }

TRAVAIL="$(mktemp -d)"
trap 'rm -rf "$TRAVAIL"' EXIT
MDP="$(openssl rand -hex 16)"

# Certificat auto-signé : il n'apparaît pas dans « find-identity -v » (non approuvé), on cherche donc le certificat.
if security find-certificate -c "$NOM" "$TROUSSEAU" >/dev/null 2>&1; then
  echo "✅ L'identité « $NOM » existe déjà dans le trousseau."
  if [ "$GITHUB" = 1 ]; then
    echo "Pour l'envoyer à GitHub, il faut le fichier .p12 d'origine : supprimez l'identité (Trousseaux d'accès) et relancez ce script avec --github."
    exit 1
  fi
  exit 0
fi

cat > "$TRAVAIL/cert.conf" <<CONF
[ req ]
distinguished_name = dn
prompt = no
x509_extensions = ext
[ dn ]
CN = $NOM
[ ext ]
basicConstraints = critical, CA:false
keyUsage = critical, digitalSignature
extendedKeyUsage = critical, codeSigning
CONF
openssl req -x509 -newkey rsa:2048 -nodes -days 3650 -keyout "$TRAVAIL/cle.pem" -out "$TRAVAIL/cert.pem" -config "$TRAVAIL/cert.conf" 2>/dev/null
# -legacy : sans lui, « security import » ne sait pas lire le PKCS#12 d'OpenSSL 3.
openssl pkcs12 -export -legacy -out "$TRAVAIL/identite.p12" -inkey "$TRAVAIL/cle.pem" -in "$TRAVAIL/cert.pem" -passout "pass:$MDP" 2>/dev/null
security import "$TRAVAIL/identite.p12" -k "$TROUSSEAU" -P "$MDP" -T /usr/bin/codesign
# Contrôle : signer un petit fichier avec la nouvelle identité (macOS peut demander l'accès au trousseau : « Toujours autoriser »).
cp /usr/bin/true "$TRAVAIL/essai"
codesign --force --sign "$NOM" "$TRAVAIL/essai" 2>/dev/null || { echo "❌ Identité inutilisable pour la signature." >&2; exit 1; }
echo "✅ Identité « $NOM » créée : ./construire.sh mac l'utilisera."

if [ "$GITHUB" = 1 ]; then
  command -v gh >/dev/null || { echo "❌ gh (GitHub CLI) introuvable." >&2; exit 1; }
  base64 -i "$TRAVAIL/identite.p12" | gh secret set MAC_SIGN_P12 --repo "$DEPOT"
  printf '%s' "$MDP" | gh secret set MAC_SIGN_P12_PASSWORD --repo "$DEPOT"
  printf '%s' "$NOM" | gh secret set MAC_SIGN_IDENTITY --repo "$DEPOT"
  echo "✅ Secrets envoyés à $DEPOT : les constructions Mac de GitHub signeront avec la même identité."
fi
