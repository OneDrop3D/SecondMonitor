#!/usr/bin/env python3
"""
Géométrie des voies des stands (OpenStreetMap, ODbL, © contributeurs OpenStreetMap) pour estimer le temps perdu au stand
quand rien n'est publié pour un circuit (voir Hub/src/SecondMonitor.Hub.Core/Track/Library/pitlane.json).

Pour chaque circuit de la bibliothèque :
  - le tracé de course (bacinger/f1-circuits pour les circuits de F1, boucle OSM pour les autres, comme circuits.py) ;
  - les voies des stands (service=pit_lane, raceway=pit_lane, ou nom « pit », « boxengasse »…) ;
  - les points où la voie des stands rejoint la piste : la paire la plus éloignée le long de la piste donne l'entrée
    et la sortie ; la longueur de la voie (Lp, plus court chemin dans le graphe des voies des stands) et la longueur
    de piste contournée (Ls, la plus courte des deux portions de piste entre l'entrée et la sortie).

Estimation (sans l'arrêt), pour les circuits où rien n'est publié : perte = Lz / v_limite − B × Ls + C, où Lz est la zone
au limiteur (longueur publiée par une série, sinon A × Lp), B ≈ 1 / vitesse moyenne en piste le long des stands, C le
freinage et la réaccélération ; A, B et C sont calés sur les pertes publiées (par défaut celles de la F1, voir --calage).

Usage :
  tools/circuits_stands.py --bacinger f1-circuits.geojson [--cache DOSSIER] [--sortie stands-osm.json] [--hors-ligne]
                           [--pitlane ../src/SecondMonitor.Hub.Core/Track/Library/pitlane.json] [--calage F1] [--vitesse 60]
  (f1-circuits.geojson : https://github.com/bacinger/f1-circuits ; avec --pitlane, seules les valeurs « estimé » sont réécrites)
Aucune donnée personnelle n'est envoyée : seulement la requête Overpass et un User-Agent générique.
"""
import argparse, heapq, json, math, os, re, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import circuits_catalogue as cat  # noqa: E402
import circuits_osm as osm  # noqa: E402
import circuits as fab  # noqa: E402

HERE = os.path.dirname(os.path.abspath(__file__))
JUNCTION = 30.0  # m : un nœud de voie des stands à moins de 30 m de la piste est une jonction
# Voies des stands secondaires (Spa : « Support Pit Lane » des courses annexes) ou anciennes : jamais la voie de la course.
SECONDARY = re.compile(r"\b(support|old|ancienne?|alte|vecchia|disused|historic|secondary|f3|truck|paddock|kart|karting)\b", re.I)


def pit_graph(doc, proj):
    """Graphe des voies des stands : {nœud: [(voisin, longueur, voie OSM)]} et positions projetées."""
    nodes = {e["id"]: e for e in doc["elements"] if e["type"] == "node"}
    adj, pos = {}, {}
    for w in doc["elements"]:
        tags = w.get("tags", {})
        if w["type"] != "way" or not osm.is_pit(tags) or tags.get("area") == "yes":
            continue
        if tags.get("disused") == "yes" or tags.get("abandoned") == "yes" or SECONDARY.search(tags.get("name", "")):
            continue
        ids = [n for n in w["nodes"] if n in nodes]
        for n in ids:
            pos[n] = proj(nodes[n]["lat"], nodes[n]["lon"])
        for a, b in zip(ids, ids[1:]):
            d = math.dist(pos[a], pos[b])
            adj.setdefault(a, []).append((b, d, w["id"]))
            adj.setdefault(b, []).append((a, d, w["id"]))
    return adj, pos


def shortest(adj, src, prev=None):
    """Distances depuis src dans le graphe ; prev (facultatif) reçoit {nœud: (précédent, voie OSM)}."""
    dist = {src: 0.0}
    q = [(0.0, src)]
    while q:
        d, u = heapq.heappop(q)
        if d > dist.get(u, math.inf):
            continue
        for v, w, way in adj.get(u, ()):
            nd = d + w
            if nd < dist.get(v, math.inf):
                dist[v] = nd
                if prev is not None:
                    prev[v] = (u, way)
                heapq.heappush(q, (nd, v))
    return dist


def path_ways(prev, src, dst):
    ways = []
    n = dst
    while n != src and n in prev:
        n, way = prev[n]
        if way not in ways:
            ways.append(way)
    return list(reversed(ways))


