#!/usr/bin/env python3
"""
Lecture des circuits dans OpenStreetMap (données ODbL, © contributeurs OpenStreetMap) pour la bibliothèque de tracés.

- fetch() : une requête Overpass par circuit (mise en cache sur disque, pause entre deux requêtes, reprises espacées) ;
- loop()  : reconstitue la boucle de course à partir des voies highway=raceway (graphe des tronçons, recherche du
  cycle dont la longueur est la plus proche de la longueur officielle), en déduit le sens de course et la ligne de départ,
  avec un indice de confiance.

Aucune donnée personnelle n'est envoyée : seulement la requête Overpass et un User-Agent générique.
"""
import json, math, os, re, time, urllib.parse, urllib.request

ENDPOINTS = [
    "https://overpass.openstreetmap.fr/api/interpreter",
    "https://overpass-api.de/api/interpreter",
    "https://maps.mail.ru/osm/tools/overpass/api/interpreter",
    "https://overpass.private.coffee/api/interpreter",
]
USER_AGENT = "SecondMonitor-circuits-import/1.0"
PAUSE = 4.0  # secondes entre deux requêtes : pas de rafale
_last_request = 0.0

START_WORDS = r"start|finish|ziel|d[ée]part|arriv[ée]e|partenza|traguardo|meta|largada|chegada|salida|llegada|timing line|control line"
PIT_WORDS = re.compile(r"\bpit|boxen|box lane|stands|pitlane|pit lane|corsia box|voie des stands|boxengasse|pits\b", re.I)


def _query(lat, lon, radius, roads=None):
    roads_q = ""
    if roads:
        # Circuits sur routes ouvertes (Le Mans, Bathurst…) : on ajoute les routes nommées du tracé.
        roads_q = f'way(around:{radius},{lat},{lon})[highway][name~"{roads}",i];'
    return f"""[out:json][timeout:120];
(
  way(around:{radius},{lat},{lon})[highway=raceway];
  way(around:{radius},{lat},{lon})[service=pit_lane];
  way(around:{radius},{lat},{lon})[raceway=pit_lane];
  {roads_q}
  node(around:{radius},{lat},{lon})[raceway];
);
out body;
>;
out body qt;
"""


ROAD_CLASSES = "^(trunk|primary|secondary|tertiary|unclassified)(_link)?$"


def fetch_roads(key, lat, lon, radius, cache_dir):
    """Routes ouvertes autour d'un circuit tracé en partie sur la voie publique (Le Mans…) : cache <key>-routes.json."""
    q = f"""[out:json][timeout:120];
way(around:{radius},{lat},{lon})[highway~"{ROAD_CLASSES}"];
out body;
>;
out skel qt;
"""
    return fetch(f"{key}-routes", lat, lon, radius, cache_dir, query=q)


def fetch(key, lat, lon, radius, cache_dir, roads=None, query=None):
    """Données OSM autour du circuit (cache : <cache_dir>/<key>.json)."""
    global _last_request
    os.makedirs(cache_dir, exist_ok=True)
    path = os.path.join(cache_dir, f"{key}.json")
    if os.path.exists(path):
        with open(path, encoding="utf-8") as f:
            return json.load(f)
    data = urllib.parse.urlencode({"data": query or _query(lat, lon, radius, roads)}).encode()
    delay = 10
    for attempt in range(int(os.environ.get("OVERPASS_ESSAIS", "6"))):
        url = ENDPOINTS[attempt % len(ENDPOINTS)]
        wait = PAUSE - (time.time() - _last_request)
        if wait > 0:
            time.sleep(wait)
        _last_request = time.time()
        try:
            req = urllib.request.Request(url, data=data, headers={"User-Agent": USER_AGENT})
            with urllib.request.urlopen(req, timeout=180) as r:
                raw = r.read()
            doc = json.loads(raw)
            if "elements" not in doc:
                raise ValueError("réponse sans éléments")
            if doc.get("remark") and "error" in doc["remark"].lower():
                raise ValueError(doc["remark"])
            with open(path, "w", encoding="utf-8") as f:
                json.dump(doc, f)
            return doc
        except Exception as e:  # serveur saturé : on attend de plus en plus longtemps
            print(f"    Overpass ({url.split('/')[2]}) : {str(e)[:120]} — nouvel essai dans {delay} s")
            time.sleep(delay)
            delay = min(delay * 2, 120)
    raise RuntimeError(f"Overpass indisponible pour {key}")


