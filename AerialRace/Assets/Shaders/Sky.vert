#version 450 core

layout (location = 0) in vec3 in_position;

out gl_PerVertex
{
    vec4 gl_Position;
};

out VertexOutput
{
    vec3 near;
    vec3 far;
};

uniform mat4 invProj;
uniform mat4 invView;
uniform mat4 invViewProj;

uniform float nearPlane;
uniform float farPlane;

vec3 unproject(vec3 pos)
{
    vec4 unproj = vec4(pos.xyz, 1f) * invViewProj;
    return unproj.xyz/unproj.w;
}

void main(void)
{
    gl_Position = vec4(in_position, 1f);

    // FIXME: This methods seems to have some jittering when moving
    far = unproject(vec3(in_position.x, in_position.y, 0));
    near = unproject(vec3(in_position.x, in_position.y, 1));
}