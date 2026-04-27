"""
Strip explicit color overrides from Jalium.UI built-in controls across all
Gallery pages. Keeps colors on custom visual elements (Border card containers,
Shape-derived primitives, brush resource definitions) untouched so the card
chrome stays styled while the Jalium.UI controls inherit the framework theme.
"""

import os
import re
import sys

VIEWS_DIR = (
    r"D:\Users\suppe\source\repos\Jalium.UI.Gallery"
    r"\Modules\Jalium.UI.Gallery.Modules.Main\Views"
)

# Tags whose Foreground/Background/BorderBrush/Fill/Stroke we KEEP -
# these are visual-element wrappers or brush-definition elements, not
# framework functional controls.
KEEP_COLOR_TAGS = {
    # Visual containers / shapes
    "Border",
    "Rectangle",
    "Ellipse",
    "Path",
    "Line",
    "Polygon",
    "Polyline",
    "Shape",
    # Brush resource elements
    "SolidColorBrush",
    "LinearGradientBrush",
    "RadialGradientBrush",
    "GradientStop",
}

# Attributes to strip on framework controls.
STRIP_ATTRS = ("Foreground", "Background", "BorderBrush")

# A start tag is `<TagName ...>` or `<TagName .../>`. We capture:
#   1. TagName (bare, no dot - we skip property-element forms like `<Button.Foreground>`)
#   2. The attribute run (may be empty, spans newlines because [^<>] includes \n)
#   3. Close `>` or `/>`
TAG_RE = re.compile(r"<([A-Za-z][A-Za-z0-9]*)(\s[^<>]*)?(/?>)", re.DOTALL)


def strip_attr(attr_run: str, prop: str) -> str:
    """Remove any `<whitespace>Prop="..."` runs from the given attribute blob."""
    # Greedy on the inner value so we don't truncate at interior punctuation,
    # but the value never contains a `"` because none of the Gallery pages use
    # quoted strings inside attribute values.
    pattern = re.compile(r"\s+" + re.escape(prop) + r'\s*=\s*"[^"]*"')
    return pattern.sub("", attr_run)


def rewrite_tag(match: re.Match) -> str:
    tag = match.group(1)
    attrs = match.group(2) or ""
    close = match.group(3)

    if tag in KEEP_COLOR_TAGS:
        return match.group(0)

    new_attrs = attrs
    for prop in STRIP_ATTRS:
        new_attrs = strip_attr(new_attrs, prop)

    return "<" + tag + new_attrs + close


def process_file(path: str) -> bool:
    with open(path, "r", encoding="utf-8") as f:
        content = f.read()

    new_content = TAG_RE.sub(rewrite_tag, content)
    if new_content == content:
        return False

    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(new_content)
    return True


def main() -> int:
    changed = 0
    scanned = 0
    for root, _dirs, files in os.walk(VIEWS_DIR):
        # Skip build output directories.
        if os.sep + "obj" + os.sep in root or os.sep + "bin" + os.sep in root:
            continue
        for fname in files:
            if not fname.endswith(".jalxaml"):
                continue
            scanned += 1
            path = os.path.join(root, fname)
            if process_file(path):
                changed += 1
                print(f"  modified: {os.path.relpath(path, VIEWS_DIR)}")

    print(f"\nScanned {scanned} files, modified {changed}.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