# ---------------------------------------------------------------- géométrie

def projector(lat0, lon0):
    k = math.cos(math.radians(lat0))
    return lambda lat, lon: ((lon - lon0) * 111320.0 * k, -(lat - lat0) * 110540.0)  # x est, z sud (mètres)


def polyline_length(pts):
    return sum(math.dist(pts[i], pts[i + 1]) for i in range(len(pts) - 1))


def signed_area(pts):
    """Aire signée dans le repère (x est, z sud) : > 0 = sens horaire vu du ciel (nord en haut)."""
    a = 0.0
    for i in range(len(pts)):
        x1, z1 = pts[i]
        x2, z2 = pts[(i + 1) % len(pts)]
        a += x1 * z2 - x2 * z1
    return a / 2


def project_on_loop(loop, cum, p):
    """(abscisse curviligne, distance) du point p projeté sur la boucle fermée."""
    best = (0.0, float("inf"))
    n = len(loop)
    for i in range(n):
        a, b = loop[i], loop[(i + 1) % n]
        dx, dz = b[0] - a[0], b[1] - a[1]
        L2 = dx * dx + dz * dz
        t = 0 if L2 == 0 else max(0.0, min(1.0, ((p[0] - a[0]) * dx + (p[1] - a[1]) * dz) / L2))
        q = (a[0] + dx * t, a[1] + dz * t)
        d = math.dist(p, q)
        if d < best[1]:
            best = (cum[i] + t * math.sqrt(L2), d)
    return best


def cumulative(loop):
    cum = [0.0]
    for i in range(len(loop)):
        cum.append(cum[-1] + math.dist(loop[i], loop[(i + 1) % len(loop)]))
    return cum


def rotate_to(loop, s):
    """Boucle recommençant à l'abscisse s (point inséré)."""
    cum = cumulative(loop)
    total = cum[-1]
    s %= total
    n = len(loop)
    for i in range(n):
        if cum[i] <= s <= cum[i + 1]:
            a, b = loop[i], loop[(i + 1) % n]
            seg = cum[i + 1] - cum[i]
            t = 0 if seg == 0 else (s - cum[i]) / seg
            p = (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t)
            return [p] + loop[i + 1:] + loop[:i + 1]
    return loop


# ---------------------------------------------------------------- tri des voies

EXCLUDED_SPORTS = ("karting", "rallycross", "motocross", "off_road", "cycling", "horse", "athletics", "running", "bmx", "speedway", "dirt", "drift", "skating", "dog")
EXCLUDED_SURFACES = ("unpaved", "dirt", "gravel", "grass", "sand", "ground", "earth", "mud", "compacted", "fine_gravel")
EXCLUDED_NAMES = re.compile(r"kart|rally|offroad|off-road|4x4|drift|skid|motocross|supermoto|parking|dragstrip|drag strip|run.?off|escape road|slip road|test track|handling|paddock road|access road|service road", re.I)


def is_pit(tags):
    return any("pit" in str(tags.get(k, "")).lower() for k in ("service", "raceway", "pit_lane", "highway")) \
        or tags.get("pit_lane") in ("yes",) or bool(PIT_WORDS.search(tags.get("name", "") + " " + tags.get("description", "")))


def is_track(tags, roads_re=None):
    if tags.get("highway") != "raceway":
        return bool(roads_re and roads_re.search(tags.get("name", "")))
    if is_pit(tags) or tags.get("area") == "yes":
        return False
    if any(tags.get(k) in ("yes", "true") for k in ("disused", "abandoned")) or "disused" in tags.get("name", "").lower():
        return False
    sport = tags.get("sport", "")
    if sport and "motor" not in sport and any(s in sport for s in EXCLUDED_SPORTS):
        return False
    if tags.get("surface") in EXCLUDED_SURFACES:
        return False
    if tags.get("raceway") in ("karting", "drag_strip", "paddock", "motocross"):
        return False
    if EXCLUDED_NAMES.search(tags.get("name", "")):
        return False
    return True


