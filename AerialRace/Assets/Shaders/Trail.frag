#version 450 core

in VertexOutput
{
    vec4 fragPos;
    vec4 lightSpacePosition;
};

out vec4 Color;

uniform vec3 ViewPos;

uniform sampler2D AlbedoTex;

uniform bool UseShadows;
uniform sampler2DShadow ShadowMap;

void main(void)
{
    Color = vec4(1, 0, 1, 0.5f);
}

