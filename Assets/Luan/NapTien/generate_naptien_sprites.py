import os
import math
import random
from PIL import Image, ImageDraw, ImageFont, ImageFilter

out_dir = r"Assets/Luan/NapTien/Sprites"
os.makedirs(out_dir, exist_ok=True)

def create_gradient(width, height, c1, c2, vertical=True):
    base = Image.new("RGBA", (width, height), (0,0,0,0))
    draw = ImageDraw.Draw(base)
    for i in range(height if vertical else width):
        ratio = i / float(height if vertical else width)
        r = int(c1[0] * (1 - ratio) + c2[0] * ratio)
        g = int(c1[1] * (1 - ratio) + c2[1] * ratio)
        b = int(c1[2] * (1 - ratio) + c2[2] * ratio)
        a = int(c1[3] * (1 - ratio) + c2[3] * ratio)
        if vertical:
            draw.line([(0, i), (width, i)], fill=(r, g, b, a))
        else:
            draw.line([(i, 0), (i, height)], fill=(r, g, b, a))
    return base

# 1. Title Banner: "NẠP TIỀN" (460x110)
def generate_title_naptien():
    w, h = 460, 110
    img = Image.new("RGBA", (w, h), (0,0,0,0))
    draw = ImageDraw.Draw(img)
    
    # Outer cyber polygon shape
    poly = [
        (20, 10), (w - 40, 10), (w - 15, 35), (w - 15, h - 15),
        (w - 35, h - 10), (35, h - 10), (10, h - 35), (10, 20)
    ]
    # Neon glow outline
    for o in range(6, 0, -1):
        alpha = int(35 * (7 - o))
        draw.polygon(poly, outline=(0, 240, 255, alpha))
    
    # Dark cyber glass body
    draw.polygon(poly, fill=(15, 12, 32, 230), outline=(0, 240, 255, 255))
    
    # Accent neon line at top & bottom
    draw.line([(30, 14), (w - 45, 14)], fill=(255, 0, 180, 255), width=3)
    draw.line([(45, h - 14), (w - 30, h - 14)], fill=(0, 240, 255, 255), width=2)
    
    # Decorative tech dots
    draw.rectangle([18, 22, 24, 28], fill=(0, 240, 255, 255))
    draw.rectangle([w - 28, h - 28, w - 22, h - 22], fill=(255, 0, 180, 255))
    
    img.save(os.path.join(out_dir, "title_naptien.png"))
    print("title_naptien.png created")

# 2. Card Pack Background (260x340)
def generate_card_pack_bg():
    w, h = 260, 340
    img = Image.new("RGBA", (w, h), (0,0,0,0))
    draw = ImageDraw.Draw(img)
    
    r = 18
    # Outer glow
    for o in range(5, 0, -1):
        alpha = int(25 * (6 - o))
        draw.rounded_rectangle([6 - o, 6 - o, w - 6 + o, h - 6 + o], radius=r+o, outline=(0, 220, 255, alpha), width=1)
        
    # Main card body: Deep gradient
    grad = create_gradient(w-12, h-12, (22, 18, 48, 240), (12, 9, 28, 245), vertical=True)
    mask = Image.new("L", (w-12, h-12), 0)
    ImageDraw.Draw(mask).rounded_rectangle([0, 0, w-13, h-13], radius=r, fill=255)
    img.paste(grad, (6, 6), mask)
    
    # Border with neon stroke
    draw.rounded_rectangle([6, 6, w - 7, h - 7], radius=r, outline=(70, 120, 220, 200), width=2)
    
    # Glowing top highlight
    draw.line([(30, 7), (w - 30, 7)], fill=(0, 240, 255, 255), width=2)
    
    # Tech corner brackets
    draw.line([(10, 25), (10, 14), (25, 14)], fill=(0, 255, 230, 255), width=2)
    draw.line([(w - 25, 14), (w - 10, 14), (w - 10, 25)], fill=(255, 0, 180, 255), width=2)
    draw.line([(10, h - 25), (10, h - 14), (25, h - 14)], fill=(0, 255, 230, 200), width=2)
    draw.line([(w - 25, h - 14), (w - 10, h - 14), (w - 10, h - 25)], fill=(255, 0, 180, 200), width=2)
    
    # Inner light pedestal area where the icon sits
    ped_mask = Image.new("L", (w, h), 0)
    ped_draw = ImageDraw.Draw(ped_mask)
    ped_draw.ellipse([30, 50, w - 30, 190], fill=60)
    ped_glow = Image.new("RGBA", (w, h), (0, 210, 255, 255))
    ped_blurred = ped_mask.filter(ImageFilter.GaussianBlur(15))
    img.paste(ped_glow, (0, 0), ped_blurred)
    
    img.save(os.path.join(out_dir, "card_pack_bg.png"))
    print("card_pack_bg.png created")

