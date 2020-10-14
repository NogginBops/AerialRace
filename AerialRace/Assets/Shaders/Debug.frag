#version 450 core

in VertexOutput
{
    vec3 worldPosition;
    vec2 fragUV;
    vec3 fragNormal;
};

out vec4 Color;

void main(void)
{
    Color = vec4(fragUV, 0, 1);
}

