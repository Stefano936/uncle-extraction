"""Original s19 surfaces. Run with Python 3, Pillow and NumPy from any directory.

The PNG alpha is URP smoothness. Height fields in metres produce tangent-space
normal maps independently of the paint and wear colours. No source images.
"""
from pathlib import Path

import numpy as np
from PIL import Image


OUT = (Path(__file__).resolve().parents[2] /
       "UnityProject/Assets/Game/Art/Materials/Textures/S19")
TAU = 2 * np.pi


def uv(width, height):
    u = (np.arange(width, dtype=np.float32) + 0.5) / width
    v = (np.arange(height, dtype=np.float32) + 0.5) / height
    return np.meshgrid(u, v)


def noise(u, v, nx, ny, seed):
    """Smooth periodic value noise, also used with anisotropic grids."""
    grid = np.random.default_rng(seed).random((ny, nx), dtype=np.float32)
    x, y = u * nx, v * ny
    ix, iy = np.floor(x).astype(np.int32), np.floor(y).astype(np.int32)
    a, b = x - ix, y - iy
    a, b = a * a * (3 - 2 * a), b * b * (3 - 2 * b)
    lo = grid[iy % ny, ix % nx] * (1 - a) + grid[iy % ny, (ix + 1) % nx] * a
    hi = grid[(iy + 1) % ny, ix % nx] * (1 - a) + grid[(iy + 1) % ny, (ix + 1) % nx] * a
    return lo * (1 - b) + hi * b


def ramp(value, lo, hi):
    t = np.clip((value - lo) / (hi - lo), 0, 1)
    return t * t * (3 - 2 * t)


def spot(u, v, x, y, rx, ry):
    return np.exp(-((u - x) / rx) ** 2 - ((v - y) / ry) ** 2)


def pigment(value, dark, light):
    a, b = np.asarray(dark) / 255.0, np.asarray(light) / 255.0
    t = np.clip(value, 0, 1)[..., None]
    return a * (1 - t) + b * t


def color_mix(a, b, t):
    return a + (b - a) * np.clip(t, 0, 1)[..., None]


def edges(u, v):
    return np.minimum(np.minimum(u, 1 - u), np.minimum(v, 1 - v))


def wood(u, v, seed, rings, knots=()):
    warp = 2.6 * noise(u, v, 4, 8, seed) + 0.7 * noise(u, v, 7, 23, seed + 1)
    knot_ink = np.zeros_like(u)
    for x, y, size in knots:
        dx, dy = (u - x) / size, (v - y) / (size * 1.35)
        halo = np.exp(-(0.58 * dx * dx + dy * dy))
        warp += 3.1 * halo * np.tanh(dy * 1.8)
        knot_ink += np.exp(-(dx * dx * 2 + dy * dy * 12)) * 0.30
    phase = v * rings + warp + 0.65 * np.sin(TAU * (u * 2 + v * 2))
    earlywood = np.sin(TAU * phase) * 0.5 + 0.5
    pores = ramp(noise(u, v, 45, 150, seed + 2), 0.64, 0.94)
    ribbon = noise(u, v, 6, 38, seed + 3)
    grain = 0.37 + earlywood * 0.30 + ribbon * 0.29 - pores * 0.13 - knot_ink
    height = 0.00036 * earlywood + 0.00025 * ribbon - 0.00028 * pores
    return grain, height


def counter_top(u, v):
    grain, height = wood(u, v, 1901, 41, ((0.29, 0.38, 0.052), (0.73, 0.66, 0.061)))
    annual = noise(u, v, 11, 7, 1908)
    bands = np.minimum.reduce([np.abs(v - p) for p in (0.30, 0.59, 0.79)])
    joint = 1 - ramp(bands, 0.001, 0.006)
    plank = np.choose(np.digitize(v, [0.30, 0.59, 0.79]), (0.04, -0.03, 0.06, -0.02))
    hands = (0.36 + 0.55 * spot(u, v, 0.34, 0.12, 0.24, 0.22)
             + 0.37 * spot(u, v, 0.77, 0.12, 0.17, 0.19))
    wear = (1 - ramp(v, 0.055, 0.19 + annual * 0.10)) * hands
    scuffs = ramp(noise(u + v * 0.011, v - u * 0.016, 37, 105, 1912), 0.71, 0.96) * wear
    amber = pigment(grain + plank - joint * 0.23, (65, 29, 14), (153, 87, 43))
    dry = pigment(grain + annual * 0.19 + scuffs * 0.28, (105, 55, 26), (194, 137, 82))
    return color_mix(amber, dry, wear * 0.88), height - joint * 0.00038 - scuffs * 0.00031, (
        0.58 + grain * 0.19 - joint * 0.20 - wear * 0.33 - scuffs * 0.05)