# 3. Card Pack Selected / Highlight (260x340)
def generate_card_pack_highlight():
    w, h = 260, 340
    img = Image.new("RGBA", (w, h), (0,0,0,0))
    draw = ImageDraw.Draw(img)
    r = 18
    # Vibrant neon glow
    for o in range(8, 0, -1):
        alpha = int(35 * (9 - o))
        draw.rounded_rectangle([6 - o, 6 - o, w - 6 + o, h - 6 + o], radius=r+o, outline=(0, 255, 230, alpha), width=1)
        
    draw.rounded_rectangle([5, 5, w - 6, h - 6], radius=r, outline=(255, 230, 80, 255), width=3)
    img.save(os.path.join(out_dir, "card_pack_highlight.png"))
    print("card_pack_highlight.png created")

# 4. Badge Bonus Ribbon (160x44)
def generate_badge_bonus():
    w, h = 160, 44
    img = Image.new("RGBA", (w, h), (0,0,0,0))
    draw = ImageDraw.Draw(img)
    
    poly = [(12, 4), (w - 4, 4), (w - 14, h - 4), (4, h - 4)]
    # Glow
    for o in range(4, 0, -1):
        draw.polygon([(p[0]-o, p[1]-o) for p in poly], outline=(255, 50, 130, 80))
    # Gradient red/orange/pink fill
    grad = create_gradient(w, h, (255, 30, 110, 255), (255, 140, 20, 255), vertical=False)
    mask = Image.new("L", (w, h), 0)
    ImageDraw.Draw(mask).polygon(poly, fill=255)
    img.paste(grad, (0,0), mask)
    
    draw.polygon(poly, outline=(255, 255, 180, 240), width=2)
    img.save(os.path.join(out_dir, "badge_bonus.png"))
    print("badge_bonus.png created")