# ---------------------------------------------------------------- graphe et boucle

class Edge:
    __slots__ = ("id", "u", "v", "pts", "length", "oneway", "name")

    def __init__(self, id_, u, v, pts, oneway, name):
        self.id, self.u, self.v, self.pts, self.oneway, self.name = id_, u, v, pts, oneway, name
        self.length = polyline_length(pts)


def build_graph(doc, proj, roads=None, snap=12.0):
    nodes = {e["id"]: e for e in doc["elements"] if e["type"] == "node"}
    roads_re = re.compile(roads, re.I) if roads else None
    ways = [e for e in doc["elements"] if e["type"] == "way" and is_track(e.get("tags", {}), roads_re)]
    ways = [w for w in ways if all(n in nodes for n in w["nodes"])]
    use = {}
    for w in ways:
        for n in set(w["nodes"]):
            use[n] = use.get(n, 0) + 1
        use[w["nodes"][0]] = use.get(w["nodes"][0], 0) + 1
        use[w["nodes"][-1]] = use.get(w["nodes"][-1], 0) + 1
    pos = {n: proj(nodes[n]["lat"], nodes[n]["lon"]) for w in ways for n in w["nodes"]}
    edges = []
    for w in ways:
        t = w.get("tags", {})
        ow = t.get("oneway", "")
        oneway = 1 if ow in ("yes", "true", "1") else -1 if ow == "-1" else 0
        start = 0
        ids = w["nodes"]
        for i in range(1, len(ids)):
            if use.get(ids[i], 0) >= 2 or i == len(ids) - 1:
                seg = ids[start:i + 1]
                if len(seg) >= 2 and seg[0] != seg[-1] or len(seg) > 3:
                    edges.append(Edge(len(edges), seg[0], seg[-1], [pos[n] for n in seg], oneway, t.get("name", "")))
                start = i
    # Extrémités libres proches d'un autre nœud : jonction virtuelle (OSM pas toujours connecté).
    deg = {}
    for e in edges:
        deg[e.u] = deg.get(e.u, 0) + 1
        deg[e.v] = deg.get(e.v, 0) + 1
    ends = [n for n, d in deg.items() if d == 1]
    junctions = list(deg)
    for a in ends:
        best, bd = None, snap
        for b in junctions:
            if b != a:
                d = math.dist(pos[a], pos[b])
                if d < bd:
                    best, bd = b, d
        if best is not None:
            edges.append(Edge(len(edges), a, best, [pos[a], pos[best]], 0, "(raccord)"))
    return edges, pos


def _component_finder(edges):
    parent = {}

    def find(x):
        parent.setdefault(x, x)
        while parent[x] != x:
            parent[x] = parent[parent[x]]
            x = parent[x]
        return x

    for e in edges:
        parent[find(e.u)] = find(e.v)
    return find, parent


def _pair(edges, candidates, make, allow_closing=False):
    """Appariement glouton des bouts libres : d'abord entre morceaux différents, puis (fermeture) dans un même morceau."""
    find, parent = _component_finder(edges)
    used = set()
    count = 0
    for closing in (False, True):
        for c in sorted(candidates, key=lambda c: c[0]):
            d, a, b = c[0], c[1], c[2]
            to_node = len(c) > 3 and c[3] == "nœud"
            if a in used or (b in used and not to_node):
                continue
            bb = c[4].u if to_node else b
            if to_node and c[5] >= len(c[4].pts) - 1:
                continue  # tronçon déjà coupé ailleurs
            same = find(a) == find(bb)
            if same != closing:
                continue
            if closing and not allow_closing and len([n for n in _free_ends(edges) if n not in used]) > 2:
                continue
            edges.append(make(c))
            parent[find(a)] = find(bb)
            used |= {a} if to_node else {a, b}
            count += 1
    return count


