#!/usr/bin/env python3
"""Procedurally generates the 12 textures for Lab 10 (texture budget).

Run:      python3 Tools/generate_textures.py
Requires: numpy, Pillow. Output: Assets/Textures/*.png (deterministic).
The initial import settings (RGBA32, mipmaps) are applied by the editor menu
Lab -> Reset Texture Import Settings (see README, "For the instructor").
"""
import json, os
import numpy as np
from PIL import Image, ImageDraw, ImageFont

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Textures")
FONT = "/usr/share/fonts/liberation-sans-fonts/LiberationSans-%s.ttf"
os.makedirs(OUT, exist_ok=True)


def save(name, rgb, alpha=None):
    rgb = np.clip(rgb, 0, 255).astype(np.uint8)
    a = np.full(rgb.shape[:2] + (1,), 255, np.uint8) if alpha is None else np.clip(alpha, 0, 255).astype(np.uint8)[..., None]
    Image.fromarray(np.concatenate([rgb, a], axis=2), "RGBA").save(os.path.join(OUT, name + ".png"))
    print("ok", name, rgb.shape[1], "x", rgb.shape[0])


def fnoise(n, beta, seed):
    """Tileable noise in 0..1: white noise shaped in the frequency domain with a 1/f^beta spectrum."""
    rng = np.random.default_rng(seed)
    f = np.fft.fftfreq(n)
    r = np.sqrt(f[:, None] ** 2 + f[None, :] ** 2)
    r[0, 0] = 1
    spec = np.fft.fft2(rng.standard_normal((n, n))) / r ** (beta / 2)
    spec[0, 0] = 0
    v = np.real(np.fft.ifft2(spec))
    v -= v.min()
    return v / v.max()


def smooth(x, a, b):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def lerp(c0, c1, t):
    return np.array(c0, float) * (1 - t[..., None]) + np.array(c1, float) * t[..., None]


def normal_map(h, strength):
    dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * strength
    dy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * strength
    n = np.stack([-dx, dy, np.ones_like(h)], axis=2)
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return (n * 0.5 + 0.5) * 255


# ---------- ground ----------
def ground():
    n = 2048
    big, mid, fine = fnoise(n, 2.4, 1), fnoise(n, 1.8, 2), fnoise(n, 0.8, 3)
    grass = smooth(big + (mid - 0.5) * 0.5, 0.42, 0.58)
    dirt = lerp((112, 90, 62), (86, 68, 46), mid)
    green = lerp((62, 104, 40), (92, 138, 52), fine)
    rgb = dirt * (1 - grass[..., None]) + green * grass[..., None]
    rgb *= (0.88 + 0.24 * fine)[..., None]
    pebbles = smooth(fnoise(n, 0.3, 4), 0.86, 0.9) * (1 - grass)
    rgb = rgb * (1 - pebbles[..., None] * 0.6) + 130 * pebbles[..., None] * 0.6
    save("Ground_Albedo", rgb)
    h = mid * 0.6 + fine * 0.25 + pebbles * 0.8
    save("Ground_Normal", normal_map(h, 28))


