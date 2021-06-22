#version 450 core

in VertexOutput
{
    vec2 uv;
};

#define ALPHA_TO_COVERAGE_FOLIAGE

#ifdef ALPHA_TO_COVERAGE_FOLIAGE
out vec4 Color;
#endif

uniform sampler2D AlbedoTex;
uniform float AlphaCutout;

void main(void)
{
    float alpha = texture(AlbedoTex, uv).a;
    if (alpha < AlphaCutout)
        discard;

#ifdef ALPHA_TO_COVERAGE_FOLIAGE
    // See: https://bgolus.medium.com/anti-aliased-alpha-test-the-esoteric-alpha-to-coverage-8b177335ae4f
    alpha = (alpha - AlphaCutout) / max(fwidth(alpha), 0.0001) + 0.5;
    Color = vec4(0,0,0,alpha);
#endif
}