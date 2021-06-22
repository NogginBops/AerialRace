#version 450 core

layout (location = 0) in vec3 in_position;

out gl_PerVertex
{
    vec4 gl_Position;
};

out VertexOutput
{
    vec2 uv;
};

uniform mat4 invProj;
uniform mat4 invView;
uniform mat4 invViewProj;

vec3 unproject(vec3 pos)
{
    vec4 unproj = vec4(pos.xyz, 1f) * invViewProj;
    return unproj.xyz / unproj.w;
}

void main(void)
{
    float x = -1.0 + float((gl_VertexID & 1) << 2);
    float y = -1.0 + float((gl_VertexID & 2) << 1);
    uv = vec2(x,y);
    gl_Position = vec4(x, y, 1f, 1f);

    //gl_Position = vec4(in_position, 1f);

    //uv = in_position.xy;
    // FIXME: This methods seems to have some jittering when moving
    //far = unproject(vec3(in_position.x, in_position.y, 0.9f));
    //near = unproject(vec3(in_position.x, in_position.y, 1));
}