def _free_ends(edges):
    deg = {}
    for e in edges:
        deg[e.u] = deg.get(e.u, 0) + 1
        deg[e.v] = deg.get(e.v, 0) + 1
    return [n for n, k in deg.items() if k == 1]


def join_ends(edges, pos, dist):
    """Relie deux à deux les bouts de piste libres les plus proches (trous de cartographie, à moins de dist m)."""
    deg = {}
    for e in edges:
        deg[e.u] = deg.get(e.u, 0) + 1
        deg[e.v] = deg.get(e.v, 0) + 1
    ends = [n for n, k in deg.items() if k == 1]
    pairs = [(math.dist(pos[a], pos[b]), a, b) for i, a in enumerate(ends) for b in ends[i + 1:] if math.dist(pos[a], pos[b]) <= dist]
    # Un bout libre peut aussi rejoindre le milieu d'un autre morceau (jonction non cartographiée) : on coupe ce tronçon.
    find, _ = _component_finder(edges)
    for a in ends:
        best = None
        for e in edges:
            if a in (e.u, e.v):
                continue
            for i, p in enumerate(e.pts[1:-1], 1):
                d = math.dist(pos[a], p)
                if d <= dist and (best is None or d < best[0]):
                    best = (d, e, i)
        if best:
            pairs.append((best[0], a, ("coupe", best[1].id, best[2]), "nœud", best[1], best[2]))

    def make(c):
        if len(c) > 3:
            e, i = c[4], c[5]
            node = c[2]
            pos[node] = e.pts[i]
            tail = Edge(len(edges), node, e.v, e.pts[i:], e.oneway, e.name)
            e.v, e.pts = node, e.pts[:i + 1]
            e.length = polyline_length(e.pts)
            edges.append(tail)
        return Edge(len(edges), c[1], c[2], [pos[c[1]], pos[c[2]]], 0, "(raccord)")

    return _pair(edges, pairs, make, allow_closing=True)


def bridge_with_roads(edges, pos, roads_doc, proj, max_gap=1500):
    """Relie les bouts de piste séparés par la voie publique (circuits mixtes) par le plus court chemin routier."""
    import heapq
    rn = {e["id"]: proj(e["lat"], e["lon"]) for e in roads_doc["elements"] if e["type"] == "node"}
    adj = {}
    for w in roads_doc["elements"]:
        if w["type"] != "way":
            continue
        ids = [n for n in w["nodes"] if n in rn]
        for a, b in zip(ids, ids[1:]):
            # Nœuds intermédiaires tous les ~25 m : un bout de piste se raccroche au milieu d'une longue ligne droite.
            k = max(1, int(math.dist(rn[a], rn[b]) / 25))
            chain = [a]
            for j in range(1, k):
                key = ("i", w["id"], a, j)
                rn[key] = (rn[a][0] + (rn[b][0] - rn[a][0]) * j / k, rn[a][1] + (rn[b][1] - rn[a][1]) * j / k)
                chain.append(key)
            chain.append(b)
            for u, v in zip(chain, chain[1:]):
                d = math.dist(rn[u], rn[v])
                adj.setdefault(u, []).append((v, d))
                adj.setdefault(v, []).append((u, d))
    if not adj:
        return 0
    deg = {}
    for e in edges:
        deg[e.u] = deg.get(e.u, 0) + 1
        deg[e.v] = deg.get(e.v, 0) + 1
    ends = [n for n, k in deg.items() if k == 1]
    keys = list(adj)

    def nearest(p):
        best = min(keys, key=lambda k: (rn[k][0] - p[0]) ** 2 + (rn[k][1] - p[1]) ** 2)
        return best if math.dist(rn[best], p) < 40 else None

    anchor = {n: nearest(pos[n]) for n in ends}
    by_road = {}
    for n, r in anchor.items():
        if r is not None:
            by_road.setdefault(r, []).append(n)
    # Toutes les liaisons routières possibles, puis appariement glouton des bouts libres (un pont par bout).
    candidates = []
    for a in ends:
        src = anchor[a]
        if src is None:
            continue
        dist, prev, heap = {src: 0.0}, {}, [(0.0, src)]
        while heap:
            d, u = heapq.heappop(heap)
            if d > dist.get(u, 1e18) or d > max_gap:
                continue
            for b in by_road.get(u, []):
                if b != a and math.dist(pos[a], pos[b]) > 20:
                    path = [u]
                    while path[-1] != src:
                        path.append(prev[path[-1]])
                    candidates.append((d, a, b, [rn[k] for k in reversed(path)]))
            for v, w in adj[u]:
                nd = d + w
                if nd < dist.get(v, 1e18):
                    dist[v], prev[v] = nd, u
                    heapq.heappush(heap, (nd, v))
    return _pair(edges, candidates, lambda c: Edge(len(edges), c[1], c[2], [pos[c[1]]] + c[3] + [pos[c[2]]], 0, "(route ouverte)"))