def counter_front(u, v):
    grain, height = wood(u, v, 1920, 40, ((0.16, 0.41, 0.032), (0.81, 0.69, 0.044)))
    panel_u = (u * 4) % 1
    border = edges(panel_u, v)
    groove = (1 - ramp(np.abs(border - 0.09), 0.005, 0.011))
    lip = (1 - ramp(np.abs(border - 0.112), 0.005, 0.015))
    step = np.choose(np.minimum((u * 4).astype(int), 3), (0.04, -0.045, 0.045, -0.03))
    toe_marks = ramp(noise(u, v, 26, 19, 1925), 0.54, 0.9) * (1 - ramp(v, 0.04, 0.32))
    field = grain * 0.82 + step + lip * 0.20 - groove * 0.32 + toe_marks * 0.13
    return pigment(field, (44, 21, 12), (132, 73, 38)), (
        height - 0.0018 * groove + 0.0011 * lip), 0.37 + grain * 0.19 - toe_marks * 0.19


def trim_wood(u, v):
    grain, height = wood(u, v, 1940, 13)
    hand_band = 0.26 + 0.7 * np.sin(np.pi * u) ** 8
    scrape = ramp(noise(u, v, 18, 46, 1945), 0.53, 0.89)
    width = (1 - ramp(np.minimum(v, 1 - v), 0.05, 0.34))
    rub = hand_band * width * (0.37 + scrape * 0.63)
    finish = pigment(grain, (53, 27, 14), (126, 68, 34))
    exposed = pigment(grain, (129, 79, 43), (193, 140, 84))
    return color_mix(finish, exposed, rub * 0.82), height - rub * 0.0006, 0.52 + grain * 0.12 - rub * 0.35


def floor_tiles(u, v):
    # 36 × 24 half-metre tiles across the existing 18 × 12 m visual slab.
    tx, tz = u * 36, v * 24
    ix, iz = tx.astype(int), tz.astype(int)
    fu, fv = tx % 1, tz % 1
    coarse = noise(u, v, 103, 69, 1950)
    fine = noise(u, v, 410, 293, 1951)
    grit = noise(u, v, 710, 513, 1952)
    cell = np.random.default_rng(1953).random((24, 36))[iz, ix]
    d = edges(fu, fv) + (fine - 0.5) * 0.012
    chips = ramp(coarse * 0.77 + fine * 0.23, 0.70, 0.87)
    d -= 0.065 * chips * (1 - ramp(d, 0.019, 0.08))
    face = ramp(d, 0.010, 0.041)
    rim = ramp(d, 0.034, 0.055) - ramp(d, 0.055, 0.091)
    cloudy = 0.36 + 0.15 * coarse + 0.11 * fine + 0.18 * cell + rim * 0.24
    # S20: menos contraste entre baldosas (antes 136-204 crema / 36-107 verde): el damero sigue, pero ya no compite con
    # el tio ni parpadea al moverse; el desgaste y las juntas pasan a ser lo que guia la lectura del piso.
    cream = pigment(cloudy, (124, 106, 80), (176, 160, 128))
    green = pigment(cloudy, (66, 80, 70), (108, 122, 106))
    tile = np.where((((ix + iz) % 2) == 0)[..., None], cream, green)

    x, z = u * 18 - 9, v * 12 - 2
    aisle_x = 4.35 * ramp(z, -1.0, 7.0)
    aisle = np.exp(-((x - aisle_x) / 0.92) ** 2)
    bar = np.exp(-((z - 7.34) / 0.64) ** 2 - ((x - 4.32) / 2.40) ** 4)
    wear = np.maximum(aisle * 0.60, bar * 0.78) * (0.40 + coarse * 0.60)
    drag_lines = ramp(noise(u + v * 0.22, v, 320, 8, 1954), 0.71, 0.91) * 0.24
    tile = color_mix(tile, tile * 0.82 + np.asarray((0.16, 0.128, 0.080)),
                     wear * 0.83 + drag_lines * wear)
    joints = pigment(coarse * 0.70 + fine * 0.3, (53, 44, 36), (87, 73, 59))
    smooth = (0.38 + fine * 0.17 + wear * 0.21 - (1 - face) * 0.34 - drag_lines * wear * 0.19)
    return color_mix(joints, tile, face), 0.0037 * face + (grit - 0.5) * (0.00024 + 0.00046 * (1 - face)), smooth


