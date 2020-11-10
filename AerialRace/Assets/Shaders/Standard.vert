#version 450 core

layout (location = 0) in vec3 in_position;
layout (location = 1) in vec2 in_uv;
layout (location = 2) in vec3 in_normal;
//layout (location = 3) in vec4 in_color;

out gl_PerVertex
{
    vec4 gl_Position;
};

out VertexOutput
{
    vec4 fragPos;
    vec2 fragUV;
    vec3 fragNormal;
    vec4 lightSpacePosition;
};

uniform mat4 mvp;
uniform mat4 proj;
uniform mat4 view;
uniform mat4 model;

uniform mat3 normalMatrix;

uniform mat4 lightSpaceMatrix;
uniform mat4 modelToLightSpace;

void main(void)
{
    fragPos = vec4(in_position, 1f) * model;
    gl_Position = vec4(in_position, 1f) * mvp;
    //gl_Position = vec4(in_position, 1f) * model * view * proj;
    fragNormal = in_normal * normalMatrix;
    fragUV = in_uv;
    lightSpacePosition = vec4(in_position, 1f) * modelToLightSpace;
}