def find_loops(edges, target, tolerance=0.12, max_steps=3_000_000):
    """Cycles simples dont la longueur est à ± tolerance de la cible."""
    adj = {}
    for e in edges:
        adj.setdefault(e.u, []).append((e, e.v))
        adj.setdefault(e.v, []).append((e, e.u))
    order = {n: i for i, n in enumerate(sorted(adj, key=str))}
    found = {}
    steps = 0
    hi = target * (1 + tolerance)
    for s in sorted(adj, key=lambda n: order[n]):
        stack = [(s, 0.0, [], {s})]
        while stack:
            steps += 1
            if steps > max_steps:
                return list(found.values()), False
            node, length, path, seen = stack.pop()
            for e, nxt in adj[node]:
                if path and e is path[-1][0]:
                    continue
                nl = length + e.length
                if nl > hi:
                    continue
                if nxt == s and (path or e.u == e.v):
                    if nl >= target * (1 - tolerance):
                        cyc = path + [(e, node)]
                        key = frozenset(x[0].id for x in cyc)
                        if key not in found:
                            found[key] = (nl, cyc)
                    continue
                if nxt in seen or order[nxt] < order[s]:
                    continue
                stack.append((nxt, nl, path + [(e, node)], seen | {nxt}))
    return list(found.values()), True


def cycle_polyline(cyc):
    pts = []
    for e, frm in cyc:
        seg = e.pts if frm == e.u else list(reversed(e.pts))
        pts.extend(seg if not pts else seg[1:])
    return pts[:-1] if math.dist(pts[0], pts[-1]) < 0.5 else pts


def oneway_score(cyc):
    """(longueur dans le sens autorisé, longueur à contresens, longueur balisée) pour le sens de parcours du cycle."""
    agree = contra = 0.0
    for e, frm in cyc:
        if e.oneway == 0:
            continue
        forward = (frm == e.u) == (e.oneway == 1)
        if forward:
            agree += e.length
        else:
            contra += e.length
    return agree, contra