def measure(doc, proj, pts):
    """dict(Lp, Ls, entrée, sortie, méthode) ou None si OSM ne décrit pas la voie des stands."""
    adj, pos = pit_graph(doc, proj)
    if not adj:
        return None
    cum = osm.cumulative(pts)
    total = cum[-1]
    near = []
    for n, p in pos.items():
        s, d = osm.project_on_loop(pts, cum, p)
        if d < JUNCTION:
            near.append((n, s))
    best = None
    for i, (u, su) in enumerate(near):
        prev = {}
        dist = shortest(adj, u, prev)
        for v, sv in near[i + 1:]:
            if v not in dist:
                continue
            ls = min((sv - su) % total, (su - sv) % total)
            lp = dist[v]
            if ls < 150 or ls > 0.4 * total or lp < 0.6 * ls or lp > 2.0 * ls + 300:
                continue
            if best is None or ls > best["Ls"]:
                best = dict(Lp=round(lp), Ls=round(ls), methode="jonctions OSM", voies=path_ways(prev, u, v))
    if best:
        return best
    # Voie non reliée à la piste dans OSM : étendue projetée et plus long chemin de la voie.
    spans = osm.pit_spans(doc, proj, pts, cum)
    if not spans:
        return None
    a, b = spans[0]
    ls = (b - a) % total
    lp = 0.0
    for u in list(adj)[:400]:
        lp = max(lp, max(shortest(adj, u).values()))
    if lp < 0.6 * ls:
        lp = ls
    ways = sorted({way for edges in adj.values() for _, _, way in edges})
    return dict(Lp=round(lp), Ls=round(ls), methode="étendue projetée (voie non reliée à la piste dans OSM)", voies=ways[:12])


def oriented_bacinger(b):
    pts = b["pts"]
    hint = cat.BACINGER_DIRECTION.get(b["id"])
    if hint and fab.direction_of(pts) != hint:
        pts = [pts[0]] + list(reversed(pts[1:]))
    return pts


def run(args):
    out = {}
    cache = args.cache
    only = set(args.ids.split(",")) if args.ids else None
    for b in fab.bacinger_tracks(args.bacinger):
        if only and b["id"] not in only:
            continue
        key = f"bac-{b['id']}"
        if args.hors_ligne and not os.path.exists(os.path.join(cache, f"{key}.json")):
            out[b["id"]] = dict(erreur="pas de cache OSM")
            continue
        try:
            doc = osm.fetch(key, b["lat"], b["lon"], int(b["extent"] + 600), cache)
        except RuntimeError as e:
            out[b["id"]] = dict(erreur=str(e))
            continue
        m = measure(doc, b["proj"], oriented_bacinger(b))
        out[b["id"]] = m or dict(erreur="aucune voie des stands dans OSM")
        print(b["id"], out[b["id"]], flush=True)

    docs = {}
    for c in cat.OSM:
        if only and c["id"] not in only:
            continue
        key = c["group"]
        if key not in docs:
            if args.hors_ligne and not os.path.exists(os.path.join(cache, f"{key}.json")):
                docs[key] = None
            else:
                try:
                    docs[key] = osm.fetch(key, c["lat"], c["lon"], c["radius"], cache, c["roads"])
                except RuntimeError:
                    docs[key] = None
        doc = docs[key]
        if doc is None:
            out[c["id"]] = dict(erreur="pas de données OSM")
            continue
        roads_doc = None
        if c["public_roads"] and (not args.hors_ligne or os.path.exists(os.path.join(cache, f"{key}-routes.json"))):
            roads_doc = osm.fetch_roads(key, c["lat"], c["lon"], c["radius"], cache)
        res, err = osm.loop(doc, c["lat"], c["lon"], c["length"], c["roads"], c["require"], c["avoid"], c["dir"], 0.5, roads_doc, c["snap"], c["max_gap"])
        if err:
            out[c["id"]] = dict(erreur=err)
            continue
        m = measure(doc, osm.projector(c["lat"], c["lon"]), res["points"])
        out[c["id"]] = m or dict(erreur="aucune voie des stands dans OSM")
        print(c["id"], out[c["id"]], flush=True)

    with open(args.sortie, "w", encoding="utf-8") as f:
        json.dump(out, f, ensure_ascii=False, indent=1)
    ok = sum(1 for v in out.values() if "Lp" in v)
    print(f"{ok}/{len(out)} voies des stands mesurées → {args.sortie}")
    return out


