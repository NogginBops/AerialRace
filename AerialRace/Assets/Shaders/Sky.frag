#version 450 core

in VertexOutput
{
    vec3 near;
    vec3 far;
};

out vec4 Color;

uniform vec3 ViewPos;

uniform vec3 SunColor;
uniform vec3 SunDirection;

void main(void)
{
    vec3 viewDir = normalize(far - near);
    // FIXME: Figure out why we have to do this!
    viewDir.xy *= -1f;

    vec3 sun = SunColor * pow(max(dot(viewDir, SunDirection), 0f), 20);
    vec3 sky = vec3(0, 0, 0.5) * max(dot(viewDir + vec3(0,1,0), vec3(0,1,0)), 0f);

    Color = vec4(sky + sun, 1);
}