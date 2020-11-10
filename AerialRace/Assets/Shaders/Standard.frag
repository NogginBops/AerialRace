#version 450 core

in VertexOutput
{
    vec4 fragPos;
    vec2 fragUV;
    vec3 fragNormal;
    vec4 lightSpacePosition;
};

out vec4 Color;

uniform vec3 ViewPos;

uniform sampler2D AlbedoTex;

uniform struct DirectionalLight {
    vec3 direction;
    vec3 color;
} dirLight;

uniform struct Scene {
    vec3 ambientLight;
} scene;

uniform bool UseShadows;
uniform sampler2DShadow ShadowMap;


float ShadowCalculation(vec4 fragPosLightSpace, vec3 normal, vec3 lightDir)
{
	if (!UseShadows) return 0;

    // perform perspective divide
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    // transform to [0,1] range
    projCoords = projCoords * 0.5 + 0.5;
    // get depth of current fragment from light's perspective
    float currentDepth = projCoords.z;

    //float bias = max(0.0005 * (1.0 - abs(dot(normal, lightDir))), biasAmount);
    //bias = biasAmount;
    //bias = 0.03 * (1.0 - abs(dot(normal, lightDir)));
    //bias = biasAmount;
    //bias = biasAmount;

	// FIXME: There might be a better way to do this!...
	if (min(min(projCoords.x, projCoords.y), 1 - max(projCoords.x, projCoords.y)) < 0) return 0;

    float pcf1 = texture(ShadowMap, vec3(projCoords.xy, currentDepth - 0.0005f));
    return pcf1;
}

void main(void)
{
    vec3 normal = normalize(gl_FrontFacing ? fragNormal : -fragNormal);

    vec3 lightDir = -dirLight.direction;
    vec3 viewDir = normalize(ViewPos - fragPos.xyz);
    vec3 halfwayDir = normalize(lightDir + viewDir);

    vec3 albedo = vec3(texture(AlbedoTex, fragUV));

    vec3 norm = normalize(normal);
    float diff = max(dot(norm, lightDir), 0.0f);
    vec3 diffuse = dirLight.color * diff * albedo;
    vec3 ambient = scene.ambientLight * albedo;

    float shadow = 1f - ShadowCalculation(lightSpacePosition, normal, dirLight.direction);

    Color = vec4(ambient + diffuse * shadow, 1);
}

