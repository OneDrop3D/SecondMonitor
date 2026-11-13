#!/usr/bin/env python3
"""
Fabrique la bibliothèque de tracés de SecondMonitor (Hub/src/SecondMonitor.Hub.Core/Track/Library/circuits.json).

Sources libres :
  - bacinger/f1-circuits (licence MIT) : f1-circuits.geojson (40 circuits de F1) ;
  - OpenStreetMap (ODbL, © contributeurs OpenStreetMap) : voies highway=raceway, lues par circuits_osm.py ;
  - TUMFTM/racetrack-database (LGPL-3.0), facultatif : sert seulement à CONTRÔLER départs et sens (aucune donnée copiée).

Chaque tracé : 400 points à pas réguliers, en mètres entiers (x vers l'est, z vers le sud : nord en haut à l'écran),
départ sur la ligne, dans le sens de course, avec un indice de confiance (0-1) sur le départ et le sens.
Le hub recale de toute façon départ et sens dès que le jeu donne des positions (Track/TrackCalibration.cs).

Usage :
  tools/circuits.py --bacinger f1-circuits.geojson [--cache DOSSIER] [--tum racetrack-database/tracks]
                    [--png DOSSIER] [--rapport FICHIER] [--hors-ligne]
Le cache Overpass (un fichier par circuit) rend la fabrication reproductible sans réinterroger OpenStreetMap.
"""
import argparse, csv, json, math, os, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import circuits_catalogue as cat  # noqa: E402
import circuits_osm as osm  # noqa: E402
import circuits_calage as calage  # noqa: E402

POINTS = 400
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, "..", "src", "SecondMonitor.Hub.Core", "Track", "Library", "circuits.json"))
# Tracés qui se croisent réellement (pont) : l'auto-intersection est normale.
CROSSING_OK = {"jp-1962"}
# Correspondances TUMFTM → identifiant de la bibliothèque (contrôle seulement).
TUM = {"Austin": "us-2012", "Budapest": "hu-1986", "Catalunya": "es-1991", "Hockenheim": "de-1932", "Melbourne": "au-1953",
       "MexicoCity": "mx-1962", "Montreal": "ca-1978", "Monza": "it-1922", "Nuerburgring": "de-1927", "Sakhir": "bh-2002",
       "SaoPaulo": "br-1940", "Sepang": "my-1999", "Shanghai": "cn-2004", "Silverstone": "gb-1948", "Sochi": "ru-2014",
       "Spa": "be-1925", "Spielberg": "at-1969", "Suzuka": "jp-1962", "YasMarina": "ae-2009", "Zandvoort": "nl-1948",
       "BrandsHatch": "gb-brands-hatch-gp", "MoscowRaceway": "ru-moscow-raceway", "Norisring": "de-norisring",
       "Oschersleben": "de-oschersleben", "IMS": "us-indianapolis-oval"}


# ---------------------------------------------------------------- outils géométriques

def resample(pts, n=POINTS):
    pts = list(pts) + [pts[0]]
    cum = [0.0]
    for i in range(len(pts) - 1):
        cum.append(cum[-1] + math.dist(pts[i], pts[i + 1]))
    out, j = [], 0
    for k in range(n):
        t = cum[-1] * k / n
        while j < len(cum) - 2 and cum[j + 1] < t:
            j += 1
        a, b = pts[j], pts[j + 1]
        seg = cum[j + 1] - cum[j]
        u = 0 if seg == 0 else (t - cum[j]) / seg
        out.append((a[0] + (b[0] - a[0]) * u, a[1] + (b[1] - a[1]) * u))
    return out, cum[-1]


def centre(pts):
    xs = [p[0] for p in pts]
    zs = [p[1] for p in pts]
    cx, cz = (min(xs) + max(xs)) / 2, (min(zs) + max(zs)) / 2
    return [[round(x - cx), round(z - cz)] for x, z in pts]


def self_crossings(pts):
    n = len(pts)

    def cross(a, b, c, d):
        def o(p, q, r):
            return (q[0] - p[0]) * (r[1] - p[1]) - (q[1] - p[1]) * (r[0] - p[0])
        return o(a, b, c) * o(a, b, d) < 0 and o(c, d, a) * o(c, d, b) < 0

    count = 0
    for i in range(n):
        a, b = pts[i], pts[(i + 1) % n]
        for j in range(i + 2, n):
            if i == 0 and j == n - 1:
                continue
            if cross(a, b, pts[j], pts[(j + 1) % n]):
                count += 1
    return count


def direction_of(pts):
    return "cw" if osm.signed_area(pts) > 0 else "ccw"


