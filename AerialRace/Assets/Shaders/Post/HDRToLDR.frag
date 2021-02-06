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
    
    vec3 LDRColor = HDRColor / (HDRColor + vec3(1.0));
    LDRColor = gammaCorrect(LDRColor, 2.2f);
    if (Tonemap == 0)
    {
        LDRColor = reinhard(HDRColor);
    }
    else if (Tonemap == 1)
    {
        LDRColor = reinhard(HDRColor);
    }
    
    Color = vec4(LDRColor, 1);
}