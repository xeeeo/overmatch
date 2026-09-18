shader_type spatial;
render_mode cull_back;

uniform sampler2D terrain_mask : filter_linear, repeat_disable;
uniform vec2 map_size;
uniform vec4 meadow : source_color = vec4(0.34, 0.39, 0.24, 1.0);
uniform vec4 meadow_light : source_color = vec4(0.46, 0.46, 0.29, 1.0);
uniform vec4 earth : source_color = vec4(0.40, 0.33, 0.23, 1.0);
uniform vec4 stone : source_color = vec4(0.37, 0.38, 0.35, 1.0);
uniform vec4 water_deep : source_color = vec4(0.10, 0.28, 0.30, 1.0);
uniform float aridity = 0.25;
uniform float terrain_seed = 0.0;

float hash(vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

float noise(vec2 p) {
    vec2 i = floor(p), f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash(i), hash(i + vec2(1, 0)), f.x),
               mix(hash(i + vec2(0, 1)), hash(i + vec2(1, 1)), f.x), f.y);
}

float fbm(vec2 p) {
    mat2 turn = mat2(vec2(0.8, -0.6), vec2(0.6, 0.8));
    float value = noise(p) * 0.55;
    p = turn * p * 2.13 + 7.3;
    value += noise(p) * 0.28;
    p = turn * p * 2.07 + 3.7;
    return value + noise(p) * 0.17;
}

void fragment() {
    vec2 p = UV * map_size;
    vec2 n = p + vec2(terrain_seed, terrain_seed * 0.37);
    vec4 mask = texture(terrain_mask, UV);
    float road = smoothstep(0.24, 0.76, mask.r);
    float rough = smoothstep(0.12, 0.88, mask.g);
    float water = smoothstep(0.22, 0.78, mask.b);
    float cliff = smoothstep(0.1, 0.75, mask.a);
    float broad = fbm(n * 0.145);
    float mid = fbm(n * 0.83);
    float grain = noise(n * 9.4);
    float grit = smoothstep(0.74, 0.88, grain);
    float dry = smoothstep(0.40 - aridity * 0.15, 0.71 - aridity * 0.18, broad);
    vec3 ground = mix(meadow.rgb, meadow_light.rgb, mid * 0.80);
    ground = mix(ground, earth.rgb, dry * (0.30 + aridity * 0.52));
    // Fine grain belongs to the surface; larger variation describes soil patches.
    ground *= 0.73 + broad * 0.44 + grain * 0.035;
    ground += grit * 0.006;
    vec3 broken_earth = earth.rgb * (0.78 + mid * 0.35 + grain * 0.045);
    broken_earth = mix(broken_earth, stone.rgb, grit * 0.22);
    ground = mix(ground, broken_earth, rough);
    // A narrow gravel shoulder separates asphalt from the surrounding grass.
    float shoulder = smoothstep(0.02, 0.36, mask.r) * (1.0 - road);
    ground = mix(ground, mix(earth.rgb, stone.rgb, 0.4), shoulder * 0.72);
    vec3 asphalt = mix(vec3(0.055, 0.061, 0.058), vec3(0.093, 0.095, 0.083), mid);
    asphalt *= 0.92 + grain * 0.16;
    asphalt = mix(asphalt, earth.rgb, smoothstep(0.66, 0.88, broad) * 0.25);
    ground = mix(ground, asphalt, road);
    ground = mix(ground, stone.rgb * (0.72 + mid * 0.23), cliff);
    float bank = 1.0 - smoothstep(0.52, 0.98, mask.b);
    float ripple = sin(p.x * 5.0 + p.y * 1.7 + TIME * 0.7 + mid * 4.0)
                 * sin(p.y * 3.1 - p.x * 0.4 - TIME * 0.48);
    vec3 water_colour = mix(water_deep.rgb, water_deep.rgb * 1.38 + vec3(0.012, 0.035, 0.024), bank);
    water_colour += pow(max(ripple, 0.0), 8.0) * 0.022;
    float foam = bank * smoothstep(0.47, 0.68, mask.b) * (0.28 + mid * 0.45);
    water_colour = mix(water_colour, vec3(0.28, 0.35, 0.28), foam * 0.32);
    ALBEDO = mix(ground, water_colour, water);
    ROUGHNESS = mix(0.96, 0.27, water);
    SPECULAR = mix(0.12, 0.45, water);
    float h = noise(n * 4.3);
    float hx = noise(n * 4.3 + vec2(0.13, 0));
    float hy = noise(n * 4.3 + vec2(0, 0.13));
    vec2 bump = vec2(h-hx, h-hy) * mix(0.14, 0.04, road);
    bump = mix(bump, vec2(ripple * 0.026, ripple * 0.017), water);
    NORMAL_MAP = normalize(vec3(bump, 1.0)) * 0.5 + 0.5;
}
