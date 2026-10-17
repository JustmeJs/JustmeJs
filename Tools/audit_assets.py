#!/usr/bin/env python3
"""Check runtime resource references and passive-icon uniqueness without importing Godot."""
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1] / "KleinesSpiel"
pattern = re.compile(r'''["'](res://[^"']+)["']''')
roots = [root / "project.godot", *sorted((root / "Scripts").rglob("*.cs"))]
seen = set()
pending = list(roots)
missing = []
while pending:
    source = pending.pop()
    if source in seen:
        continue
    seen.add(source)
    if source.suffix not in {".cs", ".tscn", ".tres", ".godot", ".gdshader"}:
        continue
    for uri in pattern.findall(source.read_text(encoding="utf-8-sig")):
        target = root / uri.removeprefix("res://")
        if not target.is_file():
            missing.append((source.relative_to(root), uri))
        elif target not in seen:
            pending.append(target)
visuals = {p for folder in ("Assets", "Resources") for p in (root / folder).rglob("*")
           if p.is_file() and p.suffix.lower() in {".png", ".svg", ".jpg", ".webp", ".tres", ".gdshader", ".ogg", ".wav"}}
unused = sorted(visuals - seen)
icons = set()
effects = set()
for item in sorted((root / "Resources/Items").glob("*.tres")):
    text = item.read_text()
    icon_uri = re.search(r'\[ext_resource type="Texture2D" path="([^"]+)"', text).group(1)
    icon = root / icon_uri.removeprefix("res://")
    if icon.suffix == ".tres":
        icon_text = icon.read_text()
        region = re.search(r'region = Rect2\(([^)]+)\)', icon_text).group(1)
        atlas = re.search(r'path="([^"]+)"', icon_text).group(1)
        icons.add((atlas, region))
    else:
        icons.add((icon_uri, "whole"))
    effects.add(int(re.search(r'EffectType = (\d+)', text).group(1)))
for source, uri in missing:
    print(f"MISSING: {source}: {uri}")
for source in unused:
    print(f"UNUSED: {source.relative_to(root)}")
if missing or unused or len(icons) != 6 or effects != set(range(6)):
    raise SystemExit(1)
print(f"ASSET AUDIT PASSED: {len(visuals)} active visual resources; six distinct passive icons and effects.")
