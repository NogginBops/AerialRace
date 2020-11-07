#version 450 core

layout (location = 0) in vec3 in_position;

out gl_PerVertex
{
    vec4 gl_Position;
};

uniform mat4 mvp;

void main()
{
    gl_Position = vec4(in_position, 1) * mvp;
}
