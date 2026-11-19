from pathlib import Path
from PIL import Image
import math

ROOT = Path(r"C:\Học\ThucTap")
SRC = ROOT / "Assets/Phu/Sprite asset/Boss Demos/boss_velvet_chapter3_chibi.png"
OUT = ROOT / "Assets/Phu/Sprite asset/Velvet Chibi Animations/Idle/Frames"
SIZE = (512, 640)

src = Image.open(SRC).convert("RGBA")
pix = src.load()
for y in range(src.height):
    for x in range(src.width):
        r, g, b, a = pix[x, y]
        if r > 246 and g > 246 and b > 246:
            pix[x, y] = (r, g, b, 0)
        elif r > 235 and g > 235 and b > 235:
            alpha = max(0, 255 - min(r, g, b)) * 4
            pix[x, y] = (r, g, b, min(a, alpha))

OUT.mkdir(parents=True, exist_ok=True)
for i in range(24):
    phase = (i % 12) / 11
    breath = math.sin(phase * math.pi * 2)
    scale = 1.0 + 0.012 * breath
    w = max(1, round(src.width * scale)); h = max(1, round(src.height * scale))
    im = src.resize((w, h), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", SIZE, (0, 0, 0, 0))
    x = (SIZE[0] - w) // 2 + round(2 * math.sin(phase * math.pi * 2))
    y = (SIZE[1] - h) // 2 - round(2 * breath)
    canvas.alpha_composite(im, (x, y))
    canvas.save(OUT / f"velvet_chibi_idle_{i+1:03d}.png")

print(f"Wrote 24 frames to {OUT}")
