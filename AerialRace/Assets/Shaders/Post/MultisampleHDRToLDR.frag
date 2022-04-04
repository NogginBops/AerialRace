#version 450 core

#include <../Common/Tonemap.glsl>

in VertexOutput
{
    vec2 uv;
};

out vec4 Color;

uniform int Samples;
uniform sampler2DMS HDR;

uniform int Tonemap;

void main(void)
{
    // Resolve the multisample
    ivec2 texSize = textureSize(HDR);
    ivec2 texCoord = ivec2(uv * texSize);
    vec3 HDRColor = vec3(0);
    for (int i = 0; i < Samples; i++)
    {
        HDRColor += texelFetch(HDR, texCoord, i).rgb;
    }
    HDRColor /= float(Samples);

    vec3 LDRColor;
    if (Tonemap == 0)
    {
        LDRColor = clamp(HDRColor / vec3(2), 0, 1);
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