def loop(doc, lat0, lon0, target, roads=None, require=None, avoid=None, direction_hint=None, pit_phase=0.5, roads_doc=None, snap=12.0, max_gap=1500):
    """Tracé de course depuis OSM : dict(points, length, confidence, notes…) ou None."""
    proj = projector(lat0, lon0)
    edges, pos = build_graph(doc, proj, roads)
    if not edges:
        return None, "aucune voie highway=raceway"
    if roads_doc:
        bridge_with_roads(edges, pos, roads_doc, proj, max_gap)
    if snap > 12:
        join_ends(edges, pos, snap)
    cycles, complete = find_loops(edges, target)
    req = [re.compile(r, re.I) for r in (require or [])]
    av = [re.compile(r, re.I) for r in (avoid or [])]
    scored = []
    for length, cyc in cycles:
        names = " | ".join(e.name for e, _ in cyc)
        if any(not r.search(names) for r in req) or any(r.search(names) for r in av):
            continue
        agree, contra = oneway_score(cyc)
        # Le cycle n'est valable que dans un sens : celui qui ne contredit aucun sens unique.
        if agree > 0 and contra > 0:
            continue
        scored.append((abs(length - target) / target, length, cyc, agree, contra))
    if not scored:
        return None, f"aucune boucle à ±12 % de {target} m ({len(cycles)} cycles, {len(edges)} tronçons{'' if complete else ', recherche tronquée'})"
    scored.sort(key=lambda s: s[0])
    err, length, cyc, agree, contra = scored[0]
    if err > 0.06:
        return None, f"meilleure boucle {length:.0f} m pour {target} m"
    notes = []
    public = sum(e.length for e, _ in cyc if e.name == "(route ouverte)")
    if public:
        notes.append(f"{public:.0f} m sur routes ouvertes")
    chosen = {e.id: e.length for e, _ in cyc}

    def different(other):
        # Une boucle voisine qui ne diffère que par une chicane (moins de 15 % du tour) ne rend pas le tracé ambigu.
        shared = sum(e.length for e, _ in other if e.id in chosen)
        return shared < 0.85 * length

    ambiguous = [s for s in scored[1:] if s[0] - err < 0.01 and different(s[2])]
    pts = cycle_polyline(cyc)

    # --- sens de course
    oneway_len = agree + contra
    if oneway_len > 0.1 * length:
        if contra > 0:
            pts.reverse()
        dir_conf = 0.95 if oneway_len > 0.5 * length else 0.85
        notes.append(f"sens : oneway ({oneway_len / length:.0%} du tour balisé)")
        hint_check = direction_hint
    else:
        dir_conf = 0.0
        hint_check = None
        if direction_hint in ("cw", "ccw"):
            want_cw = direction_hint == "cw"
            if (signed_area(pts) > 0) != want_cw:
                pts.reverse()
            dir_conf = 0.7
            notes.append(f"sens : indication du catalogue ({direction_hint})")
        else:
            notes.append("sens : inconnu")
            dir_conf = 0.2
    if hint_check in ("cw", "ccw") and (signed_area(pts) > 0) != (hint_check == "cw"):
        notes.append(f"ATTENTION : oneway contredit l'indication {hint_check}")
        dir_conf = min(dir_conf, 0.4)

    # --- ligne de départ
    cum = cumulative(pts)
    total = cum[-1]
    pit = pit_span(doc, proj, pts, cum)
    start, start_conf = None, 0.0
    named = named_start(doc, proj, pts, cum, pit_spans(doc, proj, pts, cum))
    if named:
        start, start_conf = named[0], 0.9
        notes.append(f"départ : élément OSM « {named[1]} »")
    if start is None and pit:
        a, b = pit
        span = (b - a) % total
        start = (a + span * pit_phase) % total
        start_conf = 0.6
        notes.append(f"départ : voie des stands ({span:.0f} m le long de la piste)")
    if start is None:
        start = longest_straight(pts, cum)
        start_conf = 0.3
        notes.append("départ : milieu de la plus longue ligne droite")
    pts = rotate_to(pts, start)
    conf = round(min(dir_conf, start_conf), 2)
    if ambiguous:
        notes.append(f"{len(ambiguous)} autre(s) boucle(s) de longueur voisine")
        conf = min(conf, 0.5)
    return {"points": pts, "length": length, "confidence": conf, "notes": notes, "pit": pit, "start_conf": start_conf, "dir_conf": dir_conf}, None


RACEWAY_PRIORITY = {"finish": 0, "finish_line": 0, "start-finish": 1, "start/finish": 1, "start_finish": 1, "start;finish": 1, "start": 2}


