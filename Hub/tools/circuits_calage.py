"""
Calage d'un tracé de bibliothèque sur des positions mesurées (même algorithme que Track/TrackCalibration.cs).

Entrée : le tracé (points à pas réguliers, départ à l'indice 0, sens de course) et des échantillons
(fraction du tour, x, z) mesurés dans un autre repère (le jeu, ou une autre base de tracés).
Pour chaque décalage de départ et chaque sens, on ajuste une similitude 2D (rotation, échelle, translation,
éventuellement symétrie) entre les points du tracé attendus et les positions mesurées ; le meilleur couple
(décalage, sens) est retenu s'il se détache nettement du suivant.
"""
import math


def _at(points, s):
    n = len(points)
    s %= n
    i0 = int(s)
    t = s - i0
    i = i0 % n
    a, b = points[i], points[(i + 1) % n]
    return a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t


def _residual(src, dst):
    """Résidu RMS du meilleur ajustement de similitude (avec ou sans symétrie) src → dst, et l'échelle."""
    m = len(src)
    sx = sum(p[0] for p in src) / m
    sz = sum(p[1] for p in src) / m
    dx = sum(p[0] for p in dst) / m
    dz = sum(p[1] for p in dst) / m
    best = (float("inf"), 1.0)
    for mirror in (1, -1):
        a2 = sxx = sxy = b2 = 0.0
        for (ax, az), (bx, bz) in zip(src, dst):
            ax -= sx
            az = (az - sz) * mirror
            bx -= dx
            bz -= dz
            a2 += ax * ax + az * az
            b2 += bx * bx + bz * bz
            sxx += ax * bx + az * bz
            sxy += ax * bz - az * bx
        if a2 <= 0:
            continue
        k = math.hypot(sxx, sxy)
        scale = k / a2
        res = max(0.0, b2 - k * k / a2)
        rms = math.sqrt(res / m)
        if rms < best[0]:
            best = (rms, scale)
    return best


def fit(points, samples, step=1):
    """Liste triée [(rms, décalage en points, sens, échelle)] pour tous les décalages et les deux sens."""
    n = len(points)
    dst = [(x, z) for _, x, z in samples]
    out = []
    for direction in (1, -1):
        for off in range(0, n, step):
            src = [_at(points, off + direction * f * n) for f, _, _ in samples]
            rms, scale = _residual(src, dst)
            if 0.8 <= scale <= 1.25:
                out.append((rms, off, direction, scale))
    out.sort()
    return out


def best_and_rival(results, n, min_gap=0.05):
    """Meilleur ajustement et meilleur rival « différent » (autre sens, ou départ décalé de plus de min_gap du tour)."""
    if not results:
        return None, None
    best = results[0]
    for r in results[1:]:
        d = abs((r[1] - best[1] + n / 2) % n - n / 2) / n
        if r[2] != best[2] or d > min_gap:
            return best, r
    return best, None
