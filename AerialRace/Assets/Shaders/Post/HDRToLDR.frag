#version 450 core

#include <../Common/Tonemap.glsl>

in VertexOutput
{
    vec2 uv;
};

out vec4 Color;

uniform sampler2D HDR;

uniform int Tonemap;

void main(void)
{
    vec3 HDRColor = texture(HDR, uv).rgb;
    
    vec3 LDRColor;
    if (Tonemap == 0)
    {
        LDRColor = clamp(HDRColor / vec3(2f), 0f, 1f);
    }
    else if (Tonemap == 1)
    {
        LDRColor = ACESFitted(HDRColor);
    }
    else if (Tonemap == 2)
    {
        LDRColor = reinhard(HDRColor);
    }

    vec3 srgb = apply_sRGB_gamma(LDRColor);
    
    Color = vec4(srgb, 1);
}