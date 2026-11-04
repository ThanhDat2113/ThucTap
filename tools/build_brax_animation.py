from pathlib import Path
from PIL import Image
import re, uuid, random

ROOT = Path(r"C:\Học\ThucTap\Assets\Phu\Sprite asset\Brax Animations")
GEN = Path(r"C:\Users\LENOVO\.codex\generated_images\01a11eb7-efe2-7500-9b62-e25dd0886fd4")
TEMPLATE = Path(r"C:\Học\ThucTap\Assets\Luan\Character\Riff\Riff.png.meta")
SIZE = (384, 480)
SOURCES = {
    "dance_01_08": GEN / "exec-3d1789c1-3820-4bd4-a615-5525de90f3e6.png",
    "dance_09_16": GEN / "exec-6b393ab1-95ab-42fe-9c14-ebb1490276cc.png",
    "dance_17_24": GEN / "exec-e7c17096-c079-4101-b47b-59bdc50c05e5.png",
    "dance_transition_01": GEN / "exec-31d02272-2c2f-47f1-9599-cb711da3be0e.png",
    "dance_transition_02": GEN / "exec-c6088510-b898-405f-b0b2-cf60cda1ddf8.png",
    "dance_transition_03": GEN / "exec-62e76920-63b2-4c7b-88af-e93e139b5a31.png",
    "idle": GEN / "exec-4c4f03a9-6b73-4ba3-b7a5-6c0924c0105c.png",
    "miss": GEN / "exec-6a754435-4717-406b-b6aa-38800eaa701f.png",
    "idle_transition": GEN / "exec-a0d808aa-cd2b-48ef-8680-1b8e933c2e8b.png",
    "miss_transition": GEN / "exec-69c921b5-2090-4191-ac78-ff96fc0eddd8.png",
}


