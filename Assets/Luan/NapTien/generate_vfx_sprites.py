import os
import math
from PIL import Image, ImageDraw, ImageFont, ImageFilter

out_dir = r"Assets/Luan/NapTien/Sprites"
os.makedirs(out_dir, exist_ok=True)

# 1. Radial Glow Auras (256x256)
def generate_glow_aura(filename, color_rgb):
    w, h = 256, 256
    img = Image.new("RGBA", (w, h), (0,0,0,0))
    draw = ImageDraw.Draw(img)
    cx, cy = w // 2, h // 2
    max_r = w // 2 - 4
    for r in range(max_r, 0, -2):
        ratio = r / float(max_r)
        # exponential falloff
        alpha = int(220 * math.pow(1.0 - ratio, 1.8))
        draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(color_rgb[0], color_rgb[1], color_rgb[2], alpha))
    img = img.filter(ImageFilter.GaussianBlur(6))
    img.save(os.path.join(out_dir, filename))
    print(filename, "created")

# 2. Cyber Card Panel (400x520) - with subtle tech hex / line details & neon rim
def generate_cyber_card_panel():
    w, h = 400, 520
    img = Image.new("RGBA", (w, h), (0,0,0,0))
    draw = ImageDraw.Draw(img)
    r = 22

    # Card gradient background
    base = Image.new("RGBA", (w, h), (0,0,0,0))
    b_draw = ImageDraw.Draw(base)
    for y in range(h):
        t = y / float(h)
        # Deep blue-purple to dark indigo
        cr = int(18 * (1-t) + 10 * t)
        cg = int(22 * (1-t) + 12 * t)
        cb = int(48 * (1-t) + 26 * t)
        ca = int(235 * (1-t) + 245 * t)
        b_draw.line([(0, y), (w, y)], fill=(cr, cg, cb, ca))

    mask = Image.new("L", (w, h), 0)
    ImageDraw.Draw(mask).rounded_rectangle([4, 4, w - 5, h - 5], radius=r, fill=255)
    img.paste(base, (0, 0), mask)

    # Tech grid lines on the lower portion
    grid_img = Image.new("RGBA", (w, h), (0,0,0,0))
    g_draw = ImageDraw.Draw(grid_img)
    for gy in range(260, h - 30, 24):
        g_draw.line([(20, gy), (w - 20, gy)], fill=(0, 220, 255, 18), width=1)
    for gx in range(30, w - 20, 36):
        g_draw.line([(gx, 260), (gx, h - 30)], fill=(0, 220, 255, 12), width=1)
    img.paste(grid_img, (0, 0), mask)

    # Border stroke
    draw.rounded_rectangle([4, 4, w - 5, h - 5], radius=r, outline=(0, 180, 240, 160), width=2)
    # Bright top neon lip
    draw.line([(40, 4), (w - 40, 4)], fill=(0, 255, 230, 240), width=3)
    # Bottom neon lip
    draw.line([(50, h - 5), (w - 50, h - 5)], fill=(255, 0, 180, 200), width=2)

    # Tech brackets on corners
    draw.line([(12, 35), (12, 16), (35, 16)], fill=(0, 255, 240, 255), width=3)
    draw.line([(w - 35, 16), (w - 12, 16), (w - 12, 35)], fill=(0, 255, 240, 255), width=3)
    draw.line([(12, h - 35), (12, h - 16), (35, h - 16)], fill=(255, 0, 180, 240), width=3)
    draw.line([(w - 35, h - 16), (w - 12, h - 16), (w - 12, h - 35)], fill=(255, 0, 180, 240), width=3)

    img.save(os.path.join(out_dir, "card_cyber_panel.png"))
    print("card_cyber_panel.png created")

# 3. Card Hover Glowing Frame (400x520)
def generate_card_hover_frame():
    w, h = 400, 520
    img = Image.new("RGBA", (w, h), (0,0,0,0))
    draw = ImageDraw.Draw(img)
    r = 22

    # Multiple layers of intense neon cyan & yellow glow
    for o in range(10, 0, -1):
        alpha = int(28 * (11 - o))
        draw.rounded_rectangle([4 - o, 4 - o, w - 5 + o, h - 5 + o], radius=r + o, outline=(0, 240, 255, alpha), width=1)
    
    draw.rounded_rectangle([3, 3, w - 4, h - 4], radius=r, outline=(255, 240, 80, 255), width=3)
    img.save(os.path.join(out_dir, "card_cyber_frame_hover.png"))
    print("card_cyber_frame_hover.png created")

# 4. Shimmer Streak (200x500) - for diagonal sweep
def generate_shimmer_streak():
    w, h = 160, 500
    img = Image.new("RGBA", (w, h), (0,0,0,0))
    draw = ImageDraw.Draw(img)
    for x in range(w):
        dist = abs(x - w // 2) / float(w // 2)
        alpha = int(180 * math.pow(1.0 - dist, 2.2))
        draw.line([(x, 0), (x, h)], fill=(255, 255, 255, alpha))
    img.save(os.path.join(out_dir, "fx_shimmer_streak.png"))
    print("fx_shimmer_streak.png created")

# 5. Particle Sparkle (64x64)
def generate_particle_sparkle():
    w, h = 64, 64
    img = Image.new("RGBA", (w, h), (0,0,0,0))
    draw = ImageDraw.Draw(img)
    cx, cy = 32, 32
    # 4-pointed star
    star = [
        (cx, 4), (cx + 6, cy - 6), (w - 4, cy), (cx + 6, cy + 6),
        (cx, h - 4), (cx - 6, cy + 6), (4, cy), (cx - 6, cy - 6)
    ]
    draw.polygon(star, fill=(255, 255, 255, 255))
    # Soft center glow
    for r in range(16, 0, -2):
        alpha = int(120 * (1.0 - r / 16.0))
        draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(0, 240, 255, alpha))
    img.save(os.path.join(out_dir, "fx_particle_sparkle.png"))
    print("fx_particle_sparkle.png created")

