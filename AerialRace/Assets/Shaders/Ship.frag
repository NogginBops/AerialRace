#version 450 core

in VertexOutput
{
    vec4 fragPos;
    vec2 fragUV;
    vec3 fragNormal;
    vec4 fragColor;
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

uniform struct DirectionalLight {
    vec3 direction;
    vec3 color;
} dirLight;

uniform struct Scene {
    vec3 ambientLight;
} scene;

uniform mat3 normalMatrix;

void main(void)
{
    vec3 lightDir = -dirLight.direction;
    vec3 viewDir = normalize(ViewPos - fragPos.xyz);
    vec3 halfwayDir = normalize(lightDir + viewDir);

    vec3 albedo = vec3(texture(testTex, fragUV));

    vec3 norm = normalize(fragNormal);
    float diff = max(dot(norm, lightDir), 0.0f);
    vec3 diffuse = dirLight.color * diff * albedo;
    vec3 ambient = scene.ambientLight * albedo;

    Color = vec4(ambient + diffuse, 1);

    //float f = dot(viewDir, fragNormal);
    //f = f < 0f ? 0f : f;
    //Color = vec4(fragNormal * f, 1);

    //Color = vec4(texture(testTex, fragUV).rgb, 1);
    //f = pow(f, 2f);
    //Color = vec4(f, f, f, 1);
}

