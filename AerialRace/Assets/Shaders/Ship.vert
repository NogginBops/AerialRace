#version 450 core

layout (location = 0) in vec3 in_position;
layout (location = 1) in vec2 in_uv;
layout (location = 2) in vec3 in_normal;
layout (location = 3) in vec4 in_color;

out gl_PerVertex
{
    vec4 gl_Position;
};

out VertexOutput
{
    vec4 fragPos;
    vec2 fragUV;
    vec3 fragNormal;
    vec4 fragColor;
};

uniform mat4 mvp;
uniform mat4 proj;
uniform mat4 view;
uniform mat4 model;
uniform mat3 normalMatrix;

uniform Camera {
    vec3 ViewPos;
    vec4 ClearColor;
    float  Fov;
    float Aspect;
    vec2 NearFarPlane;
} camera;

void main(void)
{
    fragPos = vec4(in_position, 1f) * model;
    gl_Position = vec4(in_position, 1f) * mvp;
    //gl_Position = vec4(in_position, 1f) * model * view * proj;
    
    fragColor = in_color;
    fragUV = in_uv;
    fragNormal = in_normal * normalMatrix;
}
