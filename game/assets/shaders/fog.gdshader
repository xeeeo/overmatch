shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, depth_test_disabled;

uniform sampler2D fog_tex : filter_linear, repeat_disable;

void fragment() {
    float a = texture(fog_tex, UV).r;
    ALBEDO = vec3(0.03, 0.04, 0.06);
    ALPHA = a * 0.93;
}
