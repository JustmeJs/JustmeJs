#!/usr/bin/env python3
"""Author Godot frame resources from the original 6 x 4 directional sprite sheets.

The right-facing poses are mirrored for left movement by ActorAnimator. This
keeps asymmetric, generated side views consistent while preserving front/back
poses. Source images are retained unchanged.
"""
from pathlib import Path
import json
ROOT = Path(__file__).resolve().parents[1] / 'KleinesSpiel'
TARGET = ROOT / 'Resources/Animations/Directions'
TARGET.mkdir(exist_ok=True)
SUMMONERS = json.loads((ROOT / 'Assets/Sprites/Directions/summoner_regions.json').read_text())
for sheet in sorted((ROOT / 'Assets/Sprites/Directions').glob('*.png')):
    parts = ['[gd_resource type="SpriteFrames" load_steps=26 format=3]',
             f'[ext_resource type="Texture2D" path="res://Assets/Sprites/Directions/{sheet.name}" id="sheet"]']
    for row in range(4):
        for col in range(6):
            x, y, w, h = SUMMONERS[sheet.stem][row*6+col] if sheet.stem in SUMMONERS else (col*256, row*256, 256, 256)
            margin = f'\nmargin = Rect2({(256-w)/2}, {168-h}, {256-w}, {256-h})' if sheet.stem in SUMMONERS else ''
            parts.append(f'[sub_resource type="AtlasTexture" id="f{row}_{col}"]\natlas = ExtResource("sheet")\nregion = Rect2({x}, {y}, {w}, {h}){margin}\nfilter_clip = true')
    animations = []
    for direction, row in [('down',0), ('left',2), ('right',2), ('up',3), ('',0)]:
        for state, cols, speed in [('idle',[0],4), ('walk',[1,0,2,0],10), ('attack',[3,4],9), ('hurt',[0,3],10), ('death',[0,5],6), ('dash',[1,2],12)]:
            actual_row = row
            if sheet.stem == 'crypt_watcher' and row == 2:
                if state in ('walk','dash'): cols = [0,1,0,1]
                if state == 'attack': actual_row = 1
            frames = ', '.join('{"duration": 1.0, "texture": SubResource("f%d_%d")}' % (actual_row,c) for c in cols)
            name = state + ('_' + direction if direction else '')
            animations.append('{"frames": [%s], "loop": %s, "name": &"%s", "speed": %s}' % (frames, str(state in ('idle','walk','dash')).lower(), name, speed))
    if sheet.stem == 'crypt_watcher':
        animations.append('{"frames": [{"duration": 1.0, "texture": SubResource("f0_3")}, {"duration": 1.0, "texture": SubResource("f0_0")}], "loop": false, "name": &"attack_radial", "speed": 9.0}')
    parts.append('[resource]\nanimations = [\n' + ',\n'.join(animations) + '\n]')
    (TARGET/(sheet.stem+'.tres')).write_text('\n\n'.join(parts)+'\n')
print('Built directional frames for',len(list(TARGET.glob('*.tres'))),'characters')
