#version 450 core

in VertexOutput
{
    vec4 sceneColor;
};

out vec4 Color;

uniform vec4 Tint;

void main(void)
{
    Color = vec4(sceneColor.rgb, 1.0);
}
