
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

float Luminance(vec4 color)
{
    // https://en.wikipedia.org/wiki/Relative_luminance
    // These values are for Rec.709 primaries
    // FIXME: Don't assume Rec.709 primaries...
    return dot(color.rgb, vec3(0.2126, 0.7152, 0.0722)) * color.a;
}

float CalcLightRadius(float lightness)
{
    return max(sqrt(lightness / LightCutout) - 1, 0);
}

// What is the unit for lightness?
float CalcPointLightAttenuation(float distance, float lightness)
{
    //return lightness / distance;
    //const float LightCutout = 0.005f;
    float radius = max(sqrt(lightness / LightCutout) - 1, 0);
    float xplus1 = distance + 1;
    float numerator = lightness * (radius - distance);
    float denom = radius * (xplus1 * xplus1);
    return numerator;
    float attenuation = numerator / denom;
    return max(attenuation, 0);
}

float CalcPointLightAttenuation2(float distance, float radius)
{
    float xplus1 = distance + 1;
    float denom = radius * (xplus1 * xplus1);
    float attenuation = 1f / denom;
    return attenuation;
}

float CalcPointLightAttenuation3(float distance)
{
    const float PI = 3.14159265359;
    float d1pi4 = 4 * PI * distance + 1;
    return 1 / (d1pi4 * d1pi4);
}

float CalcPointLightAttenuation4(float distance, float luminance)
{
    const float PI = 3.14159265359;
    float dmax = (sqrt(luminance / LightCutout) - 1);
    float dprim = 2.1 * tan((PI * distance)/(2*dmax));
    float dprim1 = dprim + 1;
    if (LightCutout == 0) return CalcPointLightAttenuation3(distance);
    if (distance > dmax) return 0;
    return 1 / (dprim1 * dprim1);
}

// https://seblagarde.files.wordpress.com/2015/07/course_notes_moving_frostbite_to_pbr_v32.pdf
float CalcPointLightAttenuation5(float distance, float invRadius)
{
    float factor = clamp(1 - pow(distance * invRadius, 4), 0, 1);
    return (factor * factor) / (distance * distance);
}

