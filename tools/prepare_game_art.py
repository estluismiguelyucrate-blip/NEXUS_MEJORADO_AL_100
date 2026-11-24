"""Convert the supplied checkerboard JPEG character art into transparent game sprites.

Requires opencv-python-headless and Pillow. The input JPEGs are retained beside each output.
"""
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ART = {
    "assets/characters/hunter-ariadna-source.jpeg": ("assets/characters/hunter-ariadna.png", (120, 70, 810, 900)),
    "assets/characters/hunter-kael-source.jpeg": ("assets/characters/hunter-kael.png", (70, 35, 875, 930)),
    "assets/characters/hunter-soren-source.jpeg": ("assets/characters/hunter-soren.png", (220, 35, 650, 930)),
    "assets/bosses/boss-umbral-source.jpeg": ("assets/bosses/boss-umbral.png", (135, 45, 820, 925)),
    "assets/bosses/boss-bosque-source.jpeg": ("assets/bosses/boss-bosque.png", (35, 50, 940, 900)),
    "assets/bosses/boss-ciudad-source.jpeg": ("assets/bosses/boss-ciudad.png", (220, 55, 610, 900)),
}


def main() -> None:
    for source_rel, (output_rel, rect) in ART.items():
        source = cv2.imread(str(ROOT / source_rel), cv2.IMREAD_COLOR)
        if source is None:
            raise FileNotFoundError(ROOT / source_rel)
        mask = np.zeros(source.shape[:2], np.uint8)
        bg_model = np.zeros((1, 65), np.float64)
        fg_model = np.zeros((1, 65), np.float64)
        cv2.grabCut(source, mask, rect, bg_model, fg_model, 8, cv2.GC_INIT_WITH_RECT)
        alpha = np.where((mask == cv2.GC_FGD) | (mask == cv2.GC_PR_FGD), 255, 0).astype(np.uint8)
        # The supplied JPEGs contain a fake checkerboard, not actual transparency.
        # Its 20.48px checker cells are keyed by their tile position and flat neutral
        # color. This also removes squares inside gaps between limbs and flame effects.
        gray = cv2.cvtColor(source, cv2.COLOR_BGR2GRAY)
        chroma = source.max(axis=2).astype(np.int16) - source.min(axis=2).astype(np.int16)
        local_mean = cv2.blur(gray, (5, 5))
        local_sq_mean = cv2.blur(gray.astype(np.float32) ** 2, (5, 5))
        local_std = np.sqrt(np.maximum(0, local_sq_mean - local_mean.astype(np.float32) ** 2))
        yy, xx = np.indices(gray.shape)
        tile_parity = (np.floor(xx / 20.48).astype(np.int32) + np.floor(yy / 20.48).astype(np.int32)) % 2
        dark_tile = (tile_parity == 0) & (gray < 48)
        light_tile = (tile_parity == 1) & (gray >= 72) & (gray <= 205)
        checker = (dark_tile & (chroma < 16) & (local_std < 24)) | (light_tile & (chroma < 18))
        alpha[checker] = 0
        alpha = cv2.morphologyEx(alpha, cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
        alpha = cv2.GaussianBlur(alpha, (0, 0), 0.8)
        rgba = cv2.cvtColor(source, cv2.COLOR_BGR2RGBA)
        rgba[:, :, 3] = alpha
        image = Image.fromarray(rgba, "RGBA")
        bounds = image.getchannel("A").point(lambda a: 255 if a > 12 else 0).getbbox()
        if not bounds:
            raise RuntimeError(f"No foreground detected in {source_rel}")
        image = image.crop(bounds)
        image.save(ROOT / output_rel, optimize=True)
        print(f"{output_rel}: {image.size[0]}x{image.size[1]}, {image.getchannel('A').getbbox()}")


if __name__ == "__main__":
    main()