# ---------- brick wall ----------
def building():
    n, bw, bh, m = 2048, 256, 128, 8
    y, x = np.mgrid[0:n, 0:n]
    row = y // bh
    xs = x + (row % 2) * (bw // 2)
    ox, oy = xs % bw, y % bh
    dist = np.minimum.reduce([ox, bw - 1 - ox, oy, bh - 1 - oy]).astype(float)
    brick = smooth(dist, m / 2 - 1, m / 2 + 9)
    rng = np.random.default_rng(10)
    ids = rng.random((n // bh, n // bw + 1))
    tone = ids[row, (xs // bw)]
    grain = fnoise(n, 1.2, 11)
    col = lerp((150, 62, 46), (176, 88, 62), tone) * (0.85 + 0.3 * grain)[..., None]
    mortar = np.array((168, 162, 150)) * (0.85 + 0.2 * grain)[..., None]
    rgb = mortar * (1 - brick[..., None]) + col * brick[..., None]
    save("Building_Albedo", rgb)
    save("Building_Normal", normal_map(brick * 1.0 + grain * 0.12, 22))


# ---------- wooden planks (props) ----------
def props():
    n, pw = 1024, 256
    y, x = np.mgrid[0:n, 0:n]
    pid = x // pw
    ox = x % pw
    rng = np.random.default_rng(20)
    warp = fnoise(n, 2.8, 21)
    grain = 0.5 + 0.5 * np.sin((x * 0.11 + warp * 40 + pid * 17) + fnoise(n, 1.4, 22) * 6)
    plank = smooth(np.minimum(ox, pw - 1 - ox).astype(float), 2, 8)
    base = lerp((120, 82, 48), (168, 120, 70), grain)
    base *= (0.8 + 0.4 * rng.random(4)[pid])[..., None]
    rgb = base * plank[..., None] + np.array((40, 28, 18)) * (1 - plank[..., None])
    save("Props_Albedo", rgb)
    save("Props_Normal", normal_map(plank * 0.8 + grain * 0.15, 16))


# ---------- sky ----------
def skybox():
    n = 2048
    v = (np.arange(n) / n)[:, None] * np.ones((1, n))
    u = np.ones((n, 1)) * (np.arange(n) / n)[None, :]
    top = lerp((38, 88, 178), (150, 195, 235), smooth(v, 0.0, 0.5))
    below = lerp((150, 175, 195), (96, 112, 106), smooth(v, 0.5, 0.75))
    sky = np.where((v < 0.5)[..., None], top, below)
    d = np.hypot((u - 0.3) * 2, v - 0.33)
    sky += (np.exp(-d * 7) * 90 + (d < 0.02) * 200)[..., None] * np.array((1.0, 0.92, 0.7))
    c = smooth(fnoise(n, 3.0, 30) + (fnoise(n, 1.6, 31) - 0.5) * 0.35, 0.52, 0.72)
    c *= smooth(v, 0.02, 0.12) * (1 - smooth(v, 0.38, 0.5))
    cloud = lerp((205, 212, 225), (255, 255, 255), smooth(fnoise(n, 2.0, 32), 0.3, 0.7))
    rgb = sky * (1 - c[..., None]) + cloud * c[..., None]
    save("Skybox", rgb)


# ---------- character: woven fabric ----------
def character():
    n = 1024
    y, x = np.mgrid[0:n, 0:n]
    weave = 0.5 + 0.5 * np.sin(x * 2 * np.pi / 8) * np.sin(y * 2 * np.pi / 8)
    stripe = smooth(np.abs((x % 256) - 128).astype(float), 36, 44)  # 0 at the stripe centre
    base = lerp((28, 70, 120), (44, 98, 160), weave) * (0.9 + 0.2 * fnoise(n, 1.5, 40))[..., None]
    accent = lerp((214, 112, 28), (240, 150, 52), weave)
    rgb = accent * (1 - stripe[..., None]) + base * stripe[..., None]
    save("Character_Albedo", rgb)
    save("Character_Normal", normal_map(weave, 6))


# ---------- noise ----------
def noise():
    v = fnoise(512, 1.0, 50)
    g = 128 + (v - 0.5) * 90 + (np.random.default_rng(51).random((512, 512)) - 0.5) * 30
    save("Noise", np.stack([g, g, g], 2))


# ---------- decals, 2x2 cells ----------
def decals():
    n, c = 1024, 512
    rgb = np.zeros((n, n, 3))
    a = np.zeros((n, n))
    yy, xx = np.mgrid[0:c, 0:c]

    def put(cx, cy, col, alpha):
        rgb[cy * c:(cy + 1) * c, cx * c:(cx + 1) * c] = col
        a[cy * c:(cy + 1) * c, cx * c:(cx + 1) * c] = alpha * 255

    # (0,0) oil stain: irregular with a soft edge
    r = np.hypot(xx - 256, yy - 256) / 220 + (fnoise(c, 2.5, 60) - 0.5) * 0.7
    put(0, 0, (20, 18, 16), (1 - smooth(r, 0.75, 1.0)) * 0.85)
    # (1,0) cracks
    im = Image.new("L", (c, c), 0)
    dr = ImageDraw.Draw(im)
    rng = np.random.default_rng(61)
    for _ in range(7):
        px, py = 256, 256
        ang = rng.uniform(0, 6.28)
        for _ in range(9):
            ang += rng.uniform(-0.6, 0.6)
            nx, ny = px + np.cos(ang) * rng.uniform(15, 35), py + np.sin(ang) * rng.uniform(15, 35)
            dr.line([(px, py), (nx, ny)], fill=255, width=int(rng.integers(2, 5)))
            px, py = nx, ny
    put(1, 0, (25, 22, 20), np.asarray(im) / 255 * 0.9)
    # (0,1) yellow arrow with hard edges
    im = Image.new("L", (c, c), 0)
    ImageDraw.Draw(im).polygon([(256, 40), (440, 250), (330, 250), (330, 470), (182, 470), (182, 250), (72, 250)], fill=255)
    put(0, 1, (240, 190, 20), np.asarray(im) / 255)
    # (1,1) red splat
    rng = np.random.default_rng(62)
    im = Image.new("L", (c, c), 0)
    dr = ImageDraw.Draw(im)
    dr.ellipse([156, 156, 356, 356], fill=255)
    for _ in range(40):
        ang, dist, rad = rng.uniform(0, 6.28), rng.uniform(110, 230), rng.uniform(4, 22)
        px, py = 256 + np.cos(ang) * dist, 256 + np.sin(ang) * dist
        dr.ellipse([px - rad, py - rad, px + rad, py + rad], fill=255)
    put(1, 1, (150, 20, 24), np.asarray(im) / 255 * 0.95)
    save("Decals", rgb, a)


# ---------- UI ----------
UI_REGIONS = {}


def ui_atlas():
    n = 2048
    img = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    f = lambda style, s: ImageFont.truetype(FONT % style, s)

    def region(name, x, y, w, h):
        UI_REGIONS[name] = [x, y, w, h]

    # health bar frame (the fill is drawn by the game separately, see hp_fill)
    region("hp_bar", 0, 0, 512, 64)
    d.rectangle([0, 0, 511, 63], fill=(20, 22, 28, 255))
    d.rectangle([4, 4, 507, 59], outline=(220, 220, 230, 255), width=2)
    region("hp_fill", 4, 68, 8, 8)
    d.rectangle([0, 64, 15, 79], fill=(200, 40, 48, 255))
    # buttons
    for i, (name, label, col) in enumerate([("btn_play", "ГРАТИ", (46, 140, 80)),
                                           ("btn_options", "НАЛАШТУВАННЯ", (50, 90, 160)),
                                           ("btn_quit", "ВИХІД", (160, 54, 54))]):
        y = 80 + i * 140
        region(name, 0, y, 512, 128)
        d.rounded_rectangle([0, y, 511, y + 127], radius=12, fill=col + (255,), outline=(255, 255, 255, 255), width=3)
        d.text((256, y + 64), label, font=f("Bold", 52), fill=(255, 255, 255, 255), anchor="mm")
    # info panel with small text
    region("panel_info", 540, 0, 760, 520)
    d.rectangle([540, 0, 1299, 519], fill=(24, 28, 38, 255), outline=(150, 160, 190, 255), width=3)
    d.text((560, 14), "Журнал місії", font=f("Bold", 36), fill=(255, 214, 90, 255))
    lines = ["Зберіть 8 монет і знайдіть золотий ключ.", "Червоні плями на землі відбирають здоров'я.",
             "Масляна пляма сповільнює рух.", "Ящики можна штовхати, бочки — ні.",
             "Жовта стрілка вказує, де лежить ключ.", "Рух: WASD · Стрибок: Пробіл · Біг: Shift",
             "Tab — камера, R — почати заново."]
    for i, t in enumerate(lines):
        d.text((560, 72 + i * 34), t, font=f("Regular", 22), fill=(225, 228, 238, 255))
    sizes = [18, 16, 14, 12, 11]
    for i, s in enumerate(sizes):
        d.text((560, 330 + i * 34), f"Дрібний шрифт {s} px: Щасливий їжак ґуляє по яру, дзьобає ключі 0123456789",
               font=f("Regular", s), fill=(200, 206, 222, 255))
    # icon grid, 16x2 cells of 128 px
    region("icons", 0, 560, 2048, 256)
    cols = [(230, 70, 80), (250, 200, 50), (90, 190, 120), (90, 150, 240), (200, 120, 230), (240, 150, 60), (230, 230, 240), (120, 130, 150)]
    for i in range(32):
        gx, gy = (i % 16) * 128, 560 + (i // 16) * 128
        col = cols[i % 8] + (255,)
        d.rounded_rectangle([gx + 6, gy + 6, gx + 121, gy + 121], radius=10, fill=(28, 32, 44, 255), outline=(90, 100, 130, 255), width=2)
        cx, cy = gx + 64, gy + 64
        k = i % 8
        if k == 0:    d.polygon([(cx, cy + 36), (cx - 40, cy - 6), (cx - 22, cy - 34), (cx, cy - 16), (cx + 22, cy - 34), (cx + 40, cy - 6)], fill=col)
        elif k == 1:  d.polygon([(cx + 40 * np.sin(a * 0.6283), cy - 40 * np.cos(a * 0.6283)) if a % 2 == 0 else (cx + 17 * np.sin(a * 0.6283), cy - 17 * np.cos(a * 0.6283)) for a in range(10)], fill=col)
        elif k == 2:  d.ellipse([cx - 34, cy - 34, cx + 34, cy + 34], fill=col); d.ellipse([cx - 16, cy - 16, cx + 16, cy + 16], fill=(28, 32, 44, 255))
        elif k == 3:  d.rectangle([cx - 8, cy - 40, cx + 8, cy + 40], fill=col); d.rectangle([cx - 40, cy - 8, cx + 40, cy + 8], fill=col)
        elif k == 4:  d.polygon([(cx - 30, cy - 36), (cx + 38, cy), (cx - 30, cy + 36)], fill=col)
        elif k == 5:  d.line([(cx - 36, cy + 36), (cx + 36, cy - 36)], fill=col, width=10); d.ellipse([cx + 14, cy - 50, cx + 50, cy - 14], fill=col)
        elif k == 6:  d.rectangle([cx - 30, cy - 4, cx + 30, cy + 36], fill=col); d.arc([cx - 22, cy - 40, cx + 22, cy + 4], 180, 360, fill=col, width=8)
        else:         d.polygon([(cx, cy - 40), (cx + 36, cy + 30), (cx - 36, cy + 30)], outline=col, width=6)
    # small caption strip for the icons
    region("caption", 0, 830, 1024, 40)
    d.rectangle([0, 830, 1023, 869], fill=(20, 22, 28, 255))
    d.text((10, 836), "Інвентар: 1 — серце · 2 — зірка · 3 — кільце · 4 — хрест · 5 — грати · 6 — ключ", font=f("Regular", 20), fill=(240, 240, 250, 255))
    # rest of the atlas: colour tiles so it is not empty
    rng = np.random.default_rng(70)
    for gy in range(900, n, 128):
        for gx in range(0, n, 128):
            c = tuple(int(v) for v in rng.integers(40, 220, 3))
            d.rounded_rectangle([gx + 4, gy + 4, gx + 123, gy + 123], radius=14, fill=c + (255,), outline=(255, 255, 255, 160), width=2)
            d.text((gx + 64, gy + 64), f"{(gy - 900) // 128 * 16 + gx // 128}", font=f("Bold", 30), fill=(255, 255, 255, 255), anchor="mm")
    img.save(os.path.join(OUT, "UI_Atlas.png"))
    print("ok UI_Atlas")
    with open(os.path.join(OUT, "UI_Atlas_regions.json"), "w", encoding="utf-8") as fh:
        json.dump(UI_REGIONS, fh, ensure_ascii=False, indent=1)


if __name__ == "__main__":
    for fn in (ground, building, props, skybox, character, noise, decals, ui_atlas):
        fn()
