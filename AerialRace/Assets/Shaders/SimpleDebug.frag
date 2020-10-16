#version 450 core

in VertexOutput
{
    vec4 fragColor;
    vec2 fragUV;
};

out vec4 Color;

layout(std140) uniform LightData
{
    vec4 position;
    vec4 colorAndAttenuation;
    vec4 coneDirectionAndAngle;
};

uniform sampler2D testTex;

void main(void)
{
    Color = vec4(texture(testTex, fragUV).rgb, 1);
}

