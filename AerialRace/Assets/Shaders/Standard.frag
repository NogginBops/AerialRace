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

uniform sampler2D NormalTex;

uniform struct Sky {
    vec3 SunDirection;
    vec3 SunColor;
    vec3 SkyColor;
    vec3 GroundColor;
} sky;

uniform struct Scene {
    vec3 ambientLight;
} scene;

uniform bool UseShadows;
uniform sampler2DShadow ShadowMap;

struct PointLight
{
    vec4 posAndInvSqrRadius;
    vec4 intensity;
};

layout(row_major) uniform LightBlock
{
    int lightCount;
    PointLight light[256];
} lights;

vec3 skyColor(vec3 direction)
{
    vec3 sun = sky.SunColor * pow(max(dot(direction, sky.SunDirection), 0f), 200);
    float directionDot = dot(direction, vec3(0,1,0));
    const float margin = 0.005f;
    float groundMask = smoothstep(-margin, margin, directionDot);
    float skyGradient = max(1-(directionDot - 0.3f), 0);
    vec3 skyColor = sky.SkyColor * groundMask * skyGradient + (sky.GroundColor * (1-groundMask));
    return skyColor + sun;
}

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

vec3 CalcDirectionalDiffuse(vec3 L, vec3 N, vec3 lightColor, vec3 surfaceAlbedo)
{
    float diff = max(dot(N, L), 0.0f);
    return lightColor * diff * surfaceAlbedo;
}

// FIXME: Make our own attenuation curve?
// Atm we are using the unity version as described by catlikecoding
float CalcPointLightAttenuation(vec3 L, float invSqrRadius)
{
    float distanceSqr = dot(L, L);
    float dist2OverRad2 = distanceSqr * invSqrRadius;
    float attenSqrt = max(0, 1 - (dist2OverRad2 * dist2OverRad2));
    float attenuation = attenSqrt * attenSqrt;

    return attenuation;
}

void main(void)
{
    vec3 normal = normalize(gl_FrontFacing ? fragNormal : -fragNormal);
    
    vec3 q1 = dFdx(fragPos.xyz);
    vec3 q2 = dFdy(fragPos.xyz);
    vec2 st1 = dFdx(fragUV);
    vec2 st2 = dFdy(fragUV);

    vec3 tangent = normalize(q1 * st2.t - q2 * st1.t);
    vec3 bitangent = normalize(-q1 * st2.s + q2 * st1.s);

    mat3 tangentToWorld = mat3(tangent, bitangent, normal);
    vec3 N = texture(NormalTex, fragUV).rgb;
    N = N * 2.0 - 1.0;
    // FIXME: Why is this the multiplication order?
    normal =  normalize(tangentToWorld * N);

    vec3 lightDir = sky.SunDirection;
    vec3 viewDir = normalize(ViewPos - fragPos.xyz);
    vec3 halfwayDir = normalize(lightDir + viewDir);

    vec3 albedo = vec3(texture(AlbedoTex, fragUV));

    float diff = max(dot(normal, lightDir), 0.0f);
    vec3 diffuse = sky.SunColor * diff * albedo;
    vec3 skyVec = normalize(normal + lightDir + reflect(-viewDir, normal));
    vec3 ambient = skyColor(skyVec) * albedo;
    ambient = scene.ambientLight * albedo;

    vec3 lightColor = vec3(0);
    for (int i = 0; i < lights.lightCount; i++)
    {
        PointLight light = lights.light[i];
        vec3 L = light.posAndInvSqrRadius.xyz - fragPos.xyz;

        // FIXME: Make our own attenuation curve?
        float attenuation = CalcPointLightAttenuation(L, light.posAndInvSqrRadius.w);
        
        float diff = max(dot(normal, normalize(L)), 0.0f);
        vec3 diffuse = diff * attenuation * albedo * lights.light[i].intensity.rgb;

        vec3 reflectDir = reflect(-lightDir, normal);
        float spec = max(dot(viewDir, reflectDir), 0.0f);
        vec3 specular = spec * attenuation * albedo * lights.light[i].intensity.rgb;

        lightColor += diffuse + specular;
    }

    float shadow = 1f - ShadowCalculation(lightSpacePosition, normal, sky.SunDirection);

    Color = vec4(ambient + diffuse * shadow + lightColor, 1);
}