# 6. Title Graphic "NẠP TIỀN" (600x200) matching title_shop.png
def generate_title_naptien_graphic():
    w, h = 600, 180
    img = Image.new("RGBA", (w, h), (0,0,0,0))
    draw = ImageDraw.Draw(img)

    # Stylized 3D backdrop plate
    plate_poly = [
        (30, 25), (w - 50, 15), (w - 20, 60), (w - 40, h - 25),
        (w - 70, h - 15), (40, h - 20), (15, h - 50), (20, 50)
    ]
    # Outer glow
    for o in range(8, 0, -1):
        draw.polygon([(p[0], p[1]) for p in plate_poly], outline=(0, 220, 255, int(20 * (9-o))))
    # Shadow
    draw.polygon([(p[0]+6, p[1]+8) for p in plate_poly], fill=(0, 0, 0, 200))
    # Plate body: Magenta to dark purple gradient
    p_grad = Image.new("RGBA", (w, h), (0,0,0,0))
    pg_draw = ImageDraw.Draw(p_grad)
    for y in range(h):
        ratio = y / float(h)
        cr = int(220 * (1 - ratio) + 80 * ratio)
        cg = int(20 * (1 - ratio) + 15 * ratio)
        cb = int(140 * (1 - ratio) + 160 * ratio)
        pg_draw.line([(0, y), (w, y)], fill=(cr, cg, cb, 240))
    p_mask = Image.new("L", (w, h), 0)
    ImageDraw.Draw(p_mask).polygon(plate_poly, fill=255)
    img.paste(p_grad, (0,0), p_mask)

    # Outline of plate
    draw.polygon(plate_poly, outline=(0, 255, 230, 255), width=3)
    # Inner gold border
    draw.polygon([(p[0]*0.96 + w*0.02, p[1]*0.94 + h*0.03) for p in plate_poly], outline=(255, 220, 50, 180), width=2)

    # Big 3D Title Letters using FreeType font or drawn
    try:
        font = ImageFont.truetype("arialbd.ttf", 68)
        sub_font = ImageFont.truetype("arialbd.ttf", 20)
    except:
        font = ImageFont.load_default()
        sub_font = ImageFont.load_default()

    text = "NẠP TIỀN"
    tx, ty = 140, 36
    # 3D Extrusion shadow
    for d in range(8, 0, -1):
        draw.text((tx + d, ty + d), text, font=font, fill=(10, 5, 25, 255))
    # Comic outline
    for ox in [-3, -2, -1, 0, 1, 2, 3]:
        for oy in [-3, -2, -1, 0, 1, 2, 3]:
            draw.text((tx + ox, ty + oy), text, font=font, fill=(0, 0, 0, 255))
    # Front fill: Neon Yellow-to-Cyan gradient
    draw.text((tx, ty), text, font=font, fill=(0, 255, 230, 255))
    draw.text((tx, ty-2), text, font=font, fill=(255, 245, 120, 255))

    # Subtext
    subtext = "RECHARGE STORE • KIM CƯƠNG & XU"
    stx, sty = 142, 115
    for sox in [-1, 0, 1]:
        for soy in [-1, 0, 1]:
            draw.text((stx + sox, sty + soy), subtext, font=sub_font, fill=(0, 0, 0, 255))
    draw.text((stx, sty), subtext, font=sub_font, fill=(255, 255, 255, 255))

    # Sparkling gems icon on the left
    gx, gy = 70, 75
    # Diamond shape
    d_pts = [(gx, gy - 36), (gx + 32, gy - 8), (gx, gy + 36), (gx - 32, gy - 8)]
    draw.polygon([(p[0]+4, p[1]+4) for p in d_pts], fill=(0,0,0,180))
    draw.polygon(d_pts, fill=(0, 230, 255, 255), outline=(255, 255, 255, 255))
    draw.line([(gx - 32, gy - 8), (gx + 32, gy - 8)], fill=(255, 255, 255, 220), width=2)
    draw.line([(gx, gy - 36), (gx, gy + 36)], fill=(255, 255, 255, 180), width=2)

    img.save(os.path.join(out_dir, "title_naptien_banner.png"))
    print("title_naptien_banner.png created")

if __name__ == "__main__":
    generate_glow_aura("glow_aura_cyan.png", (0, 220, 255))
    generate_glow_aura("glow_aura_gold.png", (255, 190, 30))
    generate_glow_aura("glow_aura_purple.png", (240, 40, 180))
    generate_cyber_card_panel()
    generate_card_hover_frame()
    generate_shimmer_streak()
    generate_particle_sparkle()
    generate_title_naptien_graphic()
    print("ALL VISUAL ENHANCEMENT SPRITES CREATED!")
