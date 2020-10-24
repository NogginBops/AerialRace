#version 450 core

in VertexOutput
{
    vec4 fragColor;
    vec2 fragUV;
    vec3 fragNormal;
};

out vec4 Color;

layout(std140) uniform LightData
{
    vec4 position;
    vec4 colorAndAttenuation;
    vec4 coneDirectionAndAngle;
};

layout(std140) uniform Camera {
    //vec3 ViewPos;
    vec4 ClearColor;
    float Fov;
    float Aspect;
    vec2 NearFarPlane;
} camera;

uniform vec3 ViewPos;

uniform sampler2D testTex;

void main(void)
{
    float f = dot(ViewPos, fragNormal);
    f = f < 0f ? 0f : f;
    vec2 uv = fragUV.xy;
    uv.y = 1- uv.y;
    Color = vec4(texture(testTex, uv).rgb * f, 1);
    Color = vec4(fragNormal, 1);
    //Color = vec4(f, f, f, 1);
}