# ---------------------------------------------------------------- sources

def bacinger_tracks(path):
    src = json.load(open(path, encoding="utf-8"))
    out = []
    for f in src["features"]:
        p = f["properties"]
        coords = f["geometry"]["coordinates"]
        lat0 = sum(c[1] for c in coords) / len(coords)
        lon0 = sum(c[0] for c in coords) / len(coords)
        proj = osm.projector(lat0, lon0)
        pts = [proj(lat, lon) for lon, lat in coords]
        if math.dist(pts[0], pts[-1]) < 1:
            pts = pts[:-1]
        out.append(dict(id=p["id"], name=p["Name"], location=p["Location"], length=p.get("length"), lat=lat0, lon=lon0,
                        pts=pts, proj=proj, extent=max(math.dist(q, (0, 0)) for q in pts)))
    return out


def osm_start_and_pit(doc, proj, pts):
    """Ligne de départ nommée dans OSM et voie des stands, projetées sur un tracé existant (même repère)."""
    cum = osm.cumulative(pts)
    pits = osm.pit_spans(doc, proj, pts, cum)
    return osm.named_start(doc, proj, pts, cum, pits), (pits[0] if pits else None), cum[-1]


def tum_direction(folder, track_id, pts):
    """(sens de TUMFTM par rapport à pts : +1 identique / -1 inverse, décalage de son départ en m), ou None
    si indisponible ou si le calage est incertain. Rien n'est copié de TUMFTM : contrôle seulement."""
    name = next((k for k, v in TUM.items() if v == track_id), None)
    if not folder or not name or not os.path.exists(os.path.join(folder, f"{name}.csv")):
        return None
    rows = [r for r in csv.reader(open(os.path.join(folder, f"{name}.csv"))) if r and not r[0].startswith("#")]
    tp, _ = resample([(float(r[0]), float(r[1])) for r in rows], 120)
    ref, _ = resample(pts, POINTS)
    best, rival = calage.best_and_rival(calage.fit(ref, [(k / 120, x, z) for k, (x, z) in enumerate(tp)], 4), POINTS)
    if not best or not rival or rival[0] < 2.5 * best[0]:
        return None
    total = osm.polyline_length(list(pts) + [pts[0]])
    return best[2], ((best[1] + POINTS / 2) % POINTS - POINTS / 2) * total / POINTS


def oneway_direction(doc, proj, pts):
    """Sens déduit des voies oneway proches du tracé : +1 = même sens que pts, -1 = inverse, 0 = inconnu."""
    edges, _ = osm.build_graph(doc, proj)
    cum = osm.cumulative(pts)
    total = cum[-1]
    vote = 0.0
    for e in edges:
        if e.oneway == 0 or e.length < 30:
            continue
        a = e.pts[0] if e.oneway == 1 else e.pts[-1]
        b = e.pts[-1] if e.oneway == 1 else e.pts[0]
        sa, da = osm.project_on_loop(pts, cum, a)
        sb, db = osm.project_on_loop(pts, cum, b)
        if da > 25 or db > 25:
            continue
        delta = (sb - sa + total / 2) % total - total / 2
        if abs(delta) < 0.3 * total:
            vote += math.copysign(e.length, delta)
    return 0 if abs(vote) < 0.05 * total else (1 if vote > 0 else -1), abs(vote) / total


# ---------------------------------------------------------------- fabrication

