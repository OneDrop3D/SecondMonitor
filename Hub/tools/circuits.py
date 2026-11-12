#!/usr/bin/env python3
"""
Fabrique la bibliothèque de tracés de SecondMonitor (Hub/src/SecondMonitor.Hub.Core/Track/Library/circuits.json)
à partir de sources libres :
  - bacinger/f1-circuits (licence MIT) : f1-circuits.geojson ;
  - OpenStreetMap (ODbL) : tracés complétés par osm.py (fichier osm-circuits.json), s'il existe.
Chaque tracé : départ sur la ligne d'arrivée, sens de la course, 400 points à pas réguliers, en mètres
(x vers l'est, z vers le sud : le nord est en haut à l'écran).

Usage : tools/circuits.py <f1-circuits.geojson> [osm-circuits.json]
"""
import json, math, os, sys

POINTS = 400

# Noms donnés par les jeux, en plus du nom officiel et de la ville (comparaison sans accents ni casse).
ALIASES = {
    "au-1953": ["melbourne grand prix circuit", "albert park"],
    "bh-2002": ["bahrain", "sakhir"],
    "es-1991": ["barcelona", "catalunya", "montmelo"],
    "ca-1978": ["gilles villeneuve", "montreal"],
    "fr-1969": ["paul ricard", "le castellet"],
    "at-1969": ["red bull ring", "spielberg", "osterreichring"],
    "gb-1948": ["silverstone"],
    "be-1925": ["spa", "spa-francorchamps", "francorchamps"],
    "it-1922": ["monza"],
    "sg-2008": ["marina bay", "singapore"],
    "jp-1962": ["suzuka"],
    "us-2012": ["circuit of the americas", "cota", "austin", "texas"],
    "mx-1962": ["hermanos rodriguez", "mexico"],
    "br-1940": ["interlagos", "jose carlos pace", "sao paulo"],
    "ae-2009": ["yas marina", "abu dhabi"],
    "it-1953": ["imola", "enzo e dino ferrari"],
    "de-1927": ["nurburgring gp", "nurburgring"],
    "pt-2008": ["algarve", "portimao"],
    "nl-1948": ["zandvoort"],
    "sa-2021": ["jeddah"],
    "us-2022": ["miami"],
    "qa-2004": ["losail", "lusail", "qatar"],
    "az-2016": ["baku"],
    "us-2023": ["las vegas"],
    "us-1909": ["indianapolis"],
    "za-1961": ["kyalami"],
    "us-1956": ["watkins glen"],
    "hu-1986": ["hungaroring"],
    "de-1932": ["hockenheim"],
    "cn-2004": ["shanghai"],
    "mc-1929": ["monaco", "monte carlo"],
    "my-1999": ["sepang"],
    "tr-2005": ["istanbul"],
    "ru-2014": ["sochi"],
    "it-1914": ["mugello"],
    "fr-1960": ["magny-cours", "nevers"],
    "pt-1972": ["estoril"],
}


def project(coords):
    lat0 = sum(c[1] for c in coords) / len(coords)
    lon0 = sum(c[0] for c in coords) / len(coords)
    k = math.cos(math.radians(lat0))
    return [((lon - lon0) * 111320 * k, -(lat - lat0) * 110540) for lon, lat in coords]


def resample(pts, n=POINTS):
    if pts[0] != pts[-1]:
        pts = pts + [pts[0]]
    seg = [math.dist(pts[i], pts[i + 1]) for i in range(len(pts) - 1)]
    total = sum(seg)
    out, i, acc = [], 0, 0.0
    for j in range(n):
        target = total * j / n
        while i < len(seg) - 1 and acc + seg[i] < target:
            acc += seg[i]
            i += 1
        t = 0 if seg[i] == 0 else (target - acc) / seg[i]
        a, b = pts[i], pts[i + 1]
        out.append([round(a[0] + (b[0] - a[0]) * t, 1), round(a[1] + (b[1] - a[1]) * t, 1)])
    return out, total


def main():
    src = json.load(open(sys.argv[1], encoding="utf-8"))
    circuits = []
    for f in src["features"]:
        p = f["properties"]
        pts, length = resample(project(f["geometry"]["coordinates"]))
        circuits.append({
            "id": p["id"], "name": p["Name"], "location": p["Location"],
            "aliases": ALIASES.get(p["id"], []), "length": round(p.get("length") or length),
            "source": "bacinger/f1-circuits (MIT)", "points": pts,
        })
    if len(sys.argv) > 2 and os.path.exists(sys.argv[2]):
        for c in json.load(open(sys.argv[2], encoding="utf-8")):
            pts, length = resample(project(c["coords"]))
            circuits.append({
                "id": c["id"], "name": c["name"], "location": c.get("location", ""), "aliases": c.get("aliases", []),
                "length": round(c.get("length") or length), "source": "OpenStreetMap (ODbL)", "points": pts,
            })
    out = os.path.join(os.path.dirname(__file__), "..", "src", "SecondMonitor.Hub.Core", "Track", "Library", "circuits.json")
    json.dump(circuits, open(out, "w", encoding="utf-8"), ensure_ascii=False, separators=(",", ":"))
    print(f"{len(circuits)} circuits → {os.path.normpath(out)} ({os.path.getsize(out) // 1024} Ko)")


if __name__ == "__main__":
    main()
