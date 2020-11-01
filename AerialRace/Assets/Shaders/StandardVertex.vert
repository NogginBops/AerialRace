#version 450 core

layout (location = 0) in vec3 in_position;
layout (location = 1) in vec2 in_uv;
layout (location = 2) in vec3 in_normal;

out gl_PerVertex
{
    vec4 gl_Position;
};

out VertexOutput
{
    vec3 worldPosition;
    vec2 fragUV;
    vec3 fragNormal;
};

// GLSL defines it's matrices width first
// so a 3x4 matrix in normal math is actually
// a 4x3 matrix in glsl... smh

layout(std140) uniform PerVertex
{
    mat4 mvp;
    mat3 normalMatrix;
};

uniform vec2 tiling;

void main(void)
{
    vec4 pos = vec4(in_position, 1);
	gl_Position = mvp * pos;
    //projection * viewMatrix * modelMatix * in_position;
	worldPosition = vec3(modelMatrix * pos);
	fragUV = in_uv * tiling;
	fragNormal = normalMatrix * in_normal;
}
