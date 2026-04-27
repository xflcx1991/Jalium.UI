"""
Replace every purple / magenta / pink hex color in the Gallery source tree with
an equivalent-lightness green. Color space is HSL: any hex whose hue lands in
the 250-335 band (blue-purple through magenta / hot pink) is rotated onto hue
140 (forest green) while keeping its original saturation and lightness so dark
purples become dark greens and pale lilacs become pale mints.

Leaves non-purple colors (reds, oranges, yellows, greens, cyans, blues, grays)
untouched so rainbow demo walls keep their other hues. Runs across .jalxaml
and .cs files in Jalium.UI.Gallery.
"""

import colorsys
import os
import re
import sys

GALLERY_ROOT = r"D:\Users\suppe\source\repos\Jalium.UI.Gallery"

# Capture exactly one hex color at a time. The alpha byte, when present, is
# preserved unchanged. We don't match 3-digit shorthand because none of the
# Gallery sources use it.
HEX_RE = re.compile(r"#([0-9A-Fa-f]{8}|[0-9A-Fa-f]{6})\b")

# Hue band (in degrees) treated as "purple-ish". Covers blue-purple (~260),
# violet (~275), purple (~290), magenta (~300), hot pink (~330), and the
# pink / crimson shoulder (~340-350) that Material-style palettes group with
# their purples. Stops before pure red (360/0).
PURPLE_HUE_MIN = 250.0
PURPLE_HUE_MAX = 350.0

# Target green hue (forest green, matches the Jalium.UI accent gradient).
TARGET_HUE = 140.0 / 360.0

# Require at least some saturation — skip near-gray colors that happen to
# round onto a purple hue (e.g. #808088).
MIN_SATURATION = 0.12


def remap(hex_body: str) -> str:
    if len(hex_body) == 8:
        alpha = hex_body[0:2]
        r = int(hex_body[2:4], 16)
        g = int(hex_body[4:6], 16)
        b = int(hex_body[6:8], 16)
    else:
        alpha = ""
        r = int(hex_body[0:2], 16)
        g = int(hex_body[2:4], 16)
        b = int(hex_body[4:6], 16)

    h, l, s = colorsys.rgb_to_hls(r / 255.0, g / 255.0, b / 255.0)
    if s < MIN_SATURATION:
        return "#" + hex_body

    hue_deg = h * 360.0
    if not (PURPLE_HUE_MIN <= hue_deg <= PURPLE_HUE_MAX):
        return "#" + hex_body

    nr, ng, nb = colorsys.hls_to_rgb(TARGET_HUE, l, s)
    nr_i = max(0, min(255, round(nr * 255)))
    ng_i = max(0, min(255, round(ng * 255)))
    nb_i = max(0, min(255, round(nb * 255)))
    new_body = f"{nr_i:02X}{ng_i:02X}{nb_i:02X}"
    if alpha:
        new_body = alpha.upper() + new_body
    return "#" + new_body


def rewrite_match(match: re.Match) -> str:
    return remap(match.group(1))


def process_file(path: str) -> int:
    with open(path, "r", encoding="utf-8") as f:
        content = f.read()

    hits = []

    def track(m: re.Match) -> str:
        original = m.group(0)
        replacement = rewrite_match(m)
        if replacement.upper() != original.upper():
            hits.append((original, replacement))
        return replacement

    new_content = HEX_RE.sub(track, content)
    if not hits:
        return 0

    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(new_content)

    rel = os.path.relpath(path, GALLERY_ROOT)
    print(f"  {rel} ({len(hits)} swap{'s' if len(hits) != 1 else ''})")
    return len(hits)


def main() -> int:
    total = 0
    files = 0
    for root, _dirs, fnames in os.walk(GALLERY_ROOT):
        if os.sep + "obj" + os.sep in root or os.sep + "bin" + os.sep in root:
            continue
        for name in fnames:
            if not (name.endswith(".jalxaml") or name.endswith(".cs")):
                continue
            path = os.path.join(root, name)
            n = process_file(path)
            if n:
                files += 1
                total += n

    print(f"\nReplaced {total} hex color(s) across {files} file(s).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