# ---------------------------------------------------------------- estimation

# Calage par moindres carrés, refait à chaque passage sur les valeurs publiées de pitlane.json (option --pitlane),
# par défaut sur la seule catégorie F1 : même définition partout (drive-through net, mesuré sur les chronos officiels),
# le plus grand échantillon, écart type ~2,4 s. Les valeurs WEC des outils LMU sont des temps de traversée et celles des
# autres séries mélangent les définitions (écart type ~7 s si on calait sur tout).
#   perte (s) ≈ A × Lp / v_limite − B × Ls + C
# Lp : voie des stands entre ses jonctions avec la piste (m), Ls : piste contournée (m), v_limite en m/s.
# A < 1 : une partie de la voie (bretelles d'entrée et de sortie) se roule au-dessus de la limite ;
# B ≈ 1 / vitesse moyenne en piste le long des stands ; C : freinage et réaccélération.
# Calage du 14/11/2026 (28 circuits F1 mesurés dans OSM) : A = 0,561, B = 0,0213 s/m, C = 14,5 s, écart type 2,4 s.


def published_rows(pitlane, mesures, categories=("F1",)):
    rows = []
    for c in pitlane["circuits"]:
        m = mesures.get(c["id"], {})
        if "Lp" not in m:
            continue
        for v in [c] + c.get("variants", []):
            method = v.get("method") or ""
            if v.get("pitLaneLossSeconds") is None or method.startswith("estim") or method == "recalculé" or not v.get("speedLimitKph"):
                continue
            if categories and v.get("category") not in categories:
                continue
            rows.append((m["Lp"] * 3.6 / v["speedLimitKph"], m["Ls"], v["pitLaneLossSeconds"], c["id"], v.get("category")))
    return rows


def calibrate(rows):
    """(A, B, C, écart type) : moindres carrés de y = A·x1 − B·x2 + C (équations normales, 3 inconnues)."""
    X = [(x1, -x2, 1.0) for x1, x2, *_ in rows]  # colonnes : A, B (signe déjà dans la colonne), C
    y = [r[2] for r in rows]
    M = [[sum(a[i] * a[j] for a in X) for j in range(3)] for i in range(3)]
    v = [sum(a[i] * yy for a, yy in zip(X, y)) for i in range(3)]
    for i in range(3):  # Gauss-Jordan
        p = max(range(i, 3), key=lambda r: abs(M[r][i]))
        M[i], M[p], v[i], v[p] = M[p], M[i], v[p], v[i]
        for r in range(3):
            if r != i:
                f = M[r][i] / M[i][i]
                M[r] = [M[r][k] - f * M[i][k] for k in range(3)]
                v[r] -= f * v[i]
    a, b, c = (v[i] / M[i][i] for i in range(3))
    res = [yy - (a * x1 - b * x2 + c) for (x1, x2, *_), yy in zip(rows, y)]
    sd = math.sqrt(sum(r * r for r in res) / max(1, len(res) - 3))
    return a, b, c, sd


def estimate(lz, ls, v_kph, coef):
    """Perte (s) pour une zone au limiteur lz (m), une piste contournée ls (m) et une vitesse limite (km/h)."""
    _, b, c = coef
    return lz * 3.6 / v_kph - b * ls + c


