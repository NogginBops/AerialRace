#version 450 core

layout (location = 0) in vec4 in_position;
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

uniform PerCameraBlock
{
    mat4 projection;
    mat3x4 viewMatrix;
};

uniform mat4 modelMatrix;
uniform mat3 normalMatrix;

uniform vec2 tiling;

void main(void)
{
    // FIXME: Figure out this transformation!!
	gl_Position = vec4(in_position * viewMatrix, 1);
    //projection * viewMatrix * modelMatix * in_position;
	worldPosition = vec3(modelMatrix * in_position);
	fragUV = in_uv * tiling;
	fragNormal = normalMatrix * in_normal;
}
