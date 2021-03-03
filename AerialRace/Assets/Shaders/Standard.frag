#version 450 core

#include <Common/Textures.glsl>
#include <Common/Lighting.glsl>
#include <Common/Sky.glsl>
#include <Common/Shadows.glsl>

in VertexOutput
{
    vec4 fragPos;
    vec2 fragUV;
    vec3 fragNormal;
    //vec4 lightSpacePosition;
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

    vec3 albedo = vec3(texture(AlbedoTex, fragUV));

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

    float shadow = 1f - ShadowCalculation(fragPos.xyz, normal, sky.SunDirection);

    vec4 depthDebug = GetShadowCascadeDebugColor(ShadowCascadeFromDepth(gl_FragCoord.z));

    Color = vec4(ambient + diffuse * shadow + lightColor, 1);// + depthDebug;
}

