#version 450 core

layout (location = 0) in vec3 in_position;
layout (location = 1) in vec2 in_uv;

out gl_PerVertex
{
    vec4 gl_Position;
};

out VertexOutput
{
    vec2 uv;
};

uniform mat4 mvp;

void main()
{
    gl_Position = vec4(in_position, 1) * mvp;
    uv = in_uv;
}
