"""Rebuild the textured finishes from the S21 source assets (Pillow and NumPy)."""
from io import BytesIO
from pathlib import Path
import subprocess

import numpy as np
from PIL import Image

from generate_s19_surfaces import noise, uv

ROOT = Path(__file__).resolve().parents[2]
TEXTURES = Path("UnityProject/Assets/Game/Art/Materials/Textures")
SOURCE = "9cf7a28bfb6b383c8059d08ae5d9b7cdcfca665b"


def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT)


def height(family, width, height_px):
    u, v = uv(width, height_px)
    fine = noise(u, v, 87, 79, 913) - .5
    cloud = noise(u, v, 9, 11, 371) - .5
    if family == "fabric":
        warp = np.cos(2 * np.pi * u * 22)
        weft = np.cos(2 * np.pi * v * 24)
        return .19 * warp * weft + .19 * fine + .19 * cloud
    if family in ("wood", "metal", "hair"):
        grids = {"wood": (12, 76), "metal": (8, 122), "hair": (70, 9)}
        nx, ny = grids[family]
        grain = noise(u, v, nx, ny, 827) - .5
        return .57 * grain + .19 * fine + .23 * cloud
    if family == "skin":
        pores = np.maximum(fine - .08, 0)
        return .29 * cloud - .37 * pores
    if family == "leather":
        pebbles = noise(u, v, 41, 37, 311) - .5
        return .49 * pebbles + .14 * fine + .18 * cloud
    grains = noise(u, v, 46, 51, 271) - .5
    return .39 * grains + .38 * fine + .26 * cloud


def family_for(stem):
    if any(word in stem for word in ("wood", "bar_front", "bar_top", "trim")):
        return "wood"
    if any(word in stem for word in ("metal", "paint", "exit")):
        return "metal"
    if "rubber" in stem:
        return "leather"
    return "stone"


def rgb_bytes(values):
    return np.round(np.clip(values, 0, 1) * 255).astype(np.uint8)


def normal_map(detail, base=None, strength=1):
    x = (np.roll(detail, 1, axis=1) - np.roll(detail, -1, axis=1)) * strength
    y = (np.roll(detail, -1, axis=0) - np.roll(detail, 1, axis=0)) * strength
    slopes = np.stack((x, y, np.ones_like(x)), axis=-1)
    if base is not None:
        n = base[:, :, :3] * 2 - 1
        slopes[:, :, :2] += n[:, :, :2] / np.maximum(n[:, :, 2:], .13)
    slopes /= np.linalg.norm(slopes, axis=-1, keepdims=True)
    return rgb_bytes(slopes * .5 + .5)


def save(path, data):
    target = ROOT / path
    target.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(data).save(target, optimize=True)


def refine(path):
    with Image.open(BytesIO(git("show", f"{SOURCE}:{path.as_posix()}"))) as image:
        mode = "RGBA" if "A" in image.getbands() else "RGB"
        data = np.asarray(image.convert(mode), dtype=np.float32) / 255
    size = (data.shape[1], data.shape[0])
    family = family_for(path.stem)
    detail = height(family, *size)
    if path.stem.endswith("_normal"):
        save(path, normal_map(detail, data, strength=.32))
        return
    strength = {"wood": .28, "metal": .24, "leather": .18, "stone": .23}[family]
    data[:, :, :3] *= 1 + strength * detail[:, :, None]
    if path.stem.endswith("_color_gloss"):
        gloss = data[:, :, 3]
        data[:, :, 3] = gloss + .18 * detail * np.minimum(gloss * 2, 1)
    save(path, rgb_bytes(data))


def main():
    for name in git("ls-tree", "-r", "--name-only", SOURCE, "--", TEXTURES.as_posix()).decode().splitlines():
        path = Path(name)
        if path.suffix == ".png":
            refine(path)
    for family in ("skin", "fabric", "leather", "stone", "metal", "hair", "wood"):
        detail = height(family, 256, 256)
        save(TEXTURES / "Detail" / f"{family}_normal.png", normal_map(detail, strength=.85))
    fabric = .86 + .42 * height("fabric", 256, 256)
    save(TEXTURES / "04_fabric_basecolor.png", rgb_bytes(np.repeat(fabric[:, :, None], 3, axis=2)))
    save(TEXTURES / "Generated" / "04_fabric_basecolor_normal.png", normal_map(height("fabric", 256, 256), strength=.8))


if __name__ == "__main__":
    main()
