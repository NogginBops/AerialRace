#version 450 core

#include <Common/Textures.glsl>
#include <Common/Lighting.glsl>
#include <Common/Sky.glsl>
#include <Common/Shadows.glsl>
#include <Common/Camera.glsl>

const float PI = 3.14159265359;

in VertexOutput
{
    vec4 fragPos;
    vec2 fragUV;
    vec3 fragNormal;
    //vec4 lightSpacePosition;
};

out vec4 Color;

uniform vec2 uvScale = vec2(1, 1);

uniform sampler2D AlbedoTex;
uniform sampler2D NormalTex;

uniform bool InvertRoughness;
uniform sampler2D RoughnessTex;

uniform sampler2D MetallicTex;

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

struct PointLight
{
    vec4 posAndInvRadius;
    vec4 intensity;
};

layout(row_major) uniform LightBlock
{
    int lightCount;
    PointLight light[256];
} lights;

vec3 CalcDirectionalDiffuse(vec3 L, vec3 N, vec3 lightColor, vec3 surfaceAlbedo)
{
    float diff = max(dot(N, L), 0.0);
    return lightColor * diff * surfaceAlbedo;
}

void main(void)
{
    vec3 vertexNormal = normalize(gl_FrontFacing ? fragNormal : -fragNormal);
    
    vec2 uv = fragUV * uvScale;

    vec3 q1 = dFdx(fragPos.xyz);
    vec3 q2 = dFdy(fragPos.xyz);
    vec2 st1 = dFdx(uv);
    vec2 st2 = dFdy(uv);

    vec3 tangent = gl_FrontFacing ? normalize(q1 * st2.t - q2 * st1.t) : normalize(q2 * st1.t - q1 * st2.t);
    vec3 bitangent = normalize(-q1 * st2.s + q2 * st1.s);

    //bitangent *= dot(cross(vertexNormal, tangent), bitangent) < 0.0 ? -1.0 : 1.0;

    //tangent *= gl_FrontFacing ? 1.0 : -1.0;
    
    //tangent = cross(bitangent, vertexNormal);
    tangent = normalize(tangent - vertexNormal * dot(vertexNormal, tangent));

    // This constructs a column major matrix
    mat3 tangentToWorld = mat3(tangent, bitangent, vertexNormal);
    vec3 N = texture(NormalTex, uv).rgb;
    N = N * 2.0 - 1.0;
    // tangentToWorld is column major 
    // so this is the correct multiplication order
    vec3 normal =  normalize(tangentToWorld * N);
    N = normal;

    //vec3 bitangent2 = normalize(bitangent - normal * dot(normal, bitangent));
    //vec3 tangent2 = normalize(tangent - normal * dot(normal, tangent));
    vec3 bitangent2 = normalize(cross(normal, tangent));
    vec3 tangent2 = normalize(cross(normal, bitangent2));
    mat3 fragTangentToWorld = mat3(tangent2, bitangent2, normal);

    //Color = vec4(tangent2, 1);
    //return;

    vec3 V = normalize(u_Camera.position.xyz - fragPos.xyz);
    vec3 R = reflect(-V, normal);

    vec3 albedo = vec3(texture(AlbedoTex, uv)) * material.Tint;
    float roughTex = texture(RoughnessTex, uv).r;
    float metallicTex = texture(MetallicTex, uv).r;

    

    vec3 sunLight;
    vec3 ambient;
    {
        vec3 L = sky.SunDirection;
        
        vec3 H = normalize(V + L);

        float diff = max(dot(normal, L), 0.0);
        vec3 diffuse = sky.SunColor * diff * albedo;
        
        vec3 skyVec = normalize(normal + L + R);
        //ambient = skyIrradianceH(fragTangentToWorld, H) * albedo;
        //ambient = skyIrradianceVN(N) * albedo;
        ambient = skyIrradiance(fragTangentToWorld) * albedo;
        //vec3 ambient = skyIrradianceVN(normal) * albedo;
        //ambient = scene.ambientLight * albedo;
        ambient *= HorizonOcclusion(R, normal);

        vec3 radiance = sky.SunColor * 100 * albedo;

        vec3 F0 = vec3(0.04);
        vec3 F  = fresnelSchlick(max(dot(H, V), 0.0), F0);

        float roughness = material.Roughness * (InvertRoughness ? 1 - roughTex : roughTex);
        float metallic = material.Metallic * metallicTex;

        float NDF = DistributionGGX(N, H, roughness);
        float G   = GeometrySmith(N, V, L, roughness);

        vec3 numerator = NDF * G * F;
        float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0);
        vec3 specular = numerator / max(denominator, 0.0001);

        specular *= HorizonOcclusion(R, normal);

        vec3 kS = F;
        vec3 kD = vec3(1.0) - kS;
        kD *= 1.0 - metallic;

        float NdotL = max(dot(N, L), 0.0);
        sunLight = (kD * albedo / PI + specular) * radiance * NdotL;
    }

    //ambient = vec3(0.01);
    //diffuse = vec3(0);

    vec3 lightColor = vec3(0);
    for (int i = 0; i < lights.lightCount; i++)
    {
        PointLight light = lights.light[i];
        vec3 L = light.posAndInvRadius.xyz - fragPos.xyz;
        float distance = length(L);
        L = normalize(L);

        vec3 H = normalize(V + L);

        //float radius = CalcLightRadius(Luminance(light.intensity));
        //if (distance > radius) continue;

        // FIXME: Make our own attenuation curve?
        //float attenuation = CalcPointLightAttenuation2(distance, light.posAndInvRadius.w);
        //float attenuation = CalcPointLightAttenuation(distance, Luminance(light.intensity));
        //float attenuation = CalcPointLightAttenuation3(distance);
        float attenuation = CalcPointLightAttenuation5(distance, light.posAndInvRadius.w);
        vec3 radiance = light.intensity.rgb * light.intensity.w * attenuation;

        if (length(radiance) < 0.1) continue;

        vec3 F0 = vec3(0.04);
        vec3 F  = fresnelSchlick(max(dot(H, V), 0.0), F0);

        //lightColor += F * radiance;
        //continue;

        float roughTex = texture(RoughnessTex, uv).r;
        float metallicTex = texture(MetallicTex, uv).r;

        //float roughTex = 0.0;
        float roughness = material.Roughness * (InvertRoughness ? 1 - roughTex : roughTex);
        float metallic = material.Metallic * metallicTex;

        float NDF = DistributionGGX(N, H, roughness);
        float G   = GeometrySmith(N, V, L, roughness);

        vec3 numerator = NDF * G * F;
        float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0);
        vec3 specular = numerator / max(denominator, 0.0001);

        specular *= HorizonOcclusion(R, normal);

        vec3 kS = F;
        vec3 kD = vec3(1.0) - kS;
        kD *= 1.0 - metallic;

        float NdotL = max(dot(N, L), 0.0);
        lightColor += (kD * albedo / PI + specular) * radiance * NdotL;
    /*
        float diff = max(dot(normal, normalize(L)), 0.0);
        vec3 diffuse = diff * attenuation * albedo * lights.light[i].intensity.rgb;

        vec3 reflectDir = reflect(-lightDir, normal);
        float spec = max(dot(viewDir, reflectDir), 0.0);
        vec3 specular = spec * attenuation * albedo * lights.light[i].intensity.rgb;
        specular = vec3(0);
        lightColor += diffuse + specular;
    */
    }

    float depth = linearDepth();

    float shadow = 1.f - ShadowCalculation(fragPos.xyz, depth, normal, sky.SunDirection);

    float shadowBlend  = GetShadowBlend(depth, ShadowCascadeFromDepth(depth));

    vec4 depthDebug = GetShadowCascadeDebugColor(ShadowCascadeFromDepth(linearDepth()));

    Color = vec4(ambient + sunLight * shadow + lightColor, 1);// + depthDebug;
    //Color = vec4(shadow, shadow, shadowBlend,1);
}