def plaster(u, v):
    mottle = noise(u, v, 6, 6, 1970)
    spread = noise(u, v, 14, 16, 1971)
    fine = noise(u, v, 155, 167, 1972)
    trowel = noise(u + 0.04 * np.sin(TAU * v), v - u, 18, 55, 1973)
    pocks = ramp(noise(u, v, 138, 129, 1974), 0.76, 0.98)
    patch = ramp(spread, 0.48, 0.82) * ramp(trowel, 0.42, 0.73)
    brush = 0.26 + mottle * 0.30 + spread * 0.15 + fine * 0.14 - patch * 0.17
    h = (fine - 0.5) * 0.0010 + (trowel - 0.5) * 0.00135 - pocks * 0.0011
    # S20: revoque algo mas claro (antes 123-170): las paredes se perdian a distancia
    return pigment(brush, (138, 126, 101), (186, 171, 139)), h, 0.09 + fine * 0.12 - patch * 0.035


def exit_door(u, v):
    fine = noise(u, v, 182, 231, 1980)
    grain = noise(u, v, 103, 7, 1981)
    clouds = noise(u, v, 15, 13, 1982)
    dist = edges(u, v)
    # Painted inset line and stripped varnish near the contact and latch.
    routed = 1 - ramp(np.abs(dist - 0.13), 0.004, 0.012)
    lip = 1 - ramp(np.abs(dist - 0.147), 0.003, 0.012)
    hand = spot(u, v, 0.873, 0.48, 0.09, 0.113) + 0.58 * spot(u, v, 0.80, 0.51, 0.105, 0.135)
    latch = 0.91 * spot(u, v, 1.0, 0.48, 0.043, 0.20)
    frame_contact = (1 - ramp(dist, 0.003, 0.028)) * (0.23 + clouds * 0.58)
    kick = spot(u, v, 0.69, 0.122, 0.24, 0.077) * 0.64
    rough = noise(u + v * 0.067, v - u * 0.15, 77, 43, 1983)
    film = (hand * 0.72 + latch + frame_contact + kick) * (0.47 + clouds * 0.53)
    exposed = ramp(film * 0.64 + rough * 0.44, 0.61, 0.84)
    scrapes = ramp(rough, 0.75, 0.96) * np.clip(film, 0, 1)
    paint = pigment(0.33 + clouds * 0.22 + grain * 0.24 - routed * 0.43 + lip * 0.19,
                    (19, 43, 43), (48, 99, 86))
    chalk = pigment(fine * 0.45 + rough * 0.17, (76, 103, 79), (156, 153, 108))
    worn = color_mix(paint, chalk, ramp(film, 0.10, 0.92) * 0.54 + scrapes * 0.46)
    undercoat = pigment(grain, (82, 54, 34), (157, 114, 67))
    return color_mix(worn, undercoat, exposed * 0.94), (
        (grain - 0.5) * 0.0009 + (fine - 0.5) * 0.0006 - routed * 0.0026 - exposed * 0.0017
    ), 0.42 + fine * 0.13 - routed * 0.13 - exposed * 0.26 - scrapes * 0.15


def save(name, size, metres, surface, repeat=False):
    w, h = size
    u, v = uv(w, h)
    color, height, smooth = surface(u, v)
    if repeat:
        dx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * (w / (2 * metres[0]))
        dy = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) * (h / (2 * metres[1]))
    else:
        dy, dx = np.gradient(height, metres[1] / h, metres[0] / w)
    length = np.sqrt(dx * dx + dy * dy + 1)
    normal = np.stack((-dx / length, -dy / length, 1 / length), axis=-1) * 0.5 + 0.5
    rgba = np.concatenate((color, smooth[..., None]), axis=-1)
    for suffix, pixels in (("color_gloss", rgba), ("normal", normal)):
        image = np.clip(np.rint(pixels[::-1] * 255), 0, 255).astype(np.uint8)
        Image.fromarray(image).save(OUT / f"{name}_{suffix}.png", optimize=True)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    save("bar_top", (1024, 256), (4.12, 0.90), counter_top)
    save("bar_front", (1024, 256), (4.00, 0.96), counter_front)
    save("contact_trim", (512, 128), (2.20, 0.12), trim_wood, repeat=True)
    save("interior_tiles", (2048, 2048), (18, 12), floor_tiles)
    save("matte_plaster", (768, 768), (2.67, 2.67), plaster, repeat=True)
    save("exit_leaf", (768, 1024), (1.50, 2.20), exit_door)


if __name__ == "__main__":
    main()
