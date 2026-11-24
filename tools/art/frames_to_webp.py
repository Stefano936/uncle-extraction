"""Cuadros PNG de juego real (-ueFrames) a WebP animado. Uso: python frames_to_webp.py <carpeta> <salida.webp> <fps> [ancho]"""
import sys
from pathlib import Path
from PIL import Image

src, out, fps = Path(sys.argv[1]), Path(sys.argv[2]), int(sys.argv[3])
width = int(sys.argv[4]) if len(sys.argv) > 4 else 960
frames = sorted(src.glob("f*.png"))
if not frames:
    sys.exit(f"sin cuadros en {src}")
imgs = []
for f in frames:
    im = Image.open(f).convert("RGB")
    if im.width != width:
        im = im.resize((width, round(im.height * width / im.width)), Image.LANCZOS)
    imgs.append(im)
imgs[0].save(out, save_all=True, append_images=imgs[1:], duration=int(1000 / fps), loop=0, quality=72, method=4)
print(f"{out} {len(imgs)} cuadros {out.stat().st_size // 1024} KB")
