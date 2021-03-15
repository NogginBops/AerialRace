#version 450 core

in VertexOutput
{
    vec2 uv;
};

uniform sampler2D AlbedoTex;
uniform float AlphaCutout;

void main(void)
{
    float alpha = texture(AlbedoTex, uv).a;
    if (alpha < AlphaCutout)
        discard;
}