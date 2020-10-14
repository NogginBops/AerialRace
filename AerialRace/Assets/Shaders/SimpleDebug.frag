#version 450 core

in VertexOutput
{
    vec4 fragColor;
};

out vec4 Color;

void main(void)
{
    Color = vec4(fragColor.rgb, 1);
}

