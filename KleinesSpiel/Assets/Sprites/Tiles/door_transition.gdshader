shader_type canvas_item;
uniform sampler2D open_texture : source_color, filter_nearest;
uniform float openness : hint_range(0.0, 1.0) = 0.0;
varying vec4 vertex_tint;

void vertex() {
    vertex_tint = COLOR;
}

void fragment() {
    vec4 closed = texture(TEXTURE, UV);
    vec4 opened = texture(open_texture, UV);
    // Reveal the passage as the portcullis lifts, in the door's local direction.
    float reveal = step(1.0 - openness, UV.y);
    COLOR = mix(closed, opened, reveal) * vertex_tint;
}
