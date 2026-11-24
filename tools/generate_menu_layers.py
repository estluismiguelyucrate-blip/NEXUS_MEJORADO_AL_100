"""Build the clean menu plate and masked hunter layer from the supplied reference.

Optional asset-authoring helper. Runtime needs only the two generated files, not Python.
Requires Pillow and NumPy when regenerating after replacing assets/menu-background.jpeg.
"""

from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "assets" / "menu-background.jpeg"
BACKGROUND = ROOT / "assets" / "backgrounds" / "menu-background-clean.jpg"
SPRITE = ROOT / "assets" / "characters" / "menu-hunter.png"
DESIGN_SIZE = (1360, 768)

# Hand-traced outer contour of the standing hunter and the long, windblown coat.
# Coordinates follow the supplied 1376x768 artwork before its 16:9 menu resize.
CONTOUR = [
    (866, 123), (878, 104), (895, 110), (914, 91), (933, 104),
    (951, 97), (970, 106), (991, 121), (1008, 143), (1015, 173),
    (1007, 190), (1021, 205), (1050, 203), (1081, 216), (1100, 246),
    (1113, 280), (1124, 324), (1136, 362), (1160, 391), (1198, 407),
    (1237, 418), (1274, 433), (1301, 450), (1304, 466), (1288, 480),
    (1261, 485), (1229, 476), (1195, 463), (1171, 466), (1150, 484),
    (1137, 511), (1126, 543), (1128, 572), (1141, 603), (1166, 632),
    (1180, 651), (1157, 672), (1131, 693), (1105, 718), (1082, 750),
    (1055, 746), (1034, 715), (1018, 681), (1003, 650), (985, 626),
    (960, 612), (935, 613), (906, 627), (876, 628), (849, 619),
    (824, 624), (794, 633), (763, 641), (729, 654), (696, 672),
    (666, 690), (637, 702), (608, 703), (589, 695), (618, 674),
    (646, 650), (675, 625), (706, 601), (737, 578), (765, 553),
    (789, 524), (806, 495), (816, 465), (821, 430), (818, 394),
    (813, 356), (816, 320), (823, 287), (837, 258), (849, 230),
    (852, 201), (858, 168),
]


def main() -> None:
    source = Image.open(SOURCE).convert("RGB").resize(DESIGN_SIZE, Image.Resampling.LANCZOS)
    scale_x = DESIGN_SIZE[0] / 1376
    mask = Image.new("L", DESIGN_SIZE, 0)
    scaled = [(round(x * scale_x), y) for x, y in CONTOUR]
    draw = ImageDraw.Draw(mask)
    draw.polygon(scaled, fill=255)
    # These translucent UI cards are re-drawn by WinForms above the animated art.
    for box in [(435, 26, 842, 111), (408, 650, 970, 744), (0, 728, 470, 768), (1190, 728, 1360, 768)]:
        draw.rectangle(box, fill=0)
    hard_mask = np.asarray(mask, dtype=np.uint8) > 0

    original = np.asarray(source, dtype=np.uint8)
    clean = original.astype(np.float32)
    # Fill behind the cutout by interpolating along each scanline. The moving
    # layer covers this repair at rest; only a narrow edge is exposed in motion.
    for y in range(DESIGN_SIZE[1]):
        xs = np.flatnonzero(hard_mask[y])
        if xs.size == 0:
            continue
        left, right = int(xs[0]), int(xs[-1])
        left_color = original[y, max(0, left - 1)].astype(np.float32)
        right_color = original[y, min(DESIGN_SIZE[0] - 1, right + 1)].astype(np.float32)
        blend = np.linspace(0.0, 1.0, right - left + 1, dtype=np.float32)[:, None]
        clean[y, left : right + 1] = left_color * (1.0 - blend) + right_color * blend

    BACKGROUND.parent.mkdir(parents=True, exist_ok=True)
    SPRITE.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(np.clip(clean, 0, 255).astype(np.uint8), "RGB").save(
        BACKGROUND, quality=94, optimize=True, progressive=True
    )

    feathered = mask.filter(ImageFilter.GaussianBlur(0.45))
    sprite = source.convert("RGBA")
    sprite.putalpha(feathered)
    bounds = feathered.getbbox()
    sprite.crop(bounds).save(SPRITE, optimize=True)
    print(f"Background: {BACKGROUND} ({BACKGROUND.stat().st_size:,} bytes)")
    print(f"Hunter layer: {SPRITE} ({SPRITE.stat().st_size:,} bytes), bounds={bounds}")


if __name__ == "__main__":
    main()