def named_start(doc, proj, pts, cum, pits=None):
    """Ligne de chronométrage nommée dans OSM : (abscisse, libellé) ou None.
    Priorité à raceway=finish (la ligne qui compte les tours dans les jeux), puis start-finish, start, puis les noms.
    Si la voie des stands est connue, la ligne doit la longer (± 300 m) : écarte les vestiges d'anciens tracés."""
    nodes = {e["id"]: e for e in doc["elements"] if e["type"] == "node"}
    total = cum[-1]
    found = []
    for el in doc["elements"]:
        tags = el.get("tags", {})
        if any(k in tags for k in ("amenity", "shop", "building", "office", "craft", "board_type")) or tags.get("tourism") == "information":
            continue
        rw = str(tags.get("raceway", "")).lower()
        if rw in RACEWAY_PRIORITY:
            prio = RACEWAY_PRIORITY[rw]
        elif re.search(START_WORDS, tags.get("name", ""), re.I) and not is_pit(tags):
            if re.search(r"grid|grille", tags.get("name", ""), re.I):
                continue
            prio = 3
        else:
            continue
        if el["type"] == "node":
            p = proj(el["lat"], el["lon"])
        elif el["type"] == "way":
            ps = [proj(nodes[n]["lat"], nodes[n]["lon"]) for n in el.get("nodes", []) if n in nodes]
            if not ps or polyline_length(ps) > 80:
                continue  # pas une ligne en travers de la piste
            p = (sum(q[0] for q in ps) / len(ps), sum(q[1] for q in ps) / len(ps))
        else:
            continue
        s, d = project_on_loop(pts, cum, p)
        if d > 30:
            continue
        if pits and not any(((s - a) % total) <= ((b - a) % total) + 300 or ((a - s) % total) <= 300 for a, b in pits):
            continue
        found.append((prio, s, tags.get("name") or rw or "ligne"))
    if not found:
        return None
    found.sort(key=lambda f: f[0])
    return found[0][1], found[0][2]


def pit_span(doc, proj, pts, cum):
    spans = pit_spans(doc, proj, pts, cum)
    return spans[0] if spans else None


def pit_spans(doc, proj, pts, cum):
    """Abscisses (entrée, sortie) de la voie des stands le long de la piste, dans le sens de course.
    Les voies des stands (souvent en plusieurs morceaux) sont regroupées ; une ancienne voie éloignée forme un autre groupe."""
    nodes = {e["id"]: e for e in doc["elements"] if e["type"] == "node"}
    total = cum[-1]
    ss = []
    for w in doc["elements"]:
        tags = w.get("tags", {})
        if w["type"] != "way" or not is_pit(tags) or tags.get("disused") == "yes" or re.search("disused|old|ancien", tags.get("name", ""), re.I):
            continue
        ps = [proj(nodes[n]["lat"], nodes[n]["lon"]) for n in w["nodes"] if n in nodes]
        if len(ps) < 2:
            continue
        # Points intermédiaires tous les ~20 m pour que l'étendue ne dépende pas du découpage de la voie.
        dense = []
        for i in range(len(ps) - 1):
            k = max(1, int(math.dist(ps[i], ps[i + 1]) / 20))
            dense += [(ps[i][0] + (ps[i + 1][0] - ps[i][0]) * j / k, ps[i][1] + (ps[i + 1][1] - ps[i][1]) * j / k) for j in range(k)]
        ss += [s for s, d in (project_on_loop(pts, cum, p) for p in dense) if d < 70]
    if len(ss) < 3:
        return None
    ss.sort()
    # Groupes séparés par des trous de plus de 250 m (en tenant compte du passage par l'origine).
    gaps = [((ss[(i + 1) % len(ss)] - ss[i]) % total, i) for i in range(len(ss))]
    cuts = sorted(i for g, i in gaps if g > 250)
    if not cuts:
        return None
    groups = []
    for j, c in enumerate(cuts):
        nxt = cuts[(j + 1) % len(cuts)]
        a, b = ss[(c + 1) % len(ss)], ss[nxt]
        span = (b - a) % total
        if 150 <= span <= 0.4 * total:
            groups.append((span, a, b))
    return [(a, b) for _, a, b in sorted(groups, reverse=True)]


def longest_straight(pts, cum):
    n = len(pts)
    def heading(i):
        a, b = pts[i], pts[(i + 1) % n]
        return math.atan2(b[1] - a[1], b[0] - a[0])
    best, bi, bj = 0, 0, 0
    i = 0
    while i < n:
        j = i
        h0 = heading(i)
        while j + 1 < i + n and abs((heading((j + 1) % n) - h0 + math.pi) % (2 * math.pi) - math.pi) < math.radians(8):
            j += 1
        L = (cum[min(j + 1, n)] - cum[i]) if j + 1 <= n else cum[-1] - cum[i] + cum[(j + 1) % n]
        if L > best:
            best, bi, bj = L, i, j
        i = j + 1
    return (cum[bi] + best / 2) % cum[-1]