# 5. Price Tag Button (200x56)
def generate_btn_price_tag():
    w, h = 200, 56
    img = Image.new("RGBA", (w, h), (0,0,0,0))
    draw = ImageDraw.Draw(img)
    
    r = 26
    # Outer glow
    for o in range(4, 0, -1):
        draw.rounded_rectangle([4-o, 4-o, w-4+o, h-4+o], radius=r+o, outline=(0, 255, 160, 60))
        
    grad = create_gradient(w, h, (0, 210, 140, 255), (0, 140, 220, 255), vertical=False)
    mask = Image.new("L", (w, h), 0)
    ImageDraw.Draw(mask).rounded_rectangle([4, 4, w-5, h-5], radius=r, fill=255)
    img.paste(grad, (0,0), mask)
    
    draw.rounded_rectangle([4, 4, w-5, h-5], radius=r, outline=(220, 255, 240, 230), width=2)
    # Shiny reflex on top half
    reflex = Image.new("RGBA", (w-12, (h-8)//2), (255, 255, 255, 70))
    img.paste(reflex, (6, 5), reflex)
    img.save(os.path.join(out_dir, "btn_price_tag.png"))
    print("btn_price_tag.png created")

# 6. Payment Modal Background (720x680)
def generate_modal_payment_bg():
    w, h = 720, 680
    img = Image.new("RGBA", (w, h), (0,0,0,0))
    draw = ImageDraw.Draw(img)
    r = 24
    
    for o in range(8, 0, -1):
        alpha = int(25 * (9 - o))
        draw.rounded_rectangle([8-o, 8-o, w-8+o, h-8+o], radius=r+o, outline=(0, 220, 255, alpha), width=1)
        
    grad = create_gradient(w-16, h-16, (18, 14, 38, 250), (10, 8, 22, 252), vertical=True)
    mask = Image.new("L", (w-16, h-16), 0)
    ImageDraw.Draw(mask).rounded_rectangle([0, 0, w-17, h-17], radius=r, fill=255)
    img.paste(grad, (8, 8), mask)
    
    draw.rounded_rectangle([8, 8, w-9, h-9], radius=r, outline=(0, 220, 255, 200), width=2)
    # Header separator
    draw.line([(24, 76), (w-24, 76)], fill=(255, 0, 180, 200), width=2)
    
    img.save(os.path.join(out_dir, "modal_payment_bg.png"))
    print("modal_payment_bg.png created")

# 7. Payment Channel Badges: VietQR, ZaloPay, MoMo (160x160)
def generate_payment_icons():
    # VietQR / Bank
    w, h = 160, 160
    img = Image.new("RGBA", (w, h), (0,0,0,0))
    draw = ImageDraw.Draw(img)
    draw.rounded_rectangle([6, 6, w-7, h-7], radius=24, fill=(12, 44, 110, 255), outline=(0, 200, 255, 255), width=3)
    # Bank card / QR icon
    draw.rounded_rectangle([28, 36, w-28, h-36], radius=14, fill=(240, 245, 255, 255))
    draw.rectangle([28, 52, w-28, 66], fill=(220, 30, 40, 255))
    draw.rectangle([40, 80, 65, 100], fill=(245, 175, 20, 255)) # chip
    # QR mini lines
    draw.rectangle([80, 80, 120, 86], fill=(30, 40, 60, 255))
    draw.rectangle([80, 94, 110, 100], fill=(30, 40, 60, 255))
    img.save(os.path.join(out_dir, "icon_vietqr.png"))
    
    # ZaloPay
    img = Image.new("RGBA", (w, h), (0,0,0,0))
    draw = ImageDraw.Draw(img)
    draw.rounded_rectangle([6, 6, w-7, h-7], radius=24, fill=(0, 110, 220, 255), outline=(0, 255, 180, 255), width=3)
    # Glowing Z logo
    z_poly = [
        (40, 44), (w-40, 44), (w-40, 62), (64, h-62), (w-40, h-62),
        (w-40, h-44), (40, h-44), (40, h-62), (w-64, 62), (40, 62)
    ]
    draw.polygon(z_poly, fill=(0, 255, 150, 255))
    img.save(os.path.join(out_dir, "icon_zalopay.png"))
    
    # MoMo
    img = Image.new("RGBA", (w, h), (0,0,0,0))
    draw = ImageDraw.Draw(img)
    draw.rounded_rectangle([6, 6, w-7, h-7], radius=24, fill=(160, 10, 90, 255), outline=(255, 70, 180, 255), width=3)
    # Circle emblem
    draw.ellipse([30, 30, w-30, h-30], fill=(220, 20, 120, 255), outline=(255, 255, 255, 255), width=3)
    # Letter M
    draw.line([(48, 106), (48, 54), (80, 84), (112, 54), (112, 106)], fill=(255, 255, 255, 255), width=10)
    img.save(os.path.join(out_dir, "icon_momo.png"))
    print("Payment icons created")

# 8. Realistic High-Tech QR Code Image (320x320)
def generate_sample_qr():
    w, h = 320, 320
    img = Image.new("RGBA", (w, h), (255, 255, 255, 255))
    draw = ImageDraw.Draw(img)
    
    # Rounded border
    draw.rounded_rectangle([2, 2, w-3, h-3], radius=16, outline=(0, 210, 255, 255), width=4)
    
    # QR matrix simulation (29x29 modules)
    grid_n = 29
    mod_size = 9
    offset_x = (w - grid_n * mod_size) // 2
    offset_y = (h - grid_n * mod_size) // 2
    
    rng = random.Random(4299)
    modules = [[rng.choice([True, False]) for _ in range(grid_n)] for _ in range(grid_n)]
    
    def set_finder(r0, c0):
        for r in range(7):
            for c in range(7):
                is_edge = (r == 0 or r == 6 or c == 0 or c == 6)
                is_center = (2 <= r <= 4 and 2 <= c <= 4)
                modules[r0 + r][c0 + c] = is_edge or is_center
        # quiet zone around finder
        for r in range(-1, 8):
            for c in range(-1, 8):
                if (r == -1 or r == 7 or c == -1 or c == 7):
                    rr, cc = r0 + r, c0 + c
                    if 0 <= rr < grid_n and 0 <= cc < grid_n:
                        modules[rr][cc] = False

    set_finder(0, 0)
    set_finder(0, grid_n - 7)
    set_finder(grid_n - 7, 0)
    
    # Draw modules
    for r in range(grid_n):
        for c in range(grid_n):
            if modules[r][c]:
                x1 = offset_x + c * mod_size
                y1 = offset_y + r * mod_size
                draw.rectangle([x1, y1, x1 + mod_size - 1, y1 + mod_size - 1], fill=(12, 16, 28, 255))
                
    # Center cyber logo inside QR
    logo_r = 24
    cx, cy = w // 2, h // 2
    draw.rounded_rectangle([cx - logo_r, cy - logo_r, cx + logo_r, cy + logo_r], radius=8, fill=(0, 220, 255, 255), outline=(255, 0, 180, 255), width=3)
    # Diamond inside center
    draw.polygon([(cx, cy - 14), (cx + 14, cy), (cx, cy + 14), (cx - 14, cy)], fill=(255, 255, 255, 255))
    
    img.save(os.path.join(out_dir, "sample_qr.png"))
    print("sample_qr.png created")

# 9. Pack Gems Icons (Tier 1 -> Tier 6) (180x180)
def generate_gem_pack_icons():
    w, h = 180, 180
    
    def draw_gem(draw, cx, cy, sz, base_color=(0, 220, 255), highlight=(220, 255, 255)):
        # Faceted diamond shape
        top_y = cy - sz
        bot_y = cy + sz * 1.1
        mid_y = cy - sz * 0.2
        left_x = cx - sz
        right_x = cx + sz
        in_left = cx - sz * 0.5
        in_right = cx + sz * 0.5
        
        # Facets
        # Top crown table
        draw.polygon([(in_left, top_y), (in_right, top_y), (right_x, mid_y), (left_x, mid_y)], fill=base_color)
        # Table center facet
        draw.polygon([(in_left, top_y), (in_right, top_y), (cx, mid_y)], fill=highlight)
        # Lower pavilion facets
        draw.polygon([(left_x, mid_y), (cx, mid_y), (cx, bot_y)], fill=(int(base_color[0]*0.8), int(base_color[1]*0.8), int(base_color[2]*0.8)))
        draw.polygon([(right_x, mid_y), (cx, mid_y), (cx, bot_y)], fill=base_color)
        draw.polygon([(left_x, mid_y), (cx, bot_y), (in_left, mid_y)], fill=(int(base_color[0]*0.6), int(base_color[1]*0.6), int(base_color[2]*0.6)))
        # Outline
        draw.line([(in_left, top_y), (in_right, top_y), (right_x, mid_y), (cx, bot_y), (left_x, mid_y), (in_left, top_y)], fill=(255, 255, 255, 220), width=2)
        draw.line([(in_left, top_y), (cx, mid_y), (cx, bot_y)], fill=(255, 255, 255, 200), width=1)
        draw.line([(in_right, top_y), (cx, mid_y)], fill=(255, 255, 255, 200), width=1)

    # Tier 1: Single Diamond
    img1 = Image.new("RGBA", (w, h), (0,0,0,0))
    d1 = ImageDraw.Draw(img1)
    draw_gem(d1, 90, 95, 48)
    img1.save(os.path.join(out_dir, "pack_gem_tier1.png"))

    # Tier 2: Double Diamond
    img2 = Image.new("RGBA", (w, h), (0,0,0,0))
    d2 = ImageDraw.Draw(img2)
    draw_gem(d2, 65, 105, 36)
    draw_gem(d2, 115, 90, 44)
    img2.save(os.path.join(out_dir, "pack_gem_tier2.png"))

    # Tier 3: Triple Diamond Cluster
    img3 = Image.new("RGBA", (w, h), (0,0,0,0))
    d3 = ImageDraw.Draw(img3)
    draw_gem(d3, 55, 115, 32)
    draw_gem(d3, 125, 115, 34)
    draw_gem(d3, 90, 85, 48)
    img3.save(os.path.join(out_dir, "pack_gem_tier3.png"))

    # Tier 4: Chest / Box of Diamonds
    img4 = Image.new("RGBA", (w, h), (0,0,0,0))
    d4 = ImageDraw.Draw(img4)
    # Chest base
    d4.rounded_rectangle([35, 95, 145, 150], radius=10, fill=(35, 25, 60, 255), outline=(0, 220, 255, 255), width=3)
    d4.rectangle([35, 115, 145, 130], fill=(255, 190, 40, 255)) # gold band
    d4.rounded_rectangle([80, 112, 100, 133], radius=4, fill=(255, 255, 255, 255)) # lock
    # Gems overflowing
    draw_gem(d4, 60, 80, 26)
    draw_gem(d4, 120, 80, 28)
    draw_gem(d4, 90, 68, 38)
    img4.save(os.path.join(out_dir, "pack_gem_tier4.png"))

    # Tier 5: Mountain of Gems & Crystals
    img5 = Image.new("RGBA", (w, h), (0,0,0,0))
    d5 = ImageDraw.Draw(img5)
    # Back crystals
    draw_gem(d5, 45, 100, 32, (180, 40, 255))
    draw_gem(d5, 135, 100, 34, (180, 40, 255))
    # Mid gems
    draw_gem(d5, 65, 125, 36)
    draw_gem(d5, 115, 125, 36)
    # Big center gem
    draw_gem(d5, 90, 75, 52)
    img5.save(os.path.join(out_dir, "pack_gem_tier5.png"))

    # Tier 6: Royal Crown & Massive Gem
    img6 = Image.new("RGBA", (w, h), (0,0,0,0))
    d6 = ImageDraw.Draw(img6)
    # Gold Crown Base
    crown_poly = [(30, 150), (150, 150), (155, 115), (125, 130), (90, 100), (55, 130), (25, 115)]
    d6.polygon(crown_poly, fill=(255, 185, 30, 255), outline=(255, 245, 160, 255))
    d6.line([(30, 145), (150, 145)], fill=(200, 20, 80, 255), width=6) # ruby velvet strip
    # Giant celestial gem resting on crown
    draw_gem(d6, 90, 65, 50, (0, 240, 255), (255, 255, 255))
    img6.save(os.path.join(out_dir, "pack_gem_tier6.png"))
    print("Gem pack icons created")

# 10. Pack Coins Icons (Tier 1 -> Tier 4) (180x180)
def generate_coin_pack_icons():
    w, h = 180, 180
    
    def draw_coin(draw, cx, cy, sz):
        draw.ellipse([cx - sz, cy - sz, cx + sz, cy + sz], fill=(255, 180, 20, 255), outline=(255, 230, 100, 255), width=3)
        draw.ellipse([cx - sz*0.7, cy - sz*0.7, cx + sz*0.7, cy + sz*0.7], outline=(200, 130, 10, 255), width=2)
        # Star in center
        draw.polygon([(cx, cy - sz*0.4), (cx + sz*0.3, cy + sz*0.3), (cx - sz*0.3, cy + sz*0.3)], fill=(255, 245, 160, 255))

    # Tier 1: 3 coins
    img1 = Image.new("RGBA", (w, h), (0,0,0,0))
    d1 = ImageDraw.Draw(img1)
    draw_coin(d1, 70, 105, 34)
    draw_coin(d1, 110, 100, 36)
    draw_coin(d1, 90, 75, 40)
    img1.save(os.path.join(out_dir, "pack_coin_tier1.png"))

    # Tier 2: Pouch of coins
    img2 = Image.new("RGBA", (w, h), (0,0,0,0))
    d2 = ImageDraw.Draw(img2)
    # Pouch shape
    d2.ellipse([45, 65, 135, 155], fill=(160, 70, 30, 255), outline=(255, 190, 50, 255), width=3)
    d2.polygon([(65, 55), (115, 55), (105, 75), (75, 75)], fill=(210, 90, 40, 255)) # neck
    # Gold ribbon
    d2.line([(60, 75), (120, 75)], fill=(255, 220, 50, 255), width=4)
    # Front coin
    draw_coin(d2, 90, 110, 26)
    img2.save(os.path.join(out_dir, "pack_coin_tier2.png"))

    # Tier 3: Gold Chest
    img3 = Image.new("RGBA", (w, h), (0,0,0,0))
    d3 = ImageDraw.Draw(img3)
    d3.rounded_rectangle([35, 85, 145, 150], radius=8, fill=(110, 60, 20, 255), outline=(255, 200, 40, 255), width=3)
    d3.rectangle([35, 105, 145, 120], fill=(255, 210, 50, 255))
    # Coins pile on top
    draw_coin(d3, 65, 75, 24)
    draw_coin(d3, 115, 75, 24)
    draw_coin(d3, 90, 65, 28)
    img3.save(os.path.join(out_dir, "pack_coin_tier3.png"))

    # Tier 4: Huge Vault of Gold
    img4 = Image.new("RGBA", (w, h), (0,0,0,0))
    d4 = ImageDraw.Draw(img4)
    # Gold ingots pyramid
    def draw_ingot(cx, cy, iw, ih):
        d4.polygon([(cx - iw, cy + ih), (cx + iw, cy + ih), (cx + iw*0.7, cy - ih), (cx - iw*0.7, cy - ih)], fill=(255, 195, 30, 255), outline=(255, 250, 180, 255), width=2)
    draw_ingot(60, 135, 35, 14)
    draw_ingot(120, 135, 35, 14)
    draw_ingot(90, 110, 35, 14)
    draw_coin(d4, 60, 85, 22)
    draw_coin(d4, 120, 85, 22)
    draw_coin(d4, 90, 70, 28)
    img4.save(os.path.join(out_dir, "pack_coin_tier4.png"))
    print("Coin pack icons created")

# 11. Special Packs: VIP Pass & Starter Pack (180x180)
def generate_special_pack_icons():
    w, h = 180, 180
    
    # VIP Pass Card
    img1 = Image.new("RGBA", (w, h), (0,0,0,0))
    d1 = ImageDraw.Draw(img1)
    # Angled futuristic VIP Card
    poly = [(30, 45), (150, 30), (145, 140), (25, 155)]
    d1.polygon(poly, fill=(24, 18, 52, 255), outline=(255, 200, 50, 255), width=3)
    # Neon gold stripe
    d1.line([(32, 90), (148, 75)], fill=(255, 215, 60, 255), width=8)
    # VIP text badge
    d1.polygon([(75, 45), (105, 45), (115, 65), (65, 65)], fill=(255, 40, 140, 255))
    img1.save(os.path.join(out_dir, "pack_vip_pass.png"))

    # Starter Gift Pack
    img2 = Image.new("RGBA", (w, h), (0,0,0,0))
    d2 = ImageDraw.Draw(img2)
    # Gift box
    d2.rounded_rectangle([40, 70, 140, 150], radius=8, fill=(180, 20, 90, 255), outline=(255, 240, 120, 255), width=3)
    # Ribbon
    d2.line([(90, 70), (90, 150)], fill=(255, 220, 40, 255), width=12)
    d2.line([(40, 110), (140, 110)], fill=(255, 220, 40, 255), width=12)
    # Bow on top
    d2.ellipse([60, 45, 90, 75], outline=(255, 220, 40, 255), width=6)
    d2.ellipse([90, 45, 120, 75], outline=(255, 220, 40, 255), width=6)
    img2.save(os.path.join(out_dir, "pack_starter.png"))
    print("Special pack icons created")

if __name__ == "__main__":
    generate_title_naptien()
    generate_card_pack_bg()
    generate_card_pack_highlight()
    generate_badge_bonus()
    generate_btn_price_tag()
    generate_modal_payment_bg()
    generate_payment_icons()
    generate_sample_qr()
    generate_gem_pack_icons()
    generate_coin_pack_icons()
    generate_special_pack_icons()
    print("ALL NAPTIEN SPRITES SUCCESSFULLY GENERATED!")