def build(args):
    report = []
    tracks = []
    cache = args.cache

    # Phase de la ligne de départ le long de la voie des stands, mesurée sur les circuits où OSM la nomme.
    phases = []

    # 1) bacinger/f1-circuits, contrôlés par OpenStreetMap (et TUMFTM si fourni)
    pending_pit = []
    for b in bacinger_tracks(args.bacinger):
        pts = b["pts"]
        notes = []
        doc = None
        if not args.hors_ligne or os.path.exists(os.path.join(cache, f"bac-{b['id']}.json")):
            try:
                doc = osm.fetch(f"bac-{b['id']}", b["lat"], b["lon"], int(b["extent"] + 600), cache)
            except RuntimeError as e:
                notes.append(str(e))

        # Sens : votes (+1 = sens bacinger, -1 = inverse) du catalogue, des voies oneway d'OSM et de TUMFTM.
        votes = []
        hint = cat.BACINGER_DIRECTION.get(b["id"])
        if hint:
            votes.append(("catalogue", 1 if direction_of(pts) == hint else -1, 1.0))
        if doc:
            ow, share = oneway_direction(doc, b["proj"], pts)
            if ow:
                votes.append((f"oneway OSM {share:.0%}", ow, 2.0 if share >= 0.5 else 1.0))
        tum = tum_direction(args.tum, b["id"], pts)
        if tum:
            votes.append(("TUMFTM", tum[0], 1.0))
        total_vote = sum(v * w for _, v, w in votes) + 0.5  # à égalité, bacinger l'emporte
        winner = 1 if total_vote > 0 else -1
        against = sum(w for _, v, w in votes if v != winner)
        pro = sum(w for _, v, w in votes if v == winner)
        if winner == -1:
            pts = [pts[0]] + list(reversed(pts[1:]))
            notes.append("sens bacinger INVERSÉ")
        dir_conf = (0.95 if pro >= 3 else 0.85 if pro >= 2 else 0.7) if against == 0 else (0.75 if pro >= against + 2 else 0.4)
        notes.append("sens : " + ", ".join(f"{n} {'pour' if v == winner else 'CONTRE'}" for n, v, _ in votes) if votes else "sens : bacinger seul")

        start_conf = 0.65
        if doc:
            named, pit, total = osm_start_and_pit(doc, b["proj"], pts)
            if named:
                s, label = named
                off = (s + total / 2) % total - total / 2
                notes.append(f"départ OSM « {label} » à {off:+.0f} m du départ bacinger")
                if pit and (pit[1] - pit[0]) % total > 100:
                    phases.append(((s - pit[0]) % total) / ((pit[1] - pit[0]) % total))
                pts = osm.rotate_to(pts, s)
                start_conf = 0.9
            elif pit:
                a, e = pit
                span = (e - a) % total
                inside = (0 - a) % total <= span + 150 or (a - 0) % total <= 150
                if inside:
                    start_conf = 0.75
                    notes.append("départ bacinger le long de la voie des stands")
                elif tum and tum[0] == winner and abs(tum[1]) < 300:
                    start_conf = 0.75
                    notes.append(f"départ bacinger confirmé par TUMFTM ({tum[1]:+.0f} m)")
                else:
                    pending_pit.append((len(tracks), a, span))
                    notes.append("départ bacinger loin de la voie des stands : départ pris sur la voie des stands")
                    start_conf = 0.6
        tracks.append(dict(id=b["id"], name=b["name"], location=b["location"], aliases=cat.BACINGER_ALIASES.get(b["id"], []),
                           length=round(b["length"] or osm.polyline_length(pts + [pts[0]])), source="bacinger/f1-circuits (MIT)",
                           raw=pts, conf=round(min(start_conf, dir_conf), 2), notes=notes, hint=None))

    phase = sorted(phases)[len(phases) // 2] if phases else 0.5
    report.append(f"Phase médiane du départ le long de la voie des stands : {phase:.2f} (sur {len(phases)} circuits où OSM nomme la ligne)")
    for i, a, span in pending_pit:
        tracks[i]["raw"] = osm.rotate_to(tracks[i]["raw"], a + span * phase)

    # 2) OpenStreetMap
    docs = {}
    for c in cat.OSM:
        key = c["group"]
        if key not in docs:
            if args.hors_ligne and not os.path.exists(os.path.join(cache, f"{key}.json")):
                docs[key] = None
            else:
                try:
                    docs[key] = osm.fetch(key, c["lat"], c["lon"], c["radius"], cache, c["roads"])
                except RuntimeError as e:
                    docs[key] = None
                    report.append(f"{c['id']} : {e}")
        doc = docs[key]
        if doc is None:
            report.append(f"ÉCHEC {c['id']} : pas de données OSM")
            continue
        roads_doc = None
        if c["public_roads"] and (not args.hors_ligne or os.path.exists(os.path.join(cache, f"{key}-routes.json"))):
            roads_doc = osm.fetch_roads(key, c["lat"], c["lon"], c["radius"], cache)
        res, err = osm.loop(doc, c["lat"], c["lon"], c["length"], c["roads"], c["require"], c["avoid"], c["dir"], phase, roads_doc, c["snap"], c["max_gap"])
        if err:
            report.append(f"ÉCHEC {c['id']} : {err}")
            continue
        tracks.append(dict(id=c["id"], name=c["name"], location=c["location"], aliases=c["aliases"], length=c["length"],
                           source="© contributeurs OpenStreetMap (ODbL)", raw=res["points"], conf=res["confidence"], notes=res["notes"], hint=c["dir"]))

    # 3) Rééchantillonnage, contrôles
    out = []
    for t in tracks:
        pts, computed = resample(t["raw"])
        q = centre(pts)
        problems = []
        if abs(computed - t["length"]) / t["length"] > 0.06:
            problems.append(f"longueur calculée {computed:.0f} m ≠ officielle {t['length']} m")
        crossings = self_crossings(q)
        if crossings and t["id"] not in CROSSING_OK:
            problems.append(f"{crossings} auto-intersection(s)")
        if t["hint"] and direction_of(pts) != t["hint"]:
            problems.append(f"sens {direction_of(pts)} ≠ catalogue {t['hint']}")
        if problems:
            report.append(f"REJET {t['id']} : " + " ; ".join(problems) + " | " + " ; ".join(t["notes"]))
            continue
        out.append(dict(id=t["id"], name=t["name"], location=t["location"], aliases=t["aliases"], length=t["length"],
                        source=t["source"], confidence=t["conf"], notes=" ; ".join(t["notes"]), points=q))
        report.append(f"ok {t['id']:28s} {computed:7.0f}/{t['length']:<6} conf {t['conf']:.2f}  {' ; '.join(t['notes'])}")

    # 4) Contrôle croisé avec TUMFTM (départ et sens), facultatif
    if args.tum:
        byid = {t["id"]: t for t in out}
        for name, tid in TUM.items():
            f = os.path.join(args.tum, f"{name}.csv")
            if tid not in byid or not os.path.exists(f):
                continue
            rows = [r for r in csv.reader(open(f)) if r and not r[0].startswith("#")]
            tp, _ = resample([(float(r[0]), float(r[1])) for r in rows], 200)
            samples = [(k / 200, x, z) for k, (x, z) in enumerate(tp)]
            pts = byid[tid]["points"]
            best, rival = calage.best_and_rival(calage.fit(pts, samples, 2), POINTS)
            off = (best[1] + POINTS / 2) % POINTS - POINTS / 2
            report.append(f"TUMFTM {name:13s} → {tid:22s} départ TUM à {off * byid[tid]['length'] / POINTS:+6.0f} m, sens {'identique' if best[2] == 1 else 'INVERSE'}, "
                          f"écart {best[0]:.0f} m (rival {rival[0] if rival else 0:.0f} m)")

    with open(args.sortie, "w", encoding="utf-8") as f:
        json.dump(out, f, ensure_ascii=False, separators=(",", ":"))
    report.append(f"{len(out)} circuits → {args.sortie} ({os.path.getsize(args.sortie) // 1024} Ko)")
    text = "\n".join(report)
    print(text)
    if args.rapport:
        open(args.rapport, "w", encoding="utf-8").write(text + "\n")
    if args.png:
        render(out, args.png)


def render(tracks, folder):
    """Planches PNG : tracé, départ (carré), sens (flèches), nord en haut. Nécessite matplotlib."""
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    os.makedirs(folder, exist_ok=True)
    per = 20
    for page in range(0, len(tracks), per):
        fig, axes = plt.subplots(4, 5, figsize=(20, 16))
        for ax, t in zip(axes.flat, tracks[page:page + per]):
            xs = [p[0] for p in t["points"]] + [t["points"][0][0]]
            zs = [-p[1] for p in t["points"]] + [-t["points"][0][1]]  # z sud → y nord
            ax.plot(xs, zs, color="#444", lw=1.5)
            ax.plot(xs[0], zs[0], "s", color="#e33", ms=9)
            for k in (5, 30, 60):
                i = k * len(t["points"]) // 100 if k > 5 else 6
                ax.annotate("", xy=(xs[i + 4], zs[i + 4]), xytext=(xs[i], zs[i]), arrowprops=dict(arrowstyle="->", color="#1a7", lw=2))
            ax.set_title(f"{t['id']}\n{t['name'][:34]} · conf {t['confidence']:.2f}", fontsize=8)
            ax.set_aspect("equal")
            ax.set_xticks([])
            ax.set_yticks([])
        for ax in axes.flat[len(tracks[page:page + per]):]:
            ax.axis("off")
        fig.tight_layout()
        fig.savefig(os.path.join(folder, f"circuits-{page // per + 1:02d}.png"), dpi=60)
        plt.close(fig)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--bacinger", required=True, help="f1-circuits.geojson (github.com/bacinger/f1-circuits)")
    ap.add_argument("--cache", default=os.path.join(HERE, ".cache-osm"), help="dossier du cache Overpass")
    ap.add_argument("--tum", help="dossier tracks/ de TUMFTM/racetrack-database (contrôle)")
    ap.add_argument("--sortie", default=OUT)
    ap.add_argument("--rapport")
    ap.add_argument("--png", help="dossier des planches PNG de contrôle")
    ap.add_argument("--hors-ligne", action="store_true", help="n'utiliser que le cache")
    build(ap.parse_args())


if __name__ == "__main__":
    main()