def normalize(im, margin=20):
    im = im.convert("RGBA")
    box = im.getchannel("A").getbbox()
    if not box:
        return Image.new("RGBA", SIZE)
    im = im.crop(box)
    scale = min((SIZE[0] - 2 * margin) / im.width, (SIZE[1] - 2 * margin) / im.height)
    im = im.resize((max(1, round(im.width * scale)), max(1, round(im.height * scale))), Image.Resampling.LANCZOS)
    out = Image.new("RGBA", SIZE)
    out.alpha_composite(im, ((SIZE[0] - im.width) // 2, SIZE[1] - margin - im.height))
    return out


def remove_neighbor_fragments(im, keep_detached=False):
    """Drop disconnected alpha islands that enter a grid cell from its border."""
    im = im.convert("RGBA")
    alpha = im.getchannel("A")
    px = alpha.load()
    w, h = im.size
    seen, comps = set(), []
    for y in range(h):
        for x in range(w):
            if px[x, y] <= 80 or (x, y) in seen:
                continue
            q, comp, touches = [(x, y)], [], False
            seen.add((x, y))
            while q:
                cx, cy = q.pop()
                comp.append((cx, cy))
                touches = touches or cx <= 1 or cy <= 1 or cx >= w - 2 or cy >= h - 2
                for nx, ny in ((cx - 1, cy), (cx + 1, cy), (cx, cy - 1), (cx, cy + 1)):
                    if 0 <= nx < w and 0 <= ny < h and px[nx, ny] > 80 and (nx, ny) not in seen:
                        seen.add((nx, ny))
                        q.append((nx, ny))
            comps.append((comp, touches))
    if not comps:
        return im
    main = max(comps, key=lambda item: len(item[0]))[0]
    keep = set(main)
    mx = [p[0] for p in main]
    my = [p[1] for p in main]
    expanded = (min(mx) - 90, min(my) - 90, max(mx) + 90, max(my) + 90)
    for comp, touches in comps:
        if comp is main or touches or len(comp) < 24:
            continue
        if not keep_detached:
            continue
        cx = sum(x for x, _ in comp) / len(comp)
        cy = sum(y for _, y in comp) / len(comp)
        # Neighboring poses enter as narrow islands along the left/right seam.
        if cx < w * 0.12 or cx > w * 0.88:
            continue
        if expanded[0] <= cx <= expanded[2] and expanded[1] <= cy <= expanded[3]:
            keep.update(comp)
    # Restore the antialiased fringe immediately around the retained solid shapes.
    expanded_keep = set(keep)
    for x, y in list(keep):
        for dy in (-2, -1, 0, 1, 2):
            for dx in (-2, -1, 0, 1, 2):
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and px[nx, ny] > 0:
                    expanded_keep.add((nx, ny))
    mask = Image.new("L", (w, h))
    mp = mask.load()
    for x, y in expanded_keep:
        mp[x, y] = px[x, y]
    out = im.copy()
    out.putalpha(mask)
    return out


def background_to_alpha(im, threshold=14):
    """Flood smooth generated backdrops from the cell corners into transparency."""
    im = im.convert("RGBA")
    if im.getchannel("A").getextrema()[0] < 250:
        return im
    rgb = im.convert("RGB")
    pix = rgb.load()
    w, h = rgb.size
    seeds = [(0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)]
    seen = set(seeds)
    q = list(seeds)
    while q:
        x, y = q.pop()
        c = pix[x, y]
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if not (0 <= nx < w and 0 <= ny < h) or (nx, ny) in seen:
                continue
            n = pix[nx, ny]
            if max(abs(c[i] - n[i]) for i in range(3)) <= threshold:
                seen.add((nx, ny))
                q.append((nx, ny))
    alpha = Image.new("L", (w, h), 255)
    ap = alpha.load()
    for x, y in seen:
        ap[x, y] = 0
    out = im.copy()
    out.putalpha(alpha)
    return out


def clear_alpha_region(im, box):
    out = im.copy().convert("RGBA")
    alpha = out.getchannel("A")
    alpha.paste(0, box)
    out.putalpha(alpha)
    return out


def split_grid(path, cols, rows, keep_detached=False):
    im = Image.open(path).convert("RGBA")
    frames = []
    for y in range(rows):
        for x in range(cols):
            l, r = round(x * im.width / cols), round((x + 1) * im.width / cols)
            t, b = round(y * im.height / rows), round((y + 1) * im.height / rows)
            cell = background_to_alpha(im.crop((l, t, r, b)))
            frames.append(normalize(remove_neighbor_fragments(cell, keep_detached)))
    return frames


def make_sheet(frames, path, cols):
    rows = (len(frames) + cols - 1) // cols
    out = Image.new("RGBA", (SIZE[0] * cols, SIZE[1] * rows))
    for i, frame in enumerate(frames):
        out.alpha_composite(frame, ((i % cols) * SIZE[0], (i // cols) * SIZE[1]))
    out.save(path)


def folder_meta(path):
    meta = Path(str(path) + ".meta")
    if not meta.exists():
        meta.write_text(
            f"fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n",
            encoding="utf-8",
        )


def sprite_meta(path):
    meta = Path(str(path) + ".meta")
    if meta.exists():
        return
    name = path.stem
    internal = random.randint(10**17, 9 * 10**18)
    text = TEMPLATE.read_text(encoding="utf-8")
    text = re.sub(r"^guid: .+$", f"guid: {uuid.uuid4().hex}", text, count=1, flags=re.M)
    text = text.replace("spritePixelsToUnits: 100", "spritePixelsToUnits: 375")
    text = text.replace("Riff", name)
    text = text.replace("480625313784399930", str(internal))
    text = re.sub(r"spriteID: [0-9a-f]+", lambda m: f"spriteID: {uuid.uuid4().hex[:24]}0800000000000000", text)
    text = re.sub(r"        x: 57\n        y: 19\n        width: 141\n        height: 286", "        x: 0\n        y: 0\n        width: 384\n        height: 480", text)
    meta.write_text(text, encoding="utf-8", newline="\n")


def save_set(label, frames, cols, duration):
    base = ROOT / label
    frame_dir = base / "Frames"
    frame_dir.mkdir(parents=True, exist_ok=True)
    folder_meta(base)
    folder_meta(frame_dir)
    for old in frame_dir.glob("*.png"):
        old.unlink()
    for i, frame in enumerate(frames, 1):
        path = frame_dir / f"brax_{label.lower()}_{i:03d}.png"
        frame.save(path)
        sprite_meta(path)
    sheet = base / f"brax_{label.lower()}_{len(frames)}.png"
    make_sheet(frames, sheet, cols)
    frames[0].save(base / f"brax_{label.lower()}_{len(frames)}_preview.gif", save_all=True,
                   append_images=frames[1:], duration=duration, loop=0, disposal=2, optimize=False)


ROOT.mkdir(parents=True, exist_ok=True)
folder_meta(ROOT)
src = ROOT / "Source Sheets"
src.mkdir(exist_ok=True)
folder_meta(src)
for name, path in SOURCES.items():
    Image.open(path).save(src / f"brax_{name}.png")

dance_keys = (split_grid(SOURCES["dance_transition_01"], 6, 2)
              + split_grid(SOURCES["dance_transition_02"], 6, 2)
              + split_grid(SOURCES["dance_transition_03"], 6, 2))
key_dir = ROOT / "Dance" / "Key Poses"
key_dir.mkdir(parents=True, exist_ok=True)
folder_meta(ROOT / "Dance")
folder_meta(key_dir)
for old in key_dir.glob("*.png"):
    old.unlink()
for i, frame in enumerate(dance_keys, 1):
    frame.save(key_dir / f"brax_dance_key_{i:03d}.png")
make_sheet(dance_keys, ROOT / "Dance" / "brax_dance_keyposes_36.png", 9)

# 36 closely spaced transition poses. A 4/3/3 cadence yields exactly 120 crisp
# frames while keeping holds short and avoiding alpha cross-fade ghosts.
dance = []
for i, frame in enumerate(dance_keys):
    dance.extend(frame.copy() for _ in range((4, 3, 3)[i % 3]))
idle_base = split_grid(SOURCES["idle"], 6, 2)
idle_mid = split_grid(SOURCES["idle_transition"], 6, 2)
idle = [frame for pair in zip(idle_base, idle_mid) for frame in pair]
miss_base = split_grid(SOURCES["miss"], 6, 2, keep_detached=True)
miss_mid = split_grid(SOURCES["miss_transition"], 6, 2, keep_detached=True)
miss = [frame for pair in zip(miss_base, miss_mid) for frame in pair]
# Source cells 5 and 6 contain a disconnected boot from the neighboring pose.
# These correspond to final Miss frames 9 and 11 after interleaving.
for frame_index in (8, 10):
    miss[frame_index] = remove_neighbor_fragments(miss[frame_index], keep_detached=False)
# The neighboring boot touches the real boot at the bottom edge, so erase its
# narrow known spill area without changing Brax's body or airborne drumstick.
miss[8] = clear_alpha_region(miss[8], (0, 300, 120, 480))
miss[10] = clear_alpha_region(miss[10], (0, 300, 112, 480))
save_set("Dance", dance, 10, 17)
save_set("Idle", idle, 6, 100)
save_set("Miss", miss, 6, 90)
print(f"Dance={len(dance)} Keys={len(dance_keys)} Idle={len(idle)} Miss={len(miss)}")