def fill(pitlane_path, mesures, coef, sd, default_kph, label):
    """Complète pitlane.json : les circuits sans valeur publiée reçoivent l'estimation géométrique (méthode « estimé »).
    Zone au limiteur : longueur publiée si une série la donne, sinon A × voie OSM ; piste contournée : OSM, sinon zone / A."""
    a = coef[0]
    pitlane = json.load(open(pitlane_path, encoding="utf-8"))
    done = 0
    for c in pitlane["circuits"]:
        m = mesures.get(c["id"], {})
        if "Lp" in m:
            c["osmPitLength"], c["osmBypassedLength"] = m["Lp"], m["Ls"]
        method = c.get("method") or ""
        estimated = method.startswith("estim")
        if c.get("pitLaneLossSeconds") is not None and not estimated:
            continue
        before = (c.get("notes") or "").split(" | Recherche : ")[-1] if estimated else (c.get("notes") or "")
        if estimated and " | Recherche : " not in (c.get("notes") or ""):
            before = ""
        published_length = c.get("publishedPitLaneLength") if estimated else c.get("pitLaneLength")
        if "Lp" not in m and not published_length:
            c["pitLaneLossSeconds"] = None
            c["reason"] = c.get("reason") or ("rien de publié, ni longueur de voie, ni voie des stands exploitable dans OpenStreetMap "
                                              f"({m.get('erreur', 'non mesurée')})")
            continue
        assumed = not c.get("speedLimitKph") or c.get("speedAssumed", False)
        v = c.get("speedLimitKph") or default_kph
        lz = published_length or a * m["Lp"]
        # Piste contournée cohérente avec une longueur publiée (OSM compte parfois des bretelles d'accès dans la voie).
        ls = min(m["Ls"], lz / a) if "Ls" in m and published_length else m["Ls"] if "Ls" in m else lz / a
        loss = estimate(lz, ls, v, coef)
        conf = 0.4 if published_length and "Lp" in m else 0.35 if published_length or m.get("methode") == "jonctions OSM" else 0.3
        osm_urls = [f"https://www.openstreetmap.org/way/{w}" for w in m.get("voies", [])]
        kept = [u for u in c.get("sources", []) if "openstreetmap.org/way/" not in u]
        if published_length:
            c["publishedPitLaneLength"] = published_length
            geo = f"zone au limiteur publiée de {published_length:.0f} m, ~{ls:.0f} m de piste contournée" + (" (OpenStreetMap)" if "Ls" in m and ls == m["Ls"] else "")
        else:
            geo = (f"{m['Lp']} m de voie entre l'entrée et la sortie dans OpenStreetMap ({m['methode']}), soit ~{lz:.0f} m au limiteur, "
                   f"{m['Ls']} m de piste contournée")
        c.update(pitLaneLossSeconds=round(loss, 1), pitLaneLength=round(lz), speedLimitKph=v, confidence=conf, method="estimé",
                 sources=kept + osm_urls, category=c.get("category") or "générique", speedAssumed=assumed)
        c["notes"] = (f"Estimé : {geo}, à {v:.0f} km/h{' (vitesse supposée)' if assumed else ''} ; formule calée sur {label} "
                      f"(écart type {sd:.1f} s)." + (f" | Recherche : {before}" if before else ""))
        c.pop("reason", None)
        done += 1
    with open(pitlane_path, "w", encoding="utf-8") as f:
        json.dump(pitlane, f, ensure_ascii=False, indent=1)
        f.write("\n")
    print(f"{done} circuits estimés → {pitlane_path}")


if __name__ == "__main__":
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--bacinger", required=True, help="f1-circuits.geojson (bacinger/f1-circuits)")
    ap.add_argument("--cache", default=os.path.join(HERE, ".cache-osm"), help="dossier du cache Overpass")
    ap.add_argument("--sortie", default="stands-osm.json", help="mesures de chaque voie des stands")
    ap.add_argument("--ids", help="seulement ces circuits (identifiants séparés par des virgules)")
    ap.add_argument("--hors-ligne", action="store_true", help="n'utiliser que le cache")
    ap.add_argument("--pitlane", help="pitlane.json à compléter : calage sur les valeurs publiées, puis estimation des autres")
    ap.add_argument("--calage", default="F1", help="catégories servant au calage (séparées par des virgules ; vide = toutes)")
    ap.add_argument("--vitesse", type=float, default=60, help="vitesse limite supposée quand rien n'est publié (km/h)")
    args = ap.parse_args()
    mesures = run(args)
    if args.pitlane:
        cats = tuple(args.calage.split(",")) if args.calage else ()
        rows = published_rows(json.load(open(args.pitlane, encoding="utf-8")), mesures, cats)
        a, b, c, sd = calibrate(rows)
        print(f"Calage sur {len(rows)} valeurs publiées ({args.calage or 'toutes catégories'}) : perte = {a:.3f} × Lp / v − {b:.5f} × Ls + {c:.2f} (écart type {sd:.1f} s)")
        for x1, x2, y, cid, cat_ in sorted(rows, key=lambda r: abs(r[2] - (a * r[0] - b * r[1] + c)), reverse=True)[:12]:
            print(f"   {cid:26s} {cat_ or '':6s} publié {y:5.1f} s, formule {a * x1 - b * x2 + c:5.1f} s")
        fill(args.pitlane, mesures, (a, b, c), sd, args.vitesse, f"{len(rows)} pertes publiées ({args.calage or 'toutes catégories'})")
