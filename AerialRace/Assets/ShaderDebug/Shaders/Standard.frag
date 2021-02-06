#version 450 core
#line 0 1

const mat3 Linear_sRGB_to_ACEScg = mat3(
    0.4329260731344912, 0.3753918670512633, 0.189346267143399,
    0.08941221865742209, 0.8165493513876458, 0.10300469858914094,
    0.019161516239216973, 0.11815442902213172, 0.9420587449115669);

vec4 texture_ACES(sampler2D tex, vec2 texCoord)
{
    vec4 sRGB = texture(tex, texCoord);

    vec3 ACEScg = sRGB.rgb * Linear_sRGB_to_ACEScg;
    ACEScg = sRGB.rgb;

    return vec4(ACEScg, sRGB.a);
}
#line 3 0
#line 0 2

// This file contains common ligting functions

vec3 fresnelSchlick(float cosTheta, vec3 f0)
{
    return f0 + (1.0 - f0) * pow(max(1.0 - cosTheta, 0.0), 5.0);
}

// From: https://seblagarde.files.wordpress.com/2015/07/course_notes_moving_frostbite_to_pbr_v32.pdf
// f90 = saturate(50.0 * dot(fresnel0, 0.33));
vec3 F_Schlick(vec3 f0, float f90, float u)
{
    return f0 + (f90 - f0) * pow(1.0f - u, 5f);
}

float Fr_DisneyDiffuse(float NdotV, float NdotL, float LdotH, float linearRoughness)
{
    float energyBias = mix(0, 0.5f, linearRoughness);
    float energyFactor = mix(1.0f, 1.0f / 1.51f, linearRoughness);
    float fd90 = energyBias + 2.0f * LdotH*LdotH * linearRoughness;
    vec3 f0 = vec3(1.0f, 1.0f, 1.0f);
    float lightScatter = F_Schlick(f0, fd90, NdotL).r;
    float viewScatter = F_Schlick(f0, fd90, NdotV).r;

    return lightScatter * viewScatter * energyFactor;
}



// From: https://learnopengl.com/PBR/Lighting

float DistributionGGX(vec3 N, vec3 H, float roughness)
{
    const float PI = 3.14159265359;

    float a = roughness * roughness;
    float a2 = a * a;
    float NdotH = max(dot(N, H), 0);
    float NdotH2 = NdotH * NdotH;

    float num = a2;
    float denom = (NdotH2 * (a2 - 1.0f) + 1.0f);
    denom = PI * denom * denom;

    return num / denom;
}

float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = (roughness + 1.0);
    float k = (r*r) / 8.0;

    float num   = NdotV;
    float denom = NdotV * (1.0 - k) + k;
	
    return num / denom;
}

float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2  = GeometrySchlickGGX(NdotV, roughness);
    float ggx1  = GeometrySchlickGGX(NdotL, roughness);
	
    return ggx1 * ggx2;
}

uniform float LightCutout;

// What is the unit for lightness?
float CalcPointLightAttenuation(float distance, float lightness)
{
    return lightness / distance;
    //const float LightCutout = 0.005f;
    float radius = max(sqrt(lightness / LightCutout) - 1, 0);
    float xplus1 = distance + 1;
    float numerator = lightness * (radius - distance);
    float denom = radius * (xplus1 * xplus1);
    return numerator;
    float attenuation = numerator / denom;
    return attenuation;
}

float CalcPointLightAttenuation2(float distance, float radius)
{
    float xplus1 = distance + 1;
    float denom = radius * (xplus1 * xplus1);
    float attenuation = 1f / denom;
    return attenuation;
}
#line 4 0
#line 0 3

uniform struct Sky {
    vec3 SunDirection;
    vec3 SunColor;
    vec3 SkyColor;
    vec3 GroundColor;
} sky;

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



#line 5 0


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

struct Material 
{
    vec3 Tint;
    float Metallic;
    float Roughness;
};

uniform Material material;

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
    N = normal;

    vec3 lightDir = sky.SunDirection;
    vec3 viewDir = normalize(ViewPos - fragPos.xyz);
    vec3 halfwayDir = normalize(lightDir + viewDir);

    vec3 albedo = vec3(texture_ACES(AlbedoTex, fragUV));

    float diff = max(dot(normal, lightDir), 0.0f);
    vec3 diffuse = sky.SunColor * diff * albedo;
    vec3 skyVec = normalize(normal + lightDir + reflect(-viewDir, normal));
    vec3 ambient = skyColor(skyVec) * albedo;
    //ambient = scene.ambientLight * albedo;
    
    //ambient = vec3(0);
    //diffuse = vec3(0);

    vec3 V = normalize(ViewPos - fragPos.xyz);

    vec3 lightColor = vec3(0);
    for (int i = 0; i < lights.lightCount; i++)
    {
        PointLight light = lights.light[i];
        vec3 L = light.posAndInvSqrRadius.xyz - fragPos.xyz;
        float distance = length(L);
        L = normalize(L);

        vec3 H = normalize(V + L);

        // FIXME: Make our own attenuation curve?
        float attenuation = CalcPointLightAttenuation2(distance, light.posAndInvSqrRadius.w);
        vec3 radiance = light.intensity.rgb * attenuation;

        vec3 F0 = vec3(0.04f);
        vec3 F  = fresnelSchlick(max(dot(H, V), 0.0f), F0);

        //lightColor += F * radiance;
        //continue;

        float roughness = material.Roughness;
        float metallic = material.Metallic;

        float NDF = DistributionGGX(N, H, roughness);
        float G   = GeometrySmith(N, V, L, roughness);

        vec3 numerator = NDF * G * F;
        float denominator = 4.0f * max(dot(N, V), 0.0f) * max(dot(N, L), 0.0f);
        vec3 specular = numerator / max(denominator, 0.0001);

        vec3 kS = F;
        vec3 kD = vec3(1.0f) - kS;
        kD *= 1.0 - metallic;

        const float PI = 3.14159265359;

        float NdotL = max(dot(N, L), 0.0f);
        lightColor += (kD * albedo / PI + specular) * radiance * NdotL;
/*
        float diff = max(dot(normal, normalize(L)), 0.0f);
        vec3 diffuse = diff * attenuation * albedo * lights.light[i].intensity.rgb;

        vec3 reflectDir = reflect(-lightDir, normal);
        float spec = max(dot(viewDir, reflectDir), 0.0f);
        vec3 specular = spec * attenuation * albedo * lights.light[i].intensity.rgb;
        specular = vec3(0);
        lightColor += diffuse + specular;
*/
    }

    float shadow = 1f - ShadowCalculation(lightSpacePosition, normal, sky.SunDirection);

    Color = vec4(ambient + diffuse * shadow + lightColor, 1);
